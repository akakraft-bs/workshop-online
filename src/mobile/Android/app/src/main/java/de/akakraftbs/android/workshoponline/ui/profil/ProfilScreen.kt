package de.akakraftbs.android.workshoponline.ui.profil

import android.text.format.Formatter
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.verticalScroll
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.automirrored.outlined.Logout
import androidx.compose.material.icons.automirrored.outlined.OpenInNew
import androidx.compose.material.icons.outlined.CloudDownload
import androidx.compose.material.icons.outlined.CloudOff
import androidx.compose.material.icons.outlined.DeleteOutline
import androidx.compose.material.icons.outlined.Dns
import androidx.compose.material.icons.outlined.Feedback
import androidx.compose.material3.AlertDialog
import androidx.compose.material3.HorizontalDivider
import androidx.compose.material3.Icon
import androidx.compose.material3.ListItem
import androidx.compose.material3.ListItemDefaults
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Switch
import androidx.compose.material3.Text
import androidx.compose.material3.TextButton
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.saveable.rememberSaveable
import androidx.compose.runtime.setValue
import androidx.compose.ui.Modifier
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.text.input.KeyboardCapitalization
import androidx.compose.ui.text.input.KeyboardType
import androidx.compose.ui.unit.dp
import androidx.hilt.lifecycle.viewmodel.compose.hiltViewModel
import androidx.lifecycle.compose.collectAsStateWithLifecycle
import de.akakraftbs.android.workshoponline.BuildConfig
import de.akakraftbs.android.workshoponline.core.auth.openInCustomTab
import de.akakraftbs.android.workshoponline.core.server.AppConfig
import de.akakraftbs.android.workshoponline.model.Role
import de.akakraftbs.android.workshoponline.ui.auth.ServerSelectionDialog
import de.akakraftbs.android.workshoponline.ui.components.ActionErrorDialog
import de.akakraftbs.android.workshoponline.ui.components.AppScaffold
import de.akakraftbs.android.workshoponline.ui.components.FormHint
import de.akakraftbs.android.workshoponline.ui.components.FormTextField
import de.akakraftbs.android.workshoponline.ui.components.Avatar
import de.akakraftbs.android.workshoponline.ui.components.LocalIsOnline
import de.akakraftbs.android.workshoponline.ui.components.SectionHeader
import de.akakraftbs.android.workshoponline.ui.components.dayTimeString
import de.akakraftbs.android.workshoponline.ui.halle.HallenGeofenceSettings

