package de.akakraftbs.android.workshoponline

import android.app.Application
import android.content.Context
import androidx.hilt.work.HiltWorkerFactory
import androidx.lifecycle.Lifecycle
import androidx.lifecycle.ProcessLifecycleOwner
import androidx.lifecycle.lifecycleScope
import androidx.lifecycle.repeatOnLifecycle
import androidx.work.Configuration
import androidx.work.WorkManager
import coil3.ImageLoader
import coil3.PlatformContext
import coil3.SingletonImageLoader
import dagger.hilt.android.HiltAndroidApp
import de.akakraftbs.android.workshoponline.core.auth.AppSession
import de.akakraftbs.android.workshoponline.core.auth.SessionState
import de.akakraftbs.android.workshoponline.core.cache.OfflineSync
import de.akakraftbs.android.workshoponline.core.cache.OfflineSyncWorker
import de.akakraftbs.android.workshoponline.core.location.HallenGeofence
import kotlinx.coroutines.flow.distinctUntilChanged
import kotlinx.coroutines.flow.drop
import kotlinx.coroutines.flow.filter
import kotlinx.coroutines.launch
import javax.inject.Inject

@HiltAndroidApp
class AkakraftApplication : Application(), Configuration.Provider, SingletonImageLoader.Factory {
    @Inject lateinit var workerFactory: HiltWorkerFactory
    @Inject lateinit var imageLoader: ImageLoader
    @Inject lateinit var session: AppSession
    @Inject lateinit var offlineSync: OfflineSync
    @Inject lateinit var workManager: WorkManager
    @Inject lateinit var geofence: HallenGeofence

    override val workManagerConfiguration: Configuration
        get() = Configuration.Builder().setWorkerFactory(workerFactory).build()

    override fun newImageLoader(context: PlatformContext): ImageLoader = imageLoader

    override fun onCreate() {
        super.onCreate()
        HallenGeofence.ensureChannel(this)
        observeSession()
    }

    /**
     * Prozessweite Auslöser – entspricht `scenePhase`/`onChange` der iOS-App:
     * - Nach der Anmeldung: Offline-Daten laden und den Hintergrund-Sync planen.
     * - App kommt in den Vordergrund oder Verbindung kehrt zurück: Nutzer aktualisieren, ggf. Sync.
     */
    private fun observeSession() {
        val scope = ProcessLifecycleOwner.get().lifecycleScope

        scope.launch {
            session.state.collect { state ->
                when (state) {
                    SessionState.SignedIn -> {
                        OfflineSyncWorker.schedule(workManager)
                        geofence.refreshStandort()
                        offlineSync.syncIfNeeded()
                    }
                    SessionState.SignedOut -> {
                        OfflineSyncWorker.cancel(workManager)
                        geofence.stop()
                    }
                    else -> Unit
                }
            }
        }

        scope.launch {
            ProcessLifecycleOwner.get().repeatOnLifecycle(Lifecycle.State.STARTED) {
                onForeground()
                session.connectivity.isOnlineFlow
                    .drop(1)
                    .distinctUntilChanged()
                    .filter { it }
                    .collect { onForeground() }
            }
        }
    }

    private suspend fun onForeground() {
        when (session.state.value) {
            SessionState.SignedIn -> {
                session.refreshUser()
                offlineSync.syncIfNeeded()
            }
            SessionState.PendingApproval -> session.refreshUser()
            else -> Unit
        }
    }
}

/** App-weite Singletons für Composables ohne eigenes ViewModel (z. B. [de.akakraftbs.android.workshoponline.ui.components.RemoteImage]). */
val Context.akakraftApp: AkakraftApplication get() = applicationContext as AkakraftApplication
