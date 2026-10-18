package de.akakraftbs.android.workshoponline.core.cache

import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.setValue
import de.akakraftbs.android.workshoponline.core.auth.AppSession
import de.akakraftbs.android.workshoponline.core.network.ApiError
import de.akakraftbs.android.workshoponline.core.network.Query
import kotlinx.serialization.DeserializationStrategy
import kotlinx.serialization.serializer
import java.time.Instant

/**
 * Eine vom Server geladene Ressource mit Offline-Fallback – gehört in ein ViewModel.
 *
 * [load] zeigt zuerst den Cache, lädt dann online nach. Offline bleibt der
 * zwischengespeicherte Stand sichtbar; [lastUpdated] nennt dessen Alter.
 * Der Zustand ist Compose-State, Änderungen lösen also direkt eine Recomposition aus.
 */
class Remote<T>(
    path: String,
    query: Query = emptyList(),
    private val deserializer: DeserializationStrategy<T>,
) {
    var value by mutableStateOf<T?>(null)
        private set
    var lastUpdated by mutableStateOf<Instant?>(null)
        private set
    var isLoading by mutableStateOf(false)
        private set
    var failure by mutableStateOf<ApiError?>(null)
        private set
    var path = path
        private set
    var query = query
        private set

    suspend fun load(session: AppSession) {
        if (value == null) {
            session.cached(deserializer, path, query)?.let {
                value = it.value
                lastUpdated = it.date
            }
        }
        isLoading = true
        try {
            val fresh = session.fetch(deserializer, path, query)
            value = fresh.value
            lastUpdated = fresh.date
            failure = null
        } catch (error: ApiError) {
            failure = if (error == ApiError.Offline && value != null) null else error
        } finally {
            isLoading = false
        }
    }

    /** Wechselt auf eine andere Abfrage (z. B. anderer Monat) und lädt neu. */
    suspend fun load(session: AppSession, path: String, query: Query) {
        if (path != this.path || query != this.query) {
            this.path = path
            this.query = query
            value = null
            lastUpdated = null
            failure = null
        }
        load(session)
    }

    /** Lokale Anpassung nach erfolgreicher Änderung, bis der nächste Abruf den Serverstand liefert. */
    fun update(transform: (T) -> T) {
        value = value?.let(transform)
    }
}

inline fun <reified T> remote(path: String, query: Query = emptyList()): Remote<T> =
    Remote(path, query, serializer<T>())

/** Führt schreibende Aktionen aus und stellt Fehler für einen Dialog bereit. */
class ActionRunner {
    var isRunning by mutableStateOf(false)
        private set
    var errorMessage by mutableStateOf<String?>(null)

    suspend fun run(work: suspend () -> Unit): Boolean {
        isRunning = true
        return try {
            work()
            true
        } catch (error: ApiError) {
            errorMessage = error.message
            false
        } finally {
            isRunning = false
        }
    }
}
