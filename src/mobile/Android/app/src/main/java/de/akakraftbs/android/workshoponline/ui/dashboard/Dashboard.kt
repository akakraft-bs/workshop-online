package de.akakraftbs.android.workshoponline.ui.dashboard

import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.lazy.items
import androidx.compose.foundation.selection.selectable
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.outlined.Campaign
import androidx.compose.material.icons.automirrored.outlined.DirectionsWalk
import androidx.compose.material.icons.outlined.ErrorOutline
import androidx.compose.material.icons.outlined.ExpandMore
import androidx.compose.material.icons.outlined.Info
import androidx.compose.material.icons.automirrored.outlined.Logout
import androidx.compose.material.icons.outlined.PanTool
import androidx.compose.material.icons.outlined.Report
import androidx.compose.material.icons.outlined.WarningAmber
import androidx.compose.material.icons.outlined.Warehouse
import androidx.compose.material3.AlertDialog
import androidx.compose.material3.Button
import androidx.compose.material3.Card
import androidx.compose.material3.CardDefaults
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButton
import androidx.compose.material3.ListItem
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.OutlinedButton
import androidx.compose.material3.OutlinedTextField
import androidx.compose.material3.RadioButton
import androidx.compose.material3.Text
import androidx.compose.material3.TextButton
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.saveable.rememberSaveable
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.rotate
import androidx.compose.ui.graphics.vector.ImageVector
import androidx.compose.ui.semantics.Role
import androidx.compose.ui.unit.dp
import androidx.hilt.lifecycle.viewmodel.compose.hiltViewModel
import androidx.lifecycle.SavedStateHandle
import androidx.navigation.NavGraphBuilder
import androidx.navigation.compose.composable
import androidx.navigation.toRoute
import dagger.hilt.android.lifecycle.HiltViewModel
import de.akakraftbs.android.workshoponline.core.auth.AppSession
import de.akakraftbs.android.workshoponline.core.cache.remote
import de.akakraftbs.android.workshoponline.core.network.HttpMethod
import de.akakraftbs.android.workshoponline.model.CalendarEvent
import de.akakraftbs.android.workshoponline.model.HallenCheck
import de.akakraftbs.android.workshoponline.model.Mangel
import de.akakraftbs.android.workshoponline.model.Motd
import de.akakraftbs.android.workshoponline.model.MotdSeverity
import de.akakraftbs.android.workshoponline.model.Tone
import de.akakraftbs.android.workshoponline.model.Umfrage
import de.akakraftbs.android.workshoponline.model.Verbrauchsmaterial
import de.akakraftbs.android.workshoponline.model.nonEmpty
import de.akakraftbs.android.workshoponline.ui.components.ActionErrorDialog
import de.akakraftbs.android.workshoponline.ui.components.AppScaffold
import de.akakraftbs.android.workshoponline.ui.components.Avatar
import de.akakraftbs.android.workshoponline.ui.components.DateBadge
import de.akakraftbs.android.workshoponline.ui.components.FeatureViewModel
import de.akakraftbs.android.workshoponline.ui.components.FormScaffold
import de.akakraftbs.android.workshoponline.ui.components.FormTextField
import de.akakraftbs.android.workshoponline.ui.components.LastUpdatedFooter
import de.akakraftbs.android.workshoponline.ui.components.LocalIsOnline
import de.akakraftbs.android.workshoponline.ui.components.LocalNavigator
import de.akakraftbs.android.workshoponline.ui.components.RefreshableList
import de.akakraftbs.android.workshoponline.ui.components.SectionHeader
import de.akakraftbs.android.workshoponline.ui.components.fromArg
import de.akakraftbs.android.workshoponline.ui.components.hexColor
import de.akakraftbs.android.workshoponline.ui.components.relativeString
import de.akakraftbs.android.workshoponline.ui.components.timeString
import de.akakraftbs.android.workshoponline.ui.components.toArg
import de.akakraftbs.android.workshoponline.ui.components.toneColor
import de.akakraftbs.android.workshoponline.ui.gemeinschaft.UmfrageDetailRoute
import de.akakraftbs.android.workshoponline.ui.gemeinschaft.UmfrageRow
import de.akakraftbs.android.workshoponline.ui.halle.CalendarEventRoute
import de.akakraftbs.android.workshoponline.ui.halle.timeText
import de.akakraftbs.android.workshoponline.ui.inventar.MaterialDetailRoute
import de.akakraftbs.android.workshoponline.ui.inventar.MaterialRow
import de.akakraftbs.android.workshoponline.ui.maengel.MangelDetailRoute
import de.akakraftbs.android.workshoponline.ui.maengel.MangelRow
import kotlinx.serialization.Serializable
import java.time.LocalDateTime
import javax.inject.Inject

