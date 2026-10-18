package de.akakraftbs.android.workshoponline.ui.profil

import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableLongStateOf
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.setValue
import androidx.lifecycle.viewModelScope
import dagger.hilt.android.lifecycle.HiltViewModel
import de.akakraftbs.android.workshoponline.core.auth.AppSession
import de.akakraftbs.android.workshoponline.core.cache.OfflineSync
import de.akakraftbs.android.workshoponline.core.cache.remote
import de.akakraftbs.android.workshoponline.core.network.HttpMethod
import de.akakraftbs.android.workshoponline.core.network.JsonCoding
import de.akakraftbs.android.workshoponline.model.UserPreferences
import de.akakraftbs.android.workshoponline.model.nonEmpty
import de.akakraftbs.android.workshoponline.ui.components.FeatureViewModel
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.launch
import kotlinx.coroutines.withContext
import kotlinx.serialization.Serializable
import okhttp3.OkHttpClient
import okhttp3.Request
import javax.inject.Inject

@HiltViewModel
class ProfilViewModel @Inject constructor(
    session: AppSession,
    private val offlineSync: OfflineSync,
    private val http: OkHttpClient,
) : FeatureViewModel(session) {
    val user = session.user
    val server = session.server
    val simulateOffline = session.connectivity.simulateOffline
    val isSyncing = offlineSync.isRunning
    val lastSync = offlineSync.lastCompleted
    val failedCount = offlineSync.failedCount

    private val preferences = remote<UserPreferences>("users/me/preferences")
    var displayName by mutableStateOf("")
    var phone by mutableStateOf("")
    var address by mutableStateOf("")
    private var loadedPreferences by mutableStateOf(false)

    var cacheBytes by mutableLongStateOf(0L)
        private set
    var serverVersion by mutableStateOf<String?>(null)
        private set

    val hasChanges: Boolean
        get() {
            val prefs = preferences.value ?: return false
            if (!loadedPreferences) return false
            return displayName.trim() != prefs.displayName.orEmpty().trim() ||
                phone.trim() != prefs.phone.orEmpty().trim() ||
                address.trim() != prefs.address.orEmpty().trim()
        }

    fun load() {
        refreshCacheSize()
        viewModelScope.launch {
            preferences.load(session)
            val prefs = preferences.value
            if (prefs != null && !loadedPreferences) {
                displayName = prefs.displayName.orEmpty()
                phone = prefs.phone.orEmpty()
                address = prefs.address.orEmpty()
                loadedPreferences = true
            }
        }
        viewModelScope.launch { serverVersion = loadServerVersion() }
    }

    fun savePreferences() {
        val input = UserPreferences(
            favoriteRoutes = preferences.value?.favoriteRoutes.orEmpty(),
            displayName = displayName.nonEmpty,
            phone = phone.nonEmpty,
            address = address.nonEmpty,
        )
        act(onSuccess = {
            preferences.update { input }
            session.refreshUser()
        }) { send(HttpMethod.PUT, "users/me/preferences", input) }
    }

    @Serializable private data class VersionInfo(val version: String)

    /**
     * Backend-Version über `GET /version`; ältere Backends ohne den Endpunkt liefern
     * stattdessen die Release-Version der Web-App (`version.json`) – wie in der iOS-App.
     */
    private suspend fun loadServerVersion(): String? {
        runCatching {
            val text = session.client.sendPublic(HttpMethod.GET, "version", null)
            return session.client.decode(VersionInfo.serializer(), text).version
        }
        if (!session.connectivity.isOnline) return null
        return withContext(Dispatchers.IO) {
            runCatching {
                val request = Request.Builder().url(session.server.value.webUrl("version.json")).build()
                http.newCall(request).execute().use { response ->
                    if (response.code != 200) return@use null
                    JsonCoding.json.decodeFromString(VersionInfo.serializer(), response.body.string()).version
                }
            }.getOrNull()
        }
    }

    fun refreshCacheSize() {
        viewModelScope.launch {
            cacheBytes = session.cache.totalBytes() + session.images.diskUsage()
        }
    }

    fun syncNow() {
        viewModelScope.launch {
            offlineSync.sync()
            refreshCacheSize()
        }
    }

    fun clearOfflineData() {
        viewModelScope.launch {
            session.cache.removeEverything()
            session.images.clear()
            refreshCacheSize()
        }
    }

    fun setSimulateOffline(enabled: Boolean) = session.connectivity.setSimulateOffline(enabled)

    fun logout() {
        viewModelScope.launch { session.logout() }
    }
}
