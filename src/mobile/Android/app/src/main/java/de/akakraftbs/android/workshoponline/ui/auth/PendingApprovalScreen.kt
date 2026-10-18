package de.akakraftbs.android.workshoponline.ui.auth

import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.layout.widthIn
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.outlined.HourglassTop
import androidx.compose.material3.Button
import androidx.compose.material3.Icon
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Scaffold
import androidx.compose.material3.Text
import androidx.compose.material3.TextButton
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.unit.dp
import androidx.hilt.lifecycle.viewmodel.compose.hiltViewModel
import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import dagger.hilt.android.lifecycle.HiltViewModel
import de.akakraftbs.android.workshoponline.core.auth.AppSession
import de.akakraftbs.android.workshoponline.ui.components.LocalIsOnline
import kotlinx.coroutines.launch
import javax.inject.Inject

@HiltViewModel
class PendingApprovalViewModel @Inject constructor(private val session: AppSession) : ViewModel() {
    var isChecking by mutableStateOf(false)
        private set

    fun check() {
        viewModelScope.launch {
            isChecking = true
            session.refreshUser()
            isChecking = false
        }
    }

    fun logout() {
        viewModelScope.launch { session.logout() }
    }
}

/** Konto angelegt, aber noch ohne Rolle. Wird beim Zurückkehren in die App automatisch neu geprüft. */
@Composable
fun PendingApprovalScreen(viewModel: PendingApprovalViewModel = hiltViewModel()) {
    Scaffold { padding ->
        Column(
            modifier = Modifier.fillMaxSize().padding(padding).padding(32.dp),
            horizontalAlignment = Alignment.CenterHorizontally,
            verticalArrangement = Arrangement.spacedBy(16.dp, Alignment.CenterVertically),
        ) {
            Icon(
                Icons.Outlined.HourglassTop,
                contentDescription = null,
                modifier = Modifier.size(56.dp),
                tint = MaterialTheme.colorScheme.primary,
            )
            Text("Freischaltung ausstehend", style = MaterialTheme.typography.headlineSmall, textAlign = TextAlign.Center)
            Text(
                "Dein Konto ist angelegt. Sobald der Vorstand es freigeschaltet hat, kannst du AkaKraft Online nutzen.",
                style = MaterialTheme.typography.bodyMedium,
                color = MaterialTheme.colorScheme.onSurfaceVariant,
                textAlign = TextAlign.Center,
                modifier = Modifier.widthIn(max = 420.dp),
            )
            Button(onClick = viewModel::check, enabled = !viewModel.isChecking && LocalIsOnline.current) {
                Text("Erneut prüfen")
            }
            TextButton(onClick = viewModel::logout) { Text("Abmelden") }
        }
    }
}
