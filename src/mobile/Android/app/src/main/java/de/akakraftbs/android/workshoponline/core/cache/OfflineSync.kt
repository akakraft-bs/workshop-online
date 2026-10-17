package de.akakraftbs.android.workshoponline.core.cache

import android.content.Context
import androidx.hilt.work.HiltWorker
import androidx.work.Constraints
import androidx.work.CoroutineWorker
import androidx.work.ExistingPeriodicWorkPolicy
import androidx.work.NetworkType
import androidx.work.PeriodicWorkRequestBuilder
import androidx.work.WorkManager
import androidx.work.WorkerParameters
import dagger.assisted.Assisted
import dagger.assisted.AssistedInject
import de.akakraftbs.android.workshoponline.core.auth.AppSession
import de.akakraftbs.android.workshoponline.core.auth.SessionState
import de.akakraftbs.android.workshoponline.core.network.ApiError
import de.akakraftbs.android.workshoponline.core.network.Query
import de.akakraftbs.android.workshoponline.model.Permissions
import kotlinx.coroutines.async
import kotlinx.coroutines.awaitAll
import kotlinx.coroutines.coroutineScope
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.launch
import kotlinx.coroutines.sync.Mutex
import kotlinx.coroutines.sync.Semaphore
import kotlinx.coroutines.sync.withPermit
import kotlinx.serialization.json.JsonArray
import kotlinx.serialization.json.JsonObject
import kotlinx.serialization.json.JsonPrimitive
import kotlinx.serialization.json.contentOrNull
import java.time.Duration
import java.time.Instant
import java.util.concurrent.ConcurrentLinkedQueue
import java.util.concurrent.TimeUnit
import javax.inject.Inject
import javax.inject.Singleton
import kotlin.coroutines.cancellation.CancellationException

/**
 * Lädt beim Start bzw. bei wiederkehrender Verbindung alle Listen in den Cache,
 * damit sie offline verfügbar sind – auch Seiten, die noch nie geöffnet wurden.
 * Bilder werden dabei nur als verkleinerte Vorschau gespeichert.
 */
@Singleton
class OfflineSync @Inject constructor(private val session: AppSession) {
    private val _isRunning = MutableStateFlow(false)
    val isRunning: StateFlow<Boolean> = _isRunning.asStateFlow()

    private val _lastCompleted = MutableStateFlow<Instant?>(null)
    val lastCompleted: StateFlow<Instant?> = _lastCompleted.asStateFlow()

    private val _failedCount = MutableStateFlow(0)
    val failedCount: StateFlow<Int> = _failedCount.asStateFlow()

    private val mutex = Mutex()

    /** Startet einen Durchlauf, wenn online und der letzte lange genug her ist. */
    suspend fun syncIfNeeded() {
        val last = _lastCompleted.value
        if (last != null && Duration.between(last, Instant.now()) < INTERVAL) return
        sync()
    }

    suspend fun sync() {
        if (session.state.value != SessionState.SignedIn || !session.connectivity.isOnline) return
        if (!mutex.tryLock()) return
        _isRunning.value = true
        try {
            val images = ConcurrentLinkedQueue<Pair<String?, String?>>()
            val results = coroutineScope {
                val prefetches = endpoints(session.permissions).map { (path, query) ->
                    async { attempt { session.prefetch(path, query) } }
                }
                val withImages = listOf(
                    async {
                        attempt {
                            list("werkzeug").forEach { images += it.string("thumbnailUrl") to it.string("imageUrl") }
                        }
                    },
                    async {
                        attempt {
                            list("verbrauchsmaterial").forEach { images += it.string("thumbnailUrl") to it.string("imageUrl") }
                        }
                    },
                    async {
                        attempt {
                            list("mangel")
                                .filter { it.string("status") in setOf("Offen", "Kenntnisgenommen") }
                                .forEach { images += null to it.string("imageUrl") }
                        }
                    },
                    async {
                        attempt { list("aufgaben").forEach { images += null to it.string("fotoUrl") } }
                    },
                )
                (prefetches + withImages).awaitAll()
            }

            storePreviews(images.filter { it.first != null || it.second != null })
            val failures = results.count { !it }
            _failedCount.value = failures
            if (failures == 0 || _lastCompleted.value == null) _lastCompleted.value = Instant.now()
        } finally {
            _isRunning.value = false
            mutex.unlock()
        }
    }