@Serializable data class MotdEditorRoute(val current: String? = null)

fun NavGraphBuilder.dashboardDestinations() {
    composable<MotdEditorRoute> { MotdEditorScreen() }
}

val MotdSeverity.icon: ImageVector
    get() = when (this) {
        MotdSeverity.Warning -> Icons.Outlined.WarningAmber
        MotdSeverity.Critical -> Icons.Outlined.Report
        else -> Icons.Outlined.Info
    }

/** Begrüßungen je Tageszeit – bewusst kurz, damit sie mit Namen in die Kopfzeile passen. */
private val salutations = listOf(
    5 until 11 to listOf("Guten Morgen", "Moin", "Morgen", "Frisch ans Werk"),
    11 until 18 to listOf("Hallo", "Moin", "Servus", "Hey"),
    18 until 23 to listOf("Guten Abend", "N’Abend", "Moin", "Schönen Abend"),
)
private val nightSalutations = listOf("Noch wach", "Nachtschicht", "Späte Schicht")

/** Wählt je Tageszeit eine Begrüßung; sie wechselt täglich, bleibt innerhalb eines Tages aber stabil. */
fun salutation(now: LocalDateTime = LocalDateTime.now()): String {
    val texts = salutations.firstOrNull { now.hour in it.first }?.second ?: nightSalutations
    return texts[(now.toLocalDate().toEpochDay() % texts.size).toInt()]
}

@HiltViewModel
class DashboardViewModel @Inject constructor(session: AppSession) : FeatureViewModel(session) {
    val motd = remote<Motd?>("motd")
    val anwesend = remote<List<HallenCheck>>("halle/anwesend")
    val umfragen = remote<List<Umfrage>>("umfrage")
    val events = remote<List<CalendarEvent>>("calendar/upcoming-events")
    val material = remote<List<Verbrauchsmaterial>>("verbrauchsmaterial")
    val maengel = remote<List<Mangel>>("mangel")
    val user = session.user

    var lowStockExpanded by mutableStateOf(false)
    var maengelExpanded by mutableStateOf(false)

    override suspend fun reloadAll() {
        loadAll(motd, anwesend, umfragen, events, material, maengel)
    }

    fun greeting(): String {
        val firstName = session.user.value?.shownName?.split(" ")?.firstOrNull().orEmpty()
        return if (firstName.isEmpty()) "Übersicht" else "${salutation()}, $firstName"
    }

    @Serializable private data class CheckinBody(val message: String?)

    fun checkIn(message: String) = act(onSuccess = { anwesend.load(session) }) {
        send(HttpMethod.POST, "halle/checkin", CheckinBody(message.nonEmpty))
    }

    fun checkOut() = act(onSuccess = { anwesend.load(session) }) { send(HttpMethod.DELETE, "halle/checkin") }
}

