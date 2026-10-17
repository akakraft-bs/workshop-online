package de.akakraftbs.android.workshoponline.core.network

import de.akakraftbs.android.workshoponline.core.auth.CredentialStore
import de.akakraftbs.android.workshoponline.core.auth.Credentials
import de.akakraftbs.android.workshoponline.core.server.ServerConfiguration
import kotlinx.coroutines.suspendCancellableCoroutine
import kotlinx.coroutines.sync.Mutex
import kotlinx.coroutines.sync.withLock
import kotlinx.serialization.DeserializationStrategy
import kotlinx.serialization.SerializationException
import kotlinx.serialization.Serializable
import okhttp3.Call
import okhttp3.Callback
import okhttp3.Cookie
import okhttp3.Headers
import okhttp3.HttpUrl
import okhttp3.MediaType.Companion.toMediaType
import okhttp3.MultipartBody
import okhttp3.OkHttpClient
import okhttp3.Request
import okhttp3.RequestBody
import okhttp3.RequestBody.Companion.toRequestBody
import okhttp3.Response
import java.io.IOException
import java.util.concurrent.TimeUnit
import kotlin.coroutines.resume
import kotlin.coroutines.resumeWithException

enum class HttpMethod { GET, POST, PUT, PATCH, DELETE }

/** Query-Parameter in fester Reihenfolge – die Reihenfolge ist Teil des Cache-Schlüssels. */
typealias Query = List<Pair<String, String>>

/**
 * HTTP-Client für genau einen Server.
 *
 * - Lesende Requests ([getData]) und schreibende Requests ([send], [upload]) sind getrennt.
 *   Schreibende Requests werden offline grundsätzlich abgelehnt – die App ist offline read-only.
 * - Das Refresh-Token kommt vom Server als HttpOnly-Cookie. Es wird hier aus `Set-Cookie`
 *   gelesen, verschlüsselt gespeichert und beim Erneuern explizit als `Cookie` mitgeschickt.
 */
