package de.akakraftbs.android.workshoponline.ui

import androidx.compose.animation.Crossfade
import androidx.compose.runtime.Composable
import androidx.compose.runtime.CompositionLocalProvider
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.getValue
import androidx.hilt.lifecycle.viewmodel.compose.hiltViewModel
import androidx.lifecycle.ViewModel
import androidx.lifecycle.compose.collectAsStateWithLifecycle
import dagger.hilt.android.lifecycle.HiltViewModel
import de.akakraftbs.android.workshoponline.core.auth.AppSession
import de.akakraftbs.android.workshoponline.core.auth.SessionState
import de.akakraftbs.android.workshoponline.ui.auth.LoginScreen
import de.akakraftbs.android.workshoponline.ui.auth.PendingApprovalScreen
import de.akakraftbs.android.workshoponline.ui.components.LoadingState
import de.akakraftbs.android.workshoponline.ui.components.LocalIsOnline
import de.akakraftbs.android.workshoponline.ui.shell.MainScreen
import javax.inject.Inject

@HiltViewModel
class RootViewModel @Inject constructor(private val session: AppSession) : ViewModel() {
    val state = session.state
    val isOnline = session.connectivity.isOnlineFlow

    suspend fun restore() = session.restore()
}

/** Root-Routing: Login / Freischaltung ausstehend / App. */
@Composable
fun RootScreen(viewModel: RootViewModel = hiltViewModel()) {
    val state by viewModel.state.collectAsStateWithLifecycle()
    val isOnline by viewModel.isOnline.collectAsStateWithLifecycle()

    LaunchedEffect(Unit) { viewModel.restore() }

    CompositionLocalProvider(LocalIsOnline provides isOnline) {
        Crossfade(targetState = state, label = "session") { current ->
            when (current) {
                SessionState.Launching -> LoadingState()
                SessionState.SignedOut -> LoginScreen()
                SessionState.PendingApproval -> PendingApprovalScreen()
                SessionState.SignedIn -> MainScreen()
            }
        }
    }
}