@Composable
fun DashboardScreen(viewModel: DashboardViewModel = hiltViewModel()) {
    val nav = LocalNavigator.current
    val isOnline = LocalIsOnline.current
    val permissions = viewModel.permissions
    var showCheckin by rememberSaveable { mutableStateOf(false) }
    LaunchedEffect(Unit) { viewModel.reload() }

    AppScaffold(
        title = viewModel.greeting(),
        actions = {
            if (permissions.isPrivileged) {
                IconButton(onClick = { nav.open(MotdEditorRoute(viewModel.motd.value?.toArg())) }, enabled = isOnline) {
                    Icon(Icons.Outlined.Campaign, contentDescription = "Nachricht des Tages")
                }
            }
        },
    ) { padding ->
        RefreshableList(padding, viewModel.isRefreshing, viewModel::refresh) {
            viewModel.motd.value?.let { motd -> item(key = "motd") { MotdCard(motd) } }

            // An der Halle
            val present = viewModel.anwesend.value.orEmpty()
            val mine = present.firstOrNull { permissions.isOwner(it.userId) }
            item(key = "halle-header") { SectionHeader("An der Halle") }
            if (present.isEmpty()) {
                item(key = "halle-empty") {
                    ListItem(
                        leadingContent = { Icon(Icons.Outlined.Warehouse, contentDescription = null) },
                        headlineContent = { Text("Gerade ist niemand eingecheckt.", color = MaterialTheme.colorScheme.onSurfaceVariant) },
                    )
                }
            }
            items(present, key = { "check-${it.id}" }) { check ->
                ListItem(
                    leadingContent = { Avatar(check.displayName, check.pictureUrl, size = 40.dp) },
                    headlineContent = { Text(check.displayName) },
                    supportingContent = {
                        Column {
                            check.message.nonEmpty?.let { Text(it, color = MaterialTheme.colorScheme.onSurface) }
                            Text("seit ${check.checkedInAt.timeString()} · bis ca. ${check.expiresAt.timeString()}")
                        }
                    },
                )
            }
            item(key = "halle-action") {
                Row(Modifier.padding(horizontal = 16.dp, vertical = 4.dp)) {
                    if (mine != null) {
                        OutlinedButton(onClick = viewModel::checkOut, enabled = isOnline) {
                            Icon(Icons.AutoMirrored.Outlined.Logout, contentDescription = null, modifier = Modifier.size(18.dp))
                            Text("  Ich bin wieder weg")
                        }
                    } else {
                        Button(onClick = { showCheckin = true }, enabled = isOnline) {
                            Icon(Icons.AutoMirrored.Outlined.DirectionsWalk, contentDescription = null, modifier = Modifier.size(18.dp))
                            Text("  Ich bin an der Halle")
                        }
                    }
                }
            }

            // Offene Umfragen
            val offeneUmfragen = viewModel.umfragen.value.orEmpty().filter { it.isOpen && !it.hasParticipated }
            if (offeneUmfragen.isNotEmpty()) {
                item(key = "umfragen-header") {
                    Row(verticalAlignment = Alignment.CenterVertically) {
                        SectionHeader("Deine Stimme fehlt")
                        Icon(Icons.Outlined.PanTool, contentDescription = null, modifier = Modifier.size(16.dp).padding(top = 16.dp), tint = MaterialTheme.colorScheme.primary)
                    }
                }
                items(offeneUmfragen, key = { "umfrage-${it.id}" }) { umfrage ->
                    UmfrageRow(umfrage) { nav.open(UmfrageDetailRoute(umfrage.toArg())) }
                }
            }

            // Veranstaltungen
            val upcoming = viewModel.events.value.orEmpty().take(5)
            if (upcoming.isNotEmpty()) {
                item(key = "events-header") { SectionHeader("Nächste Veranstaltungen") }
                items(upcoming, key = { "event-${it.id}" }) { event ->
                    ListItem(
                        modifier = Modifier.clickable { nav.open(CalendarEventRoute(event.toArg())) },
                        leadingContent = {
                            event.start?.let { DateBadge(it, hexColor(event.calendarColor) ?: MaterialTheme.colorScheme.primary) }
                        },
                        headlineContent = { Text(event.title) },
                        supportingContent = { Text(event.timeText()) },
                    )
                }
            }

            // Bestand niedrig
            val lowStock = viewModel.material.value.orEmpty().filter { it.isLowStock }
            if (lowStock.isNotEmpty()) {
                item(key = "stock-header") {
                    ExpandableHeader("Bestand niedrig (${lowStock.size})", Tone.Orange, Icons.Outlined.WarningAmber, viewModel.lowStockExpanded) {
                        viewModel.lowStockExpanded = !viewModel.lowStockExpanded
                    }
                }
                if (viewModel.lowStockExpanded) {
                    items(lowStock, key = { "stock-${it.id}" }) { item ->
                        MaterialRow(item, null) { nav.open(MaterialDetailRoute(item.toArg())) }
                    }
                }
            }

            // Offene Mängel
            val offeneMaengel = viewModel.maengel.value.orEmpty().filter { it.status.isActive }.sortedByDescending { it.createdAt }
            if (offeneMaengel.isNotEmpty()) {
                item(key = "maengel-header") {
                    ExpandableHeader("Offene Mängel (${offeneMaengel.size})", Tone.Red, Icons.Outlined.ErrorOutline, viewModel.maengelExpanded) {
                        viewModel.maengelExpanded = !viewModel.maengelExpanded
                    }
                }
                if (viewModel.maengelExpanded) {
                    items(offeneMaengel.take(5), key = { "mangel-${it.id}" }) { mangel ->
                        MangelRow(mangel) { nav.open(MangelDetailRoute(mangel.toArg())) }
                    }
                }
            }

            item(key = "updated") { LastUpdatedFooter(viewModel.anwesend.lastUpdated ?: viewModel.umfragen.lastUpdated) }
        }
    }

    if (showCheckin) {
        CheckinDialog(onCheckin = viewModel::checkIn, onDismiss = { showCheckin = false })
    }
    ActionErrorDialog(viewModel.action)
}