class ApiClient(
    val server: ServerConfiguration,
    baseHttpClient: OkHttpClient,
    private val credentialStore: CredentialStore,
    private val isOnline: () -> Boolean,
) {
    var onSessionExpired: (() -> Unit)? = null

    @Volatile
    var credentials: Credentials? = null
        private set

    val json = JsonCoding.json

    private val http = baseHttpClient.newBuilder()
        .cache(null)
        .connectTimeout(20, TimeUnit.SECONDS)
        .readTimeout(20, TimeUnit.SECONDS)
        .build()
    private val refreshMutex = Mutex()

    suspend fun loadCredentials() {
        credentials = credentialStore.load(server)
    }

    val hasCredentials: Boolean get() = credentials != null

    // region Lesen

    suspend fun getData(path: String, query: Query = emptyList()): String {
        if (!isOnline()) throw ApiError.Offline
        return perform(HttpMethod.GET, path, query)
    }

    suspend inline fun <reified T> get(path: String, query: Query = emptyList()): T =
        decode(kotlinx.serialization.serializer<T>(), getData(path, query))

    // endregion

    // region Schreiben (nur online)

    suspend fun send(method: HttpMethod, path: String, query: Query = emptyList(), bodyJson: String? = null): String {
        if (!isOnline()) throw ApiError.Offline
        val payload = bodyJson ?: if (method == HttpMethod.DELETE) null else "{}"
        return perform(method, path, query, payload?.toRequestBody(JSON))
    }

    suspend inline fun <reified B> send(method: HttpMethod, path: String, body: B, query: Query = emptyList()): String =
        send(method, path, query, json.encodeToString(kotlinx.serialization.serializer<B>(), body))

    suspend fun upload(path: String, bytes: ByteArray, fileName: String, mimeType: String): String {
        if (!isOnline()) throw ApiError.Offline
        val body = MultipartBody.Builder()
            .setType(MultipartBody.FORM)
            .addFormDataPart("file", fileName, bytes.toRequestBody(mimeType.toMediaType()))
            .build()
        return perform(HttpMethod.POST, path, body = body)
    }

    // endregion

    // region Öffentliche Auth-Endpunkte

    suspend fun sendPublic(method: HttpMethod, path: String, bodyJson: String?): String {
        if (!isOnline()) throw ApiError.Offline
        return perform(method, path, body = bodyJson?.toRequestBody(JSON), authenticated = false)
    }

    suspend fun login(email: String, password: String) {
        if (!isOnline()) throw ApiError.Offline
        val body = json.encodeToString(LoginBody.serializer(), LoginBody(email, password)).toRequestBody(JSON)
        val response = execute(makeRequest(HttpMethod.POST, "auth/login", emptyList(), body, authenticated = false))
        when (response.status) {
            200 -> storeTokens(response, previousRefreshToken = null)
            400, 401 -> throw ApiError.Server(response.status, "E-Mail oder Passwort ist falsch.")
            403 -> throw ApiError.EmailNotConfirmed
            else -> throw ApiError.Server(response.status, ApiError.message(response.body))
        }
    }

    /**
     * Startadresse für den Google-Login im Browser (Custom Tab).
     * Das Backend kennt Mobile-Clients unter `client=ios` und leitet danach auf `akakraft://auth` um.
     */
    fun googleLoginUrl(challenge: String): HttpUrl =
        url("auth/login/google", listOf("client" to "ios", "code_challenge" to challenge))

    /** Tauscht den Einmal-Code aus dem Google-Callback gegen Token und Refresh-Token. */
    suspend fun exchangeGoogleCode(code: String, verifier: String) {
        if (!isOnline()) throw ApiError.Offline
        val body = json.encodeToString(GoogleExchangeBody.serializer(), GoogleExchangeBody(code, verifier)).toRequestBody(JSON)
        val response = execute(makeRequest(HttpMethod.POST, "auth/google/exchange", emptyList(), body, authenticated = false))
        if (response.status != 200) {
            throw ApiError.Server(response.status, "Google-Anmeldung fehlgeschlagen. Bitte versuche es erneut.")
        }
        storeTokens(response, previousRefreshToken = null)
    }

    suspend fun logout() {
        val refreshToken = credentials?.refreshToken
        if (isOnline() && refreshToken != null) {
            val request = makeRequest(HttpMethod.POST, "auth/logout", emptyList(), EMPTY_BODY, authenticated = true)
                .newBuilder().header("Cookie", "refresh_token=$refreshToken").build()
            runCatching { execute(request) }
        }
        clearCredentials()
    }

    suspend fun clearCredentials() {
        credentials = null
        credentialStore.clear(server)
    }

    // endregion

    fun url(path: String, query: Query = emptyList()): HttpUrl =
        server.apiBaseUrl.newBuilder().apply {
            path.trim('/').takeIf { it.isNotEmpty() }?.let { addPathSegments(it) }
            query.forEach { (name, value) -> addQueryParameter(name, value) }
        }.build()

    fun <T> decode(deserializer: DeserializationStrategy<T>, text: String): T =
        try {
            json.decodeFromString(deserializer, text)
        } catch (error: SerializationException) {
            throw ApiError.Decoding(error.toString())
        } catch (error: IllegalArgumentException) {
            throw ApiError.Decoding(error.toString())
        }

    // region Intern

    private suspend fun perform(
        method: HttpMethod,
        path: String,
        query: Query = emptyList(),
        body: RequestBody? = null,
        authenticated: Boolean = true,
        isRetry: Boolean = false,
    ): String {
        val current = credentials
        if (authenticated && current != null && !current.isAccessTokenFresh && current.refreshToken != null) {
            refresh(staleToken = current.accessToken)
        }

        val request = makeRequest(method, path, query, body, authenticated)
        val response = execute(request)

        return when {
            response.status in 200..299 ->
                // 204 / leerer Body (z. B. keine MOTD) wird als JSON-null dekodierbar.
                response.body.ifEmpty { "null" }
            response.status == 401 && authenticated && !isRetry && credentials?.refreshToken != null -> {
                refresh(staleToken = request.header("Authorization")?.removePrefix("Bearer "))
                perform(method, path, query, body, authenticated = true, isRetry = true)
            }
            response.status == 401 -> {
                if (authenticated) expireSession()
                throw ApiError.Unauthorized
            }
            response.status == 403 -> throw ApiError.Forbidden
            response.status == 404 -> throw ApiError.NotFound
            else -> throw ApiError.Server(response.status, ApiError.message(response.body))
        }
    }

    /** Erneuert das Access-Token höchstens einmal gleichzeitig; parallele Aufrufe warten auf das Ergebnis. */
    private suspend fun refresh(staleToken: String?) = refreshMutex.withLock {
        val current = credentials
        if (current != null && current.accessToken != staleToken && current.isAccessTokenFresh) return@withLock
        performRefresh()
    }

    private suspend fun performRefresh() {
        val refreshToken = credentials?.refreshToken ?: run {
            expireSession()
            throw ApiError.Unauthorized
        }
        val request = makeRequest(HttpMethod.POST, "auth/refresh", emptyList(), EMPTY_BODY, authenticated = false)
            .newBuilder().header("Cookie", "refresh_token=$refreshToken").build()
        val response = execute(request)
        if (response.status != 200) {
            if (response.status == 401) expireSession()
            throw ApiError.Unauthorized
        }
        storeTokens(response, previousRefreshToken = refreshToken)
    }

    private suspend fun storeTokens(response: RawResponse, previousRefreshToken: String?) {
        val token = decode(TokenResponse.serializer(), response.body)
        val refreshCookie = Cookie.parseAll(response.url, response.headers).firstOrNull { it.name == "refresh_token" }?.value
        val updated = Credentials(
            accessToken = token.token,
            accessTokenExpiresAt = token.expiresAt.toEpochMilli(),
            refreshToken = refreshCookie ?: previousRefreshToken,
        )
        credentials = updated
        credentialStore.save(server, updated)
    }

    private suspend fun expireSession() {
        if (credentials == null) return
        clearCredentials()
        onSessionExpired?.invoke()
    }

    private fun makeRequest(
        method: HttpMethod,
        path: String,
        query: Query,
        body: RequestBody?,
        authenticated: Boolean,
    ): Request = Request.Builder()
        .url(url(path, query))
        .method(method.name, body)
        .header("Accept", "application/json")
        .apply {
            val token = credentials?.accessToken
            if (authenticated && token != null) header("Authorization", "Bearer $token")
        }
        .build()

    private suspend fun execute(request: Request): RawResponse =
        try {
            http.newCall(request).await().use { response ->
                RawResponse(response.code, response.body.string(), response.headers, response.request.url)
            }
        } catch (error: Exception) {
            throw ApiError.from(error)
        }

    private class RawResponse(val status: Int, val body: String, val headers: Headers, val url: HttpUrl)

    private companion object {
        val JSON = "application/json".toMediaType()
        val EMPTY_BODY = ByteArray(0).toRequestBody(null)
    }

    // endregion
}

private suspend fun Call.await(): Response = suspendCancellableCoroutine { continuation ->
    enqueue(object : Callback {
        override fun onResponse(call: Call, response: Response) = continuation.resume(response) { _, value, _ -> value.close() }
        override fun onFailure(call: Call, e: IOException) = continuation.resumeWithException(e)
    })
    continuation.invokeOnCancellation { cancel() }
}

@Serializable
private data class LoginBody(val email: String, val password: String)

@Serializable
private data class GoogleExchangeBody(val code: String, val codeVerifier: String)

@Serializable
private data class TokenResponse(val token: String, val expiresAt: ApiInstant)
