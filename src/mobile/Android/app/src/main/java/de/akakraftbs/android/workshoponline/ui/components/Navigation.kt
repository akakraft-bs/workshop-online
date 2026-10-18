package de.akakraftbs.android.workshoponline.ui.components

import androidx.compose.runtime.compositionLocalOf
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.setValue
import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import androidx.navigation.NavController
import de.akakraftbs.android.workshoponline.core.auth.AppSession
import de.akakraftbs.android.workshoponline.core.cache.ActionRunner
import de.akakraftbs.android.workshoponline.core.cache.Remote
import de.akakraftbs.android.workshoponline.core.network.ApiClient
import de.akakraftbs.android.workshoponline.core.network.JsonCoding
import de.akakraftbs.android.workshoponline.model.Permissions
import kotlinx.coroutines.async
import kotlinx.coroutines.awaitAll
import kotlinx.coroutines.coroutineScope
import kotlinx.coroutines.launch

/** Navigation für alle Bildschirme der App-Shell, ohne den NavController durchreichen zu müssen. */
class Navigator(private val navController: NavController) {
    fun open(route: Any) = navController.navigate(route) { launchSingleTop = true }

    fun back() {
        navController.popBackStack()
    }

    /** Schließt z. B. Formular *und* Detailseite nach dem Bearbeiten. */
    fun back(count: Int) {
        repeat(count) { navController.popBackStack() }
    }
}

val LocalNavigator = compositionLocalOf<Navigator> { error("Kein Navigator gesetzt") }

/** Objekte als Routen-Argument (JSON), damit Detailseiten sofort und auch nach Prozess-Neustart Inhalt haben. */
inline fun <reified T> T.toArg(): String = JsonCoding.json.encodeToString(this)

inline fun <reified T> String.fromArg(): T = JsonCoding.json.decodeFromString(this)

/**
 * Basis für die ViewModels der Fachbereiche – Gegenstück zu `@State Remote` + `ActionRunner` auf iOS.
 *
 * - [reloadAll] lädt alle Ressourcen der Seite (cache-first). Bildschirme rufen [reload] bei jedem Anzeigen auf,
 *   dadurch sind Listen nach Änderungen auf Unterseiten automatisch aktuell.
 * - [act] führt eine schreibende Aktion aus; Fehler erscheinen über [action] als Dialog.
 */
abstract class FeatureViewModel(protected val session: AppSession) : ViewModel() {
    val action = ActionRunner()
    var isRefreshing by mutableStateOf(false)
        private set

    val permissions: Permissions get() = session.permissions

    protected open suspend fun reloadAll() = Unit

    fun reload() {
        viewModelScope.launch { reloadAll() }
    }

    fun refresh() {
        viewModelScope.launch {
            isRefreshing = true
            reloadAll()
            isRefreshing = false
        }
    }

    protected suspend fun loadAll(vararg remotes: Remote<*>) = coroutineScope {
        remotes.map { async { it.load(session) } }.awaitAll()
    }

    fun act(onSuccess: suspend () -> Unit = {}, work: suspend ApiClient.() -> Unit) {
        viewModelScope.launch {
            if (action.run { session.client.work() }) onSuccess()
        }
    }

    fun webUrl(route: String): String = session.server.value.webUrl(route)
}
