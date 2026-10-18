package de.akakraftbs.android.workshoponline.core.auth

import de.akakraftbs.android.workshoponline.core.cache.ImagePipeline
import de.akakraftbs.android.workshoponline.core.cache.ResponseCache
import de.akakraftbs.android.workshoponline.core.connectivity.ConnectivityMonitor
import de.akakraftbs.android.workshoponline.core.network.ApiClient
import de.akakraftbs.android.workshoponline.core.network.ApiError
import de.akakraftbs.android.workshoponline.core.network.Query
import de.akakraftbs.android.workshoponline.core.server.ServerConfiguration
import de.akakraftbs.android.workshoponline.core.server.ServerStore
import de.akakraftbs.android.workshoponline.model.Permissions
import de.akakraftbs.android.workshoponline.model.User
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.sync.Mutex
import kotlinx.coroutines.sync.withLock
import kotlinx.serialization.DeserializationStrategy
import kotlinx.serialization.serializer
import okhttp3.OkHttpClient
import java.time.Instant
import javax.inject.Inject
import javax.inject.Singleton

enum class SessionState { Launching, SignedOut, PendingApproval, SignedIn }

data class Fetched<T>(val value: T, val date: Instant)

/**
 * Zentraler Einstieg für Anmeldung, Server und Datenzugriff.
 *
 * Datenzugriff läuft cache-first: [cached] liefert sofort den letzten Stand,
 * [fetch] lädt online nach und aktualisiert den Cache. Der Cache ist nach
 * Server *und* Nutzer getrennt ([dataScope]).
 */
@Singleton
class AppSession @Inject constructor(
    private val serverStore: ServerStore,
    val connectivity: ConnectivityMonitor,
    val cache: ResponseCache,
    val images: ImagePipeline,
    private val credentialStore: CredentialStore,
    private val httpClient: OkHttpClient,
) {
    private val _state = MutableStateFlow(SessionState.Launching)
    val state: StateFlow<SessionState> = _state.asStateFlow()

    private val _user = MutableStateFlow<User?>(null)
    val user: StateFlow<User?> = _user.asStateFlow()

    private val _signOutReason = MutableStateFlow<String?>(null)
    val signOutReason: StateFlow<String?> = _signOutReason.asStateFlow()

    @Volatile
    var client: ApiClient = makeClient(ServerConfiguration.Standard)
        private set

    private val _server = MutableStateFlow(client.server)
    val server: StateFlow<ServerConfiguration> = _server.asStateFlow()

    val permissions: Permissions get() = Permissions(_user.value)

    private val restoreMutex = Mutex()
    private var restored = false

    // region Lebenszyklus

    /** Stellt die letzte Sitzung wieder her. Mehrfache Aufrufe (UI, Worker) laufen nur einmal. */
    suspend fun restore() = restoreMutex.withLock {
        if (restored) return@withLock
        restored = true

        val stored = serverStore.current()
        if (stored != client.server) useServer(stored)
        client.loadCredentials()
        if (!client.hasCredentials) {
            _state.value = SessionState.SignedOut
            return@withLock
        }
        cache.entry(meKey)
            ?.let { runCatching { client.decode(User.serializer(), it.payload) }.getOrNull() }
            ?.let(::apply)
        refreshUser()
        if (_user.value == null) _state.value = SessionState.SignedOut
    }

    suspend fun login(email: String, password: String) {
        client.login(email.trim(), password)
        refreshUser()
        if (_user.value == null) throw ApiError.Transport("Anmeldung fehlgeschlagen.")
    }

    suspend fun loginWithGoogle(code: String, verifier: String) {
        client.exchangeGoogleCode(code, verifier)
        refreshUser()
        if (_user.value == null) throw ApiError.Transport("Anmeldung fehlgeschlagen.")
    }

    suspend fun refreshUser() {
        try {
            val data = client.getData("auth/me")
            val me = client.decode(User.serializer(), data)
            cache.store(data, key = meKey, scope = sessionScope)
            apply(me)
        } catch (error: ApiError) {
            // Offline oder Serverfehler: zuletzt bekannter Nutzer bleibt aktiv.
        }
    }

    suspend fun logout() {
        client.logout()
        cache.removeAll(dataScope)
        cache.removeAll(sessionScope)
        images.clear()
        _user.value = null
        _signOutReason.value = null
        _state.value = SessionState.SignedOut
    }

    /** Wechselt den Zielserver. Die Daten des bisherigen Servers bleiben getrennt erhalten. */
    suspend fun switchServer(server: ServerConfiguration) {
        if (server.id == client.server.id) return
        logout()
        serverStore.select(server)
        useServer(server)
        _state.value = SessionState.SignedOut
    }

    // endregion

    // region Datenzugriff

    suspend fun <T> cached(deserializer: DeserializationStrategy<T>, path: String, query: Query = emptyList()): Fetched<T>? {
        val entry = cache.entry(cacheKey(path, query)) ?: return null
        val value = runCatching { client.decode(deserializer, entry.payload) }.getOrNull() ?: return null
        return Fetched(value, entry.updatedAt)
    }

    suspend fun <T> fetch(deserializer: DeserializationStrategy<T>, path: String, query: Query = emptyList()): Fetched<T> {
        val data = client.getData(path, query)
        val value = client.decode(deserializer, data)
        cache.store(data, key = cacheKey(path, query), scope = dataScope)
        return Fetched(value, Instant.now())
    }

    /** Lädt eine Antwort nur in den Cache (Vorladen für die Offline-Nutzung). */
    suspend fun prefetch(path: String, query: Query = emptyList()) {
        val data = client.getData(path, query)
        cache.store(data, key = cacheKey(path, query), scope = dataScope)
    }

    // endregion

    // region Intern

    private val sessionScope: String get() = "${client.server.id}|session"
    private val meKey: String get() = "$sessionScope|auth/me"
    private val dataScope: String get() = "${client.server.id}|${_user.value?.id?.lowercase() ?: "anonymous"}"

    private fun cacheKey(path: String, query: Query): String {
        val normalized = path.removePrefix("/")
        val queryString = query.joinToString("&") { (name, value) -> "$name=$value" }
        return "$dataScope|$normalized?$queryString"
    }

    private fun apply(user: User) {
        _user.value = user
        _signOutReason.value = null
        _state.value = if (Permissions(user).isMember) SessionState.SignedIn else SessionState.PendingApproval
    }

    private fun useServer(server: ServerConfiguration) {
        client = makeClient(server)
        _server.value = server
    }

    private fun makeClient(server: ServerConfiguration) =
        ApiClient(server, httpClient, credentialStore, isOnline = { connectivity.isOnline }).also {
            it.onSessionExpired = ::handleSessionExpired
        }

    private fun handleSessionExpired() {
        _user.value = null
        _signOutReason.value = ApiError.Unauthorized.message
        _state.value = SessionState.SignedOut
    }

    // endregion
}

suspend inline fun <reified T> AppSession.cached(path: String, query: Query = emptyList()): Fetched<T>? =
    cached(serializer<T>(), path, query)

suspend inline fun <reified T> AppSession.fetch(path: String, query: Query = emptyList()): Fetched<T> =
    fetch(serializer<T>(), path, query)