    // region Intern

    /** Lädt eine Liste roh (ohne Modell), damit nur die Bildfelder ausgelesen werden müssen. */
    private suspend fun list(path: String): List<JsonObject> =
        session.fetch(JsonArray.serializer(), path).value.filterIsInstance<JsonObject>()

    private fun JsonObject.string(key: String): String? =
        (this[key] as? JsonPrimitive)?.contentOrNull?.trim()?.takeIf { it.isNotEmpty() }

    private suspend fun attempt(work: suspend () -> Unit): Boolean =
        try {
            work()
            true
        } catch (error: CancellationException) {
            throw error
        } catch (error: ApiError) {
            false
        }

    /** Lädt Vorschaubilder mit begrenzter Parallelität; vorhandene werden übersprungen. */
    private suspend fun storePreviews(pairs: List<Pair<String?, String?>>) = coroutineScope {
        val limit = Semaphore(4)
        pairs.forEach { (thumbnail, original) ->
            val keys = listOfNotNull(thumbnail, original)
            val source = keys.firstOrNull() ?: return@forEach
            launch {
                limit.withPermit {
                    if (session.connectivity.isOnline) session.images.storePreview(source, keys)
                }
            }
        }
    }

    /** Alle lesenden Listen-Endpunkte; Pfad und Query müssen exakt denen der Ansichten entsprechen. */
    private fun endpoints(permissions: Permissions): List<Pair<String, Query>> {
        val month = Endpoints.currentMonth()
        val list = mutableListOf<Pair<String, Query>>(
            "users/me/badges" to emptyList(),
            "users/me/preferences" to emptyList(),
            "motd" to emptyList(),
            "halle/anwesend" to emptyList(),
            "calendar/upcoming-events" to emptyList(),
            "calendar/configs" to emptyList(),
            "storage-locations" to emptyList(),
            "verbrauchsmaterial/units" to emptyList(),
            "umfrage" to emptyList(),
            "wunsch" to emptyList(),
            "hallenbuch" to Endpoints.hallenbuchFirstPage,
            "fahrzeuge" to emptyList(),
            "parkplatz/overview" to emptyList(),
            "parkplatz/historie" to listOf("limit" to "100"),
            "verein/info" to emptyList(),
            "verein/dokumente" to emptyList(),
            "verein/zugaenge" to emptyList(),
            "verein/projekte" to emptyList(),
        )
        for (kind in Endpoints.CalendarKind.entries) {
            list += "calendar/events" to Endpoints.calendarEvents(month, kind)
            list += "calendar/events" to Endpoints.calendarEvents(month.plusMonths(1), kind)
        }
        if (permissions.canSeeVorstandsbereich) {
            list += "crm/partner" to emptyList()
            list += "users/assignable" to emptyList()
        }
        if (permissions.isAdmin) {
            list += "users" to emptyList()
            list += "admin/ablageorte" to emptyList()
        }
        return list
    }

    // endregion

    companion object {
        /** Mindestabstand zwischen zwei automatischen Durchläufen im Vordergrund. */
        val INTERVAL: Duration = Duration.ofMinutes(10)
    }
}

/** Hält den Offline-Stand auch ohne geöffnete App aktuell (nur mit Verbindung und ausreichend Akku). */
@HiltWorker
class OfflineSyncWorker @AssistedInject constructor(
    @Assisted context: Context,
    @Assisted params: WorkerParameters,
    private val session: AppSession,
    private val offlineSync: OfflineSync,
) : CoroutineWorker(context, params) {
    override suspend fun doWork(): Result {
        session.restore()
        offlineSync.sync()
        return Result.success()
    }

    companion object {
        private const val NAME = "offline-sync"

        fun schedule(workManager: WorkManager) {
            val request = PeriodicWorkRequestBuilder<OfflineSyncWorker>(4, TimeUnit.HOURS)
                .setConstraints(
                    Constraints.Builder()
                        .setRequiredNetworkType(NetworkType.CONNECTED)
                        .setRequiresBatteryNotLow(true)
                        .build(),
                )
                .build()
            workManager.enqueueUniquePeriodicWork(NAME, ExistingPeriodicWorkPolicy.KEEP, request)
        }

        fun cancel(workManager: WorkManager) {
            workManager.cancelUniqueWork(NAME)
        }
    }
}