@Composable
fun ProfilScreen(onBack: () -> Unit, onFeedback: () -> Unit, viewModel: ProfilViewModel = hiltViewModel()) {
    val context = LocalContext.current
    val isOnline = LocalIsOnline.current
    val user by viewModel.user.collectAsStateWithLifecycle()
    val server by viewModel.server.collectAsStateWithLifecycle()
    val simulateOffline by viewModel.simulateOffline.collectAsStateWithLifecycle()
    val isSyncing by viewModel.isSyncing.collectAsStateWithLifecycle()
    val lastSync by viewModel.lastSync.collectAsStateWithLifecycle()
    val failedCount by viewModel.failedCount.collectAsStateWithLifecycle()
    var confirmLogout by rememberSaveable { mutableStateOf(false) }
    var confirmClear by rememberSaveable { mutableStateOf(false) }
    var showServerDialog by rememberSaveable { mutableStateOf(false) }

    LaunchedEffect(Unit) { viewModel.load() }

    AppScaffold(
        title = "Profil & Einstellungen",
        onBack = onBack,
        actions = {
            // Wie in den Android-Einstellungen üblich: „Sichern“ erscheint erst, wenn etwas geändert wurde.
            if (viewModel.hasChanges) {
                TextButton(onClick = viewModel::savePreferences, enabled = isOnline && !viewModel.action.isRunning) { Text("Sichern") }
            }
        },
    ) { padding ->
        Column(
            modifier = Modifier
                .fillMaxSize()
                .padding(top = padding.calculateTopPadding())
                .verticalScroll(rememberScrollState())
                .padding(bottom = 16.dp),
        ) {
            user?.let { current ->
                ListItem(
                    leadingContent = { Avatar(current.shownName, current.pictureUrl, size = 56.dp) },
                    headlineContent = { Text(current.shownName, style = MaterialTheme.typography.titleLarge) },
                    supportingContent = { Text(current.email) },
                )
                ListItem(
                    overlineContent = { Text("Rollen") },
                    headlineContent = {
                        Text(current.roles.filter { it != Role.None }.joinToString(", ") { it.label }.ifEmpty { "–" })
                    },
                )
            }

            SectionHeader("Profil")
            Column(Modifier.padding(horizontal = 16.dp), verticalArrangement = Arrangement.spacedBy(8.dp)) {
                FormTextField(viewModel.displayName, { viewModel.displayName = it }, "Anzeigename", capitalization = KeyboardCapitalization.Words)
                FormTextField(viewModel.phone, { viewModel.phone = it }, "Telefon", keyboardType = KeyboardType.Phone)
                FormTextField(viewModel.address, { viewModel.address = it }, "Adresse", minLines = 2)
                FormHint("Telefon und Adresse sind für Vorstandsmitglieder in der Vereinsinfo sichtbar.")
            }

            HallenGeofenceSettings()

            SectionHeader("Offline & Server")
            ListItem(overlineContent = { Text("Server") }, headlineContent = { Text(server.displayName) })
            ListItem(
                overlineContent = { Text("Status") },
                headlineContent = {
                    Text(
                        if (isOnline) "Online" else "Offline",
                        color = if (isOnline) MaterialTheme.colorScheme.primary else MaterialTheme.colorScheme.error,
                    )
                },
            )
            ListItem(
                overlineContent = { Text("Offline-Stand") },
                headlineContent = {
                    Text(
                        when {
                            isSyncing -> "Wird geladen …"
                            lastSync == null -> "Noch nicht geladen"
                            failedCount > 0 -> "${lastSync!!.dayTimeString()} ($failedCount Bereiche fehlgeschlagen)"
                            else -> lastSync!!.dayTimeString()
                        },
                    )
                },
            )
            ListItem(
                overlineContent = { Text("Gespeicherte Daten") },
                headlineContent = { Text(Formatter.formatShortFileSize(context, viewModel.cacheBytes)) },
            )
            ActionItem("Offline-Daten jetzt laden", Icons.Outlined.CloudDownload, enabled = isOnline && !isSyncing) {
                viewModel.syncNow()
            }
            ActionItem("Offline-Daten löschen", Icons.Outlined.DeleteOutline) { confirmClear = true }
            if (BuildConfig.DEBUG) {
                ListItem(
                    modifier = Modifier.clickable { viewModel.setSimulateOffline(!simulateOffline) },
                    leadingContent = { Icon(Icons.Outlined.CloudOff, contentDescription = null) },
                    headlineContent = { Text("Offline-Modus simulieren") },
                    trailingContent = { Switch(checked = simulateOffline, onCheckedChange = viewModel::setSimulateOffline) },
                )
            }
            if (AppConfig.allowsCustomServer) {
                ActionItem("Server wechseln", Icons.Outlined.Dns) { showServerDialog = true }
            }
            Text(
                "Beim Öffnen der App werden alle Bereiche samt Vorschaubildern geladen und bleiben offline lesbar – " +
                    "auch Seiten, die du noch nie geöffnet hast. Änderungen sind nur mit Verbindung möglich.",
                style = MaterialTheme.typography.bodySmall,
                color = MaterialTheme.colorScheme.onSurfaceVariant,
                modifier = Modifier.padding(horizontal = 16.dp, vertical = 8.dp),
            )

            HorizontalDivider(Modifier.padding(vertical = 8.dp))
            ActionItem("Feedback senden", Icons.Outlined.Feedback, onClick = onFeedback)
            ActionItem("Web-App öffnen", Icons.AutoMirrored.Outlined.OpenInNew) {
                context.openInCustomTab(server.webUrl(""))
            }
            ListItem(overlineContent = { Text("App-Version") }, headlineContent = { Text("${BuildConfig.VERSION_NAME} (${BuildConfig.VERSION_CODE})") })
            ListItem(overlineContent = { Text("Server-Version") }, headlineContent = { Text(viewModel.serverVersion ?: "–") })

            HorizontalDivider(Modifier.padding(vertical = 8.dp))
            ListItem(
                modifier = Modifier.clickable { confirmLogout = true },
                leadingContent = { Icon(Icons.AutoMirrored.Outlined.Logout, contentDescription = null) },
                headlineContent = { Text("Abmelden") },
                colors = ListItemDefaults.colors(
                    headlineColor = MaterialTheme.colorScheme.error,
                    leadingIconColor = MaterialTheme.colorScheme.error,
                ),
            )
        }
    }

    if (confirmLogout) {
        AlertDialog(
            onDismissRequest = { confirmLogout = false },
            title = { Text("Abmelden?") },
            text = { Text("Die offline gespeicherten Daten werden von diesem Gerät gelöscht.") },
            confirmButton = {
                TextButton(onClick = { confirmLogout = false; viewModel.logout() }) { Text("Abmelden") }
            },
            dismissButton = { TextButton(onClick = { confirmLogout = false }) { Text("Abbrechen") } },
        )
    }
    if (confirmClear) {
        AlertDialog(
            onDismissRequest = { confirmClear = false },
            title = { Text("Offline-Daten löschen?") },
            text = { Text("Gespeicherte Listen und Bilder werden entfernt und beim nächsten Online-Start neu geladen.") },
            confirmButton = {
                TextButton(onClick = { confirmClear = false; viewModel.clearOfflineData() }) { Text("Löschen") }
            },
            dismissButton = { TextButton(onClick = { confirmClear = false }) { Text("Abbrechen") } },
        )
    }
    if (showServerDialog) ServerSelectionDialog(onDismiss = { showServerDialog = false })
    ActionErrorDialog(viewModel.action)
}

@Composable
private fun ActionItem(
    title: String,
    icon: androidx.compose.ui.graphics.vector.ImageVector,
    enabled: Boolean = true,
    onClick: () -> Unit,
) {
    val alpha = if (enabled) 1f else 0.38f
    ListItem(
        modifier = Modifier.clickable(enabled = enabled, onClick = onClick),
        leadingContent = { Icon(icon, contentDescription = null) },
        headlineContent = { Text(title) },
        colors = ListItemDefaults.colors(
            headlineColor = MaterialTheme.colorScheme.onSurface.copy(alpha = alpha),
            leadingIconColor = MaterialTheme.colorScheme.onSurfaceVariant.copy(alpha = alpha),
        ),
    )
}