@Composable
private fun MotdCard(motd: Motd) {
    val color = toneColor(motd.severity.tone)
    Card(
        modifier = Modifier.fillMaxWidth().padding(horizontal = 16.dp, vertical = 8.dp),
        colors = CardDefaults.cardColors(containerColor = color.copy(alpha = 0.12f)),
    ) {
        Row(Modifier.padding(16.dp), horizontalArrangement = Arrangement.spacedBy(12.dp)) {
            Icon(motd.severity.icon, contentDescription = motd.severity.label, tint = color)
            Column(verticalArrangement = Arrangement.spacedBy(4.dp)) {
                Text(motd.message)
                Text(motd.updatedAt.relativeString(), style = MaterialTheme.typography.bodySmall, color = MaterialTheme.colorScheme.onSurfaceVariant)
            }
        }
    }
}

@Composable
private fun ExpandableHeader(title: String, tone: Tone, icon: ImageVector, expanded: Boolean, onToggle: () -> Unit) {
    ListItem(
        modifier = Modifier.clickable(onClick = onToggle),
        leadingContent = { Icon(icon, contentDescription = null, tint = toneColor(tone)) },
        headlineContent = { Text(title, color = toneColor(tone), style = MaterialTheme.typography.titleSmall) },
        trailingContent = {
            Icon(Icons.Outlined.ExpandMore, contentDescription = if (expanded) "Einklappen" else "Aufklappen", modifier = Modifier.rotate(if (expanded) 180f else 0f))
        },
    )
}

@Composable
private fun CheckinDialog(onCheckin: (String) -> Unit, onDismiss: () -> Unit) {
    var message by rememberSaveable { mutableStateOf("") }
    AlertDialog(
        onDismissRequest = onDismiss,
        title = { Text("Einchecken") },
        text = {
            Column(verticalArrangement = Arrangement.spacedBy(8.dp)) {
                OutlinedTextField(
                    value = message,
                    onValueChange = { message = it },
                    label = { Text("Was machst du? (optional)") },
                    maxLines = 3,
                    modifier = Modifier.fillMaxWidth(),
                )
                Text(
                    "Andere Mitglieder sehen, dass du an der Halle bist. Der Check-in läuft automatisch ab.",
                    style = MaterialTheme.typography.bodySmall,
                    color = MaterialTheme.colorScheme.onSurfaceVariant,
                )
            }
        },
        confirmButton = { TextButton(onClick = { onDismiss(); onCheckin(message) }) { Text("Einchecken") } },
        dismissButton = { TextButton(onClick = onDismiss) { Text("Abbrechen") } },
    )
}

// region Nachricht des Tages

@HiltViewModel
class MotdEditorViewModel @Inject constructor(session: AppSession, saved: SavedStateHandle) : FeatureViewModel(session) {
    val current: Motd? = saved.toRoute<MotdEditorRoute>().current?.fromArg()
    var message by mutableStateOf(current?.message.orEmpty())
    var severity by mutableStateOf(current?.severity?.takeIf { it != MotdSeverity.Unknown } ?: MotdSeverity.Info)

    @Serializable private data class MotdBody(val message: String, val severity: MotdSeverity)

    fun publish(onDone: () -> Unit) = act(onSuccess = { onDone() }) {
        send(HttpMethod.PUT, "motd", MotdBody(message.trim(), severity))
    }

    fun remove(onDone: () -> Unit) = act(onSuccess = { onDone() }) { send(HttpMethod.DELETE, "motd") }
}

@Composable
private fun MotdEditorScreen(viewModel: MotdEditorViewModel = hiltViewModel()) {
    val nav = LocalNavigator.current
    val isOnline = LocalIsOnline.current
    FormScaffold(
        title = "Nachricht des Tages",
        onClose = nav::back,
        canSave = viewModel.message.isNotBlank(),
        onSave = { viewModel.publish(nav::back) },
        action = viewModel.action,
        saveLabel = "Veröffentlichen",
    ) {
        FormTextField(viewModel.message, { viewModel.message = it }, "Nachricht", minLines = 3)
        Text("Wichtigkeit", style = MaterialTheme.typography.titleSmall)
        MotdSeverity.selectable.forEach { severity ->
            Row(
                Modifier
                    .fillMaxWidth()
                    .selectable(selected = viewModel.severity == severity, role = Role.RadioButton) { viewModel.severity = severity }
                    .padding(vertical = 4.dp),
                verticalAlignment = Alignment.CenterVertically,
            ) {
                RadioButton(selected = viewModel.severity == severity, onClick = null)
                Icon(severity.icon, contentDescription = null, tint = toneColor(severity.tone), modifier = Modifier.padding(start = 12.dp))
                Text(severity.label, modifier = Modifier.padding(start = 8.dp))
            }
        }
        if (viewModel.current != null) {
            TextButton(onClick = { viewModel.remove(nav::back) }, enabled = isOnline) {
                Text("Nachricht entfernen", color = MaterialTheme.colorScheme.error)
            }
        }
    }
}

// endregion
