package de.akakraftbs.android.workshoponline.ui.shell

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import dagger.hilt.android.lifecycle.HiltViewModel
import de.akakraftbs.android.workshoponline.core.auth.AppSession
import de.akakraftbs.android.workshoponline.core.cache.remote
import de.akakraftbs.android.workshoponline.model.Badges
import de.akakraftbs.android.workshoponline.model.Permissions
import de.akakraftbs.android.workshoponline.model.User
import de.akakraftbs.android.workshoponline.model.withoutAccessCount
import kotlinx.coroutines.flow.SharingStarted
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.drop
import kotlinx.coroutines.flow.filter
import kotlinx.coroutines.flow.map
import kotlinx.coroutines.flow.stateIn
import kotlinx.coroutines.launch
import javax.inject.Inject

/** Zustand der App-Shell: Zähler für die Navigation und den Bereich „Mehr“. */
@HiltViewModel
class MainViewModel @Inject constructor(private val session: AppSession) : ViewModel() {
    val badges = remote<Badges>("users/me/badges")
    private val users = remote<List<User>>("users")

    val user: StateFlow<User?> = session.user
    val permissions: StateFlow<Permissions> = session.user
        .map { Permissions(it) }
        .stateIn(viewModelScope, SharingStarted.Eagerly, session.permissions)

    init {
        viewModelScope.launch {
            session.connectivity.isOnlineFlow.drop(1).filter { it }.collect { loadBadges() }
        }
    }

    /** Offene Freischaltungen – nur für Admins (der Endpunkt ist AdminOnly). */
    val usersWithoutAccess: Int
        get() = if (permissions.value.isAdmin) users.value?.withoutAccessCount ?: 0 else 0

    fun moreBadge(isCompact: Boolean): Int {
        val badges = badges.value ?: return 0
        return badges.pendingUmfragen + badges.openAufgaben +
            (if (permissions.value.isAdmin) badges.unseenFeedback else 0) +
            (if (isCompact) badges.openMaengel else 0) +
            usersWithoutAccess
    }

    fun webUrl(route: String): String = session.server.value.webUrl(route)

    fun loadBadges() {
        viewModelScope.launch {
            badges.load(session)
            if (session.permissions.isAdmin) users.load(session)
        }
    }
}
