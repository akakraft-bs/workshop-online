package de.akakraftbs.android.workshoponline.ui.profil

import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableLongStateOf
import androidx.compose.runtime.setValue
import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import dagger.hilt.android.lifecycle.HiltViewModel
import de.akakraftbs.android.workshoponline.core.auth.AppSession
import de.akakraftbs.android.workshoponline.core.cache.OfflineSync
import kotlinx.coroutines.launch
import javax.inject.Inject

@HiltViewModel
class ProfilViewModel @Inject constructor(
    private val session: AppSession,
    private val offlineSync: OfflineSync,
) : ViewModel() {
    val user = session.user
    val server = session.server
    val simulateOffline = session.connectivity.simulateOffline
    val isSyncing = offlineSync.isRunning
    val lastSync = offlineSync.lastCompleted
    val failedCount = offlineSync.failedCount

    var cacheBytes by mutableLongStateOf(0L)
        private set

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
