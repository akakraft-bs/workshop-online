package de.akakraftbs.android.workshoponline.ui.verwaltung

import androidx.compose.foundation.BorderStroke
import androidx.compose.foundation.background
import androidx.compose.foundation.border
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.FlowRow
import androidx.compose.foundation.layout.PaddingValues
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.lazy.items
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.foundation.text.selection.SelectionContainer
import androidx.compose.foundation.verticalScroll
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.automirrored.outlined.OpenInNew
import androidx.compose.material.icons.outlined.Build
import androidx.compose.material.icons.outlined.Check
import androidx.compose.material.icons.outlined.DeleteOutline
import androidx.compose.material.icons.outlined.Feedback
import androidx.compose.material.icons.outlined.ManageAccounts
import androidx.compose.material.icons.outlined.NotificationsActive
import androidx.compose.material.icons.outlined.Photo
import androidx.compose.material.icons.outlined.Place
import androidx.compose.material.icons.outlined.Inbox
import androidx.compose.material.icons.outlined.TextFields
import androidx.compose.material3.Badge
import androidx.compose.material3.Button
import androidx.compose.material3.CircularProgressIndicator
import androidx.compose.material3.DropdownMenu
import androidx.compose.material3.DropdownMenuItem
import androidx.compose.material3.FilterChip
import androidx.compose.material3.Icon
import androidx.compose.material3.ListItem
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.OutlinedButton
import androidx.compose.material3.Switch
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateListOf
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.saveable.rememberSaveable
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.text.font.FontFamily
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.hilt.lifecycle.viewmodel.compose.hiltViewModel
import androidx.lifecycle.SavedStateHandle
import androidx.lifecycle.viewModelScope
import androidx.navigation.NavGraphBuilder
import androidx.navigation.compose.composable
import androidx.navigation.toRoute
import dagger.hilt.android.lifecycle.HiltViewModel
import de.akakraftbs.android.workshoponline.core.auth.AppSession
import de.akakraftbs.android.workshoponline.core.cache.remote
import de.akakraftbs.android.workshoponline.core.network.HttpMethod
import de.akakraftbs.android.workshoponline.model.AblageortOverview
import de.akakraftbs.android.workshoponline.model.Feedback
import de.akakraftbs.android.workshoponline.model.FeedbackStatus
import de.akakraftbs.android.workshoponline.model.Permissions
import de.akakraftbs.android.workshoponline.model.Role
import de.akakraftbs.android.workshoponline.model.Tone
import de.akakraftbs.android.workshoponline.model.User
import de.akakraftbs.android.workshoponline.model.nonEmpty
import de.akakraftbs.android.workshoponline.model.withoutAccessCount
import de.akakraftbs.android.workshoponline.ui.components.ActionErrorDialog
import de.akakraftbs.android.workshoponline.ui.components.AppScaffold
import de.akakraftbs.android.workshoponline.ui.components.Avatar
import de.akakraftbs.android.workshoponline.ui.components.ConfirmDialog
import de.akakraftbs.android.workshoponline.ui.components.DropdownField
import de.akakraftbs.android.workshoponline.ui.components.FeatureViewModel
import de.akakraftbs.android.workshoponline.ui.components.FormHint
import de.akakraftbs.android.workshoponline.ui.components.FormScaffold
import de.akakraftbs.android.workshoponline.ui.components.FormTextField
import de.akakraftbs.android.workshoponline.ui.components.LocalIsOnline
import de.akakraftbs.android.workshoponline.ui.components.LocalNavigator
import de.akakraftbs.android.workshoponline.ui.components.MenuAction
import de.akakraftbs.android.workshoponline.ui.components.OverflowMenu
import de.akakraftbs.android.workshoponline.ui.components.RemoteList
import de.akakraftbs.android.workshoponline.ui.components.SectionHeader
import de.akakraftbs.android.workshoponline.ui.components.StatusPill
import de.akakraftbs.android.workshoponline.ui.components.SwitchRow
import de.akakraftbs.android.workshoponline.ui.components.dayString
import de.akakraftbs.android.workshoponline.ui.components.dayTimeString
import de.akakraftbs.android.workshoponline.ui.components.emptyItem
import de.akakraftbs.android.workshoponline.ui.components.fromArg
import de.akakraftbs.android.workshoponline.ui.components.hexColor
import de.akakraftbs.android.workshoponline.ui.components.openLink
import de.akakraftbs.android.workshoponline.ui.components.toArg
import de.akakraftbs.android.workshoponline.ui.components.toneColor
import de.akakraftbs.android.workshoponline.ui.inventar.SearchField
import de.akakraftbs.android.workshoponline.ui.inventar.StorageLocationLabel
import kotlinx.coroutines.launch
import kotlinx.serialization.Serializable
import java.text.Collator
import javax.inject.Inject

@Serializable data object AdminRoute
@Serializable data object AdminUsersRoute
@Serializable data class AdminUserDetailRoute(val user: String)
@Serializable data object AdminFeedbackRoute
@Serializable data object AblageorteRoute
@Serializable data class AblageortEditRoute(val ort: String)
@Serializable data object PushTestRoute
@Serializable data object WartungRoute

fun NavGraphBuilder.adminDestinations() {
    composable<AdminRoute> { AdminScreen() }
    composable<AdminUsersRoute> { AdminUsersScreen() }
    composable<AdminUserDetailRoute> { AdminUserDetailScreen() }
    composable<AdminFeedbackRoute> { AdminFeedbackScreen() }
    composable<AblageorteRoute> { AblageorteScreen() }
    composable<AblageortEditRoute> { AblageortEditScreen() }
    composable<PushTestRoute> { PushTestScreen() }
    composable<WartungRoute> { WartungScreen() }
}

private val collator: Collator = Collator.getInstance()

// region Übersicht

@HiltViewModel
class AdminViewModel @Inject constructor(session: AppSession) : FeatureViewModel(session) {
    val users = remote<List<User>>("users")
    override suspend fun reloadAll() = users.load(session)
}

@Composable
private fun AdminScreen(viewModel: AdminViewModel = hiltViewModel()) {
    val nav = LocalNavigator.current
    val context = LocalContext.current
    LaunchedEffect(Unit) { viewModel.reload() }

    AppScaffold(title = "Adminbereich", onBack = nav::back) { padding ->
        Column(Modifier.fillMaxSize().padding(top = padding.calculateTopPadding()).verticalScroll(rememberScrollState())) {
            val pending = viewModel.users.value?.withoutAccessCount ?: 0
            AdminLink("Nutzerverwaltung", Icons.Outlined.ManageAccounts, badge = pending) { nav.open(AdminUsersRoute) }
            AdminLink("Feedback", Icons.Outlined.Feedback) { nav.open(AdminFeedbackRoute) }
            AdminLink("Ablageorte", Icons.Outlined.Place) { nav.open(AblageorteRoute) }
            AdminLink("Test-Benachrichtigung", Icons.Outlined.NotificationsActive) { nav.open(PushTestRoute) }
            AdminLink("Wartung", Icons.Outlined.Build) { nav.open(WartungRoute) }

            SectionHeader("In der Web-App")
            AdminLink("Kalender-Einstellungen", Icons.AutoMirrored.Outlined.OpenInNew) { context.openLink(viewModel.webUrl("admin/kalender")) }
            AdminLink("Parkkonten & Portal-Zugang", Icons.AutoMirrored.Outlined.OpenInNew) { context.openLink(viewModel.webUrl("admin/parkplatz")) }
            Column(Modifier.padding(horizontal = 16.dp, vertical = 4.dp)) {
                FormHint("Diese Einstellungen werden selten geändert und sind am Computer übersichtlicher.")
            }
        }
    }
}

@Composable
private fun AdminLink(title: String, icon: androidx.compose.ui.graphics.vector.ImageVector, badge: Int = 0, onClick: () -> Unit) {
    ListItem(
        modifier = Modifier.clickable(onClick = onClick),
        leadingContent = { Icon(icon, contentDescription = null) },
        headlineContent = { Text(title) },
        trailingContent = { if (badge > 0) Badge { Text("$badge") } },
    )
}

// endregion

// region Nutzer

@HiltViewModel
class AdminUsersViewModel @Inject constructor(session: AppSession) : FeatureViewModel(session) {
    val users = remote<List<User>>("users")
    var search by mutableStateOf("")
    var onlyPending by mutableStateOf(false)

    override suspend fun reloadAll() = users.load(session)

    /** Nutzer ohne Zugriff zuerst, damit offene Freischaltungen sofort auffallen. */
    fun visible(items: List<User>): List<User> {
        val query = search.trim().lowercase()
        return items
            .filter { !onlyPending || !Permissions(it).isMember }
            .filter { query.isEmpty() || it.shownName.lowercase().contains(query) || it.email.lowercase().contains(query) }
            .sortedWith(compareBy<User> { Permissions(it).isMember }.thenBy(collator) { it.shownName })
    }
}

@Composable
private fun AdminUsersScreen(viewModel: AdminUsersViewModel = hiltViewModel()) {
    val nav = LocalNavigator.current
    LaunchedEffect(Unit) { viewModel.reload() }

    AppScaffold(title = "Nutzer", onBack = nav::back) { padding ->
        Column(Modifier.fillMaxSize().padding(top = padding.calculateTopPadding())) {
            SearchField(viewModel.search, { viewModel.search = it }, "Name oder E-Mail")
            Row(Modifier.padding(horizontal = 16.dp)) {
                FilterChip(selected = viewModel.onlyPending, onClick = { viewModel.onlyPending = !viewModel.onlyPending }, label = { Text("Nur ohne Freischaltung") })
            }
            RemoteList(viewModel.users, PaddingValues(0.dp), viewModel.isRefreshing, viewModel::refresh, bottomSpacing = 24.dp) { users ->
                items(viewModel.visible(users), key = { it.id }) { user ->
                    val member = Permissions(user).isMember
                    ListItem(
                        modifier = Modifier.clickable { nav.open(AdminUserDetailRoute(user.toArg())) },
                        leadingContent = { Avatar(user.shownName, user.pictureUrl) },
                        headlineContent = { Text(user.shownName) },
                        supportingContent = {
                            Text(
                                user.roles.filter { it != Role.None }.joinToString(", ") { it.label }.nonEmpty ?: "Kein Zugriff",
                                color = if (member) MaterialTheme.colorScheme.onSurfaceVariant else toneColor(Tone.Orange),
                            )
                        },
                    )
                }
            }
        }
    }
}

@HiltViewModel
class AdminUserDetailViewModel @Inject constructor(session: AppSession, saved: SavedStateHandle) : FeatureViewModel(session) {
    val user: User = saved.toRoute<AdminUserDetailRoute>().user.fromArg()
    val roles = mutableStateListOf<Role>().apply { addAll(user.roles) }

    val isSelf: Boolean get() = permissions.isOwner(user.id)

    fun setRole(role: Role, enabled: Boolean) = act(onSuccess = { if (enabled) roles.add(role) else roles.remove(role) }) {
        send(if (enabled) HttpMethod.POST else HttpMethod.DELETE, "users/${user.id}/roles/${role.raw}")
    }

    fun delete(onDone: () -> Unit) = act(onSuccess = { onDone() }) { send(HttpMethod.DELETE, "users/${user.id}") }
}

@Composable
private fun AdminUserDetailScreen(viewModel: AdminUserDetailViewModel = hiltViewModel()) {
    val nav = LocalNavigator.current
    val isOnline = LocalIsOnline.current
    val user = viewModel.user
    var confirmDelete by rememberSaveable { mutableStateOf(false) }

    AppScaffold(title = user.shownName, onBack = nav::back) { padding ->
        Column(Modifier.fillMaxSize().padding(top = padding.calculateTopPadding()).verticalScroll(rememberScrollState()).padding(bottom = 24.dp)) {
            ListItem(
                leadingContent = { Avatar(user.shownName, user.pictureUrl, size = 52.dp) },
                headlineContent = { Text(user.shownName, style = MaterialTheme.typography.titleLarge) },
                supportingContent = {
                    Column {
                        Text(user.email)
                        Text("Registriert am ${user.createdAt.dayString()}", style = MaterialTheme.typography.bodySmall)
                    }
                },
            )
            SectionHeader("Rollen")
            Role.assignable.forEach { role ->
                ListItem(
                    headlineContent = { Text(role.label) },
                    trailingContent = {
                        Switch(
                            checked = role in viewModel.roles,
                            onCheckedChange = { viewModel.setRole(role, it) },
                            enabled = isOnline && !viewModel.action.isRunning,
                        )
                    },
                )
            }
            if (!viewModel.isSelf) {
                OutlinedButton(onClick = { confirmDelete = true }, enabled = isOnline, modifier = Modifier.padding(16.dp)) {
                    Icon(Icons.Outlined.DeleteOutline, contentDescription = null, tint = MaterialTheme.colorScheme.error)
                    Text("  Nutzer löschen", color = MaterialTheme.colorScheme.error)
                }
            }
        }
    }

    if (confirmDelete) {
        ConfirmDialog(
            "${user.shownName} löschen?",
            "Das Konto und alle persönlichen Daten werden entfernt.",
            confirmLabel = "Endgültig löschen",
            onConfirm = { viewModel.delete(nav::back) },
            onDismiss = { confirmDelete = false },
        )
    }
    ActionErrorDialog(viewModel.action)
}

// endregion

// region Feedback

@HiltViewModel
class AdminFeedbackViewModel @Inject constructor(session: AppSession) : FeatureViewModel(session) {
    val feedback = remote<List<Feedback>>("admin/feedback")
    override suspend fun reloadAll() = feedback.load(session)

    @Serializable private data class StatusBody(val status: FeedbackStatus)

    fun setStatus(item: Feedback, status: FeedbackStatus) = act(onSuccess = { reloadAll() }) {
        send(HttpMethod.PATCH, "admin/feedback/${item.id}/status", StatusBody(status))
    }
}

@Composable
private fun AdminFeedbackScreen(viewModel: AdminFeedbackViewModel = hiltViewModel()) {
    val nav = LocalNavigator.current
    val isOnline = LocalIsOnline.current
    LaunchedEffect(Unit) { viewModel.reload() }

    AppScaffold(title = "Feedback", onBack = nav::back) { padding ->
        RemoteList(viewModel.feedback, padding, viewModel.isRefreshing, viewModel::refresh, bottomSpacing = 24.dp) { items ->
            if (items.isEmpty()) emptyItem(Icons.Outlined.Inbox, "Kein Feedback", "")
            items(items.sortedByDescending { it.createdAt }, key = { it.id }) { feedback ->
                var menu by remember { mutableStateOf(false) }
                ListItem(
                    headlineContent = { Text(feedback.userName, fontWeight = FontWeight.SemiBold) },
                    supportingContent = {
                        Column(verticalArrangement = Arrangement.spacedBy(4.dp)) {
                            Text(feedback.text, color = MaterialTheme.colorScheme.onSurface)
                            Text(listOfNotNull(
                                feedback.createdAt.dayTimeString(),
                                feedback.pageUrl.nonEmpty,
                                feedback.appVersion.nonEmpty?.let { "App $it" },
                                feedback.serverVersion.nonEmpty?.let { "Server $it" },
                            ).joinToString(" · "))
                        }
                    },
                    trailingContent = {
                        Box {
                            StatusPill(feedback.status.label, feedback.status.tone, modifier = Modifier.clickable(enabled = isOnline) { menu = true })
                            DropdownMenu(expanded = menu, onDismissRequest = { menu = false }) {
                                FeedbackStatus.selectable.forEach { status ->
                                    DropdownMenuItem(text = { Text(status.label) }, onClick = { menu = false; viewModel.setStatus(feedback, status) })
                                }
                            }
                        }
                    },
                )
            }
        }
    }
    ActionErrorDialog(viewModel.action)
}

// endregion

// region Ablageorte

@HiltViewModel
class AblageorteViewModel @Inject constructor(session: AppSession) : FeatureViewModel(session) {
    val orte = remote<List<AblageortOverview>>("admin/ablageorte")
    override suspend fun reloadAll() = orte.load(session)

    fun delete(id: String) = act(onSuccess = { reloadAll() }) { send(HttpMethod.DELETE, "admin/ablageorte/$id") }
}

@Composable
private fun AblageorteScreen(viewModel: AblageorteViewModel = hiltViewModel()) {
    val nav = LocalNavigator.current
    val isOnline = LocalIsOnline.current
    LaunchedEffect(Unit) { viewModel.reload() }

    AppScaffold(title = "Ablageorte", onBack = nav::back) { padding ->
        RemoteList(viewModel.orte, padding, viewModel.isRefreshing, viewModel::refresh, bottomSpacing = 24.dp) { items ->
            items(items.sortedWith(compareBy(collator) { it.name }), key = { it.stableId }) { ort ->
                ListItem(
                    modifier = Modifier.clickable(enabled = isOnline) { nav.open(AblageortEditRoute(ort.toArg())) },
                    headlineContent = { StorageLocationLabel(ort.name, hexColor(ort.color)) },
                    trailingContent = {
                        Row(verticalAlignment = Alignment.CenterVertically) {
                            Text("${ort.itemCount}", color = MaterialTheme.colorScheme.onSurfaceVariant)
                            val id = ort.id
                            if (id != null && isOnline) {
                                OverflowMenu(listOf(MenuAction("Löschen", Icons.Outlined.DeleteOutline, destructive = true) { viewModel.delete(id) }))
                            }
                        }
                    },
                )
            }
        }
    }
    ActionErrorDialog(viewModel.action)
}

/** Farbauswahl für Ablageorte (Android hat keinen System-Farbwähler). */
private val palette = listOf(
    "#E53935", "#D81B60", "#8E24AA", "#5E35B1", "#3949AB", "#1E88E5", "#039BE5", "#00ACC1",
    "#00897B", "#43A047", "#7CB342", "#C0CA33", "#FDD835", "#FFB300", "#FB8C00", "#F4511E",
    "#6D4C41", "#757575", "#546E7A", "#293748",
)

@HiltViewModel
class AblageortEditViewModel @Inject constructor(session: AppSession, saved: SavedStateHandle) : FeatureViewModel(session) {
    val ort: AblageortOverview = saved.toRoute<AblageortEditRoute>().ort.fromArg()
    var name by mutableStateOf(ort.name)
    var hasColor by mutableStateOf(ort.color != null)
    var color by mutableStateOf(ort.color?.uppercase() ?: palette[5])

    @Serializable private data class UpdateBody(val name: String, val color: String?)
    @Serializable private data class RenameBody(val currentName: String, val newName: String, val color: String?)

    fun save(onDone: () -> Unit) {
        val hex = if (hasColor) color else null
        act(onSuccess = { onDone() }) {
            val id = ort.id
            if (id != null) send(HttpMethod.PUT, "admin/ablageorte/$id", UpdateBody(name.trim(), hex))
            else send(HttpMethod.POST, "admin/ablageorte/rename-from-name", RenameBody(ort.name, name.trim(), hex))
        }
    }
}

@Composable
private fun AblageortEditScreen(viewModel: AblageortEditViewModel = hiltViewModel()) {
    val nav = LocalNavigator.current
    FormScaffold(
        title = "Ablageort",
        onClose = nav::back,
        canSave = viewModel.name.isNotBlank(),
        onSave = { viewModel.save(nav::back) },
        action = viewModel.action,
    ) {
        FormTextField(viewModel.name, { viewModel.name = it }, "Name")
        SwitchRow("Farbe", viewModel.hasColor, { viewModel.hasColor = it })
        if (viewModel.hasColor) {
            FlowRow(horizontalArrangement = Arrangement.spacedBy(10.dp), verticalArrangement = Arrangement.spacedBy(10.dp)) {
                palette.forEach { hex ->
                    val selected = viewModel.color.equals(hex, ignoreCase = true)
                    Box(
                        Modifier
                            .size(40.dp)
                            .clip(CircleShape)
                            .background(hexColor(hex) ?: Color.Gray)
                            .border(BorderStroke(if (selected) 3.dp else 0.dp, MaterialTheme.colorScheme.onSurface), CircleShape)
                            .clickable { viewModel.color = hex },
                        contentAlignment = Alignment.Center,
                    ) {
                        if (selected) Icon(Icons.Outlined.Check, contentDescription = "Ausgewählt", tint = Color.White)
                    }
                }
            }
        }
        FormHint("Wird von ${viewModel.ort.itemCount} Einträgen verwendet. Umbenennen ändert alle Einträge.")
    }
}

// endregion

// region Push & Wartung

@HiltViewModel
class PushTestViewModel @Inject constructor(session: AppSession) : FeatureViewModel(session) {
    val users = remote<List<User>>("users")
    var userId by mutableStateOf<String?>(null)
    var title by mutableStateOf("Test")
    var message by mutableStateOf("Hallo von AkaKraft Online!")
    var sent by mutableStateOf(false)
        private set

    init {
        viewModelScope.launch { users.load(session) }
    }

    @Serializable private data class PushBody(val userId: String?, val title: String, val body: String)

    fun send() = act(onSuccess = { sent = true }) {
        send(HttpMethod.POST, "admin/push/test", PushBody(userId, title.trim(), message.trim()))
    }
}

@Composable
private fun PushTestScreen(viewModel: PushTestViewModel = hiltViewModel()) {
    val nav = LocalNavigator.current
    val isOnline = LocalIsOnline.current
    AppScaffold(title = "Test-Benachrichtigung", onBack = nav::back) { padding ->
        Column(
            Modifier.fillMaxSize().padding(top = padding.calculateTopPadding()).verticalScroll(rememberScrollState()).padding(16.dp),
            verticalArrangement = Arrangement.spacedBy(12.dp),
        ) {
            DropdownField(
                label = "Empfänger",
                options = viewModel.users.value.orEmpty(),
                selected = viewModel.users.value?.firstOrNull { it.id == viewModel.userId },
                optionLabel = { it.shownName },
                onSelect = { viewModel.userId = it?.id },
                noneLabel = "Alle Nutzer",
            )
            FormTextField(viewModel.title, { viewModel.title = it }, "Titel")
            FormTextField(viewModel.message, { viewModel.message = it }, "Text", minLines = 2)
            Button(
                onClick = viewModel::send,
                enabled = isOnline && viewModel.title.isNotBlank() && viewModel.message.isNotBlank() && !viewModel.action.isRunning,
            ) { Text(if (viewModel.sent) "Gesendet ✓" else "Senden") }
        }
    }
    ActionErrorDialog(viewModel.action)
}

@HiltViewModel
class WartungViewModel @Inject constructor(session: AppSession) : FeatureViewModel(session) {
    var result by mutableStateOf<String?>(null)
        private set

    fun run(path: String) = act { result = send(HttpMethod.POST, path).takeIf { it != "null" } ?: "Erledigt." }
}

@Composable
private fun WartungScreen(viewModel: WartungViewModel = hiltViewModel()) {
    val nav = LocalNavigator.current
    val isOnline = LocalIsOnline.current
    val enabled = isOnline && !viewModel.action.isRunning
    AppScaffold(title = "Wartung", onBack = nav::back) { padding ->
        Column(Modifier.fillMaxSize().padding(top = padding.calculateTopPadding()).verticalScroll(rememberScrollState())) {
            ListItem(
                modifier = Modifier.clickable(enabled = enabled) { viewModel.run("admin/trim-text-fields") },
                leadingContent = { Icon(Icons.Outlined.TextFields, contentDescription = null) },
                headlineContent = { Text("Leerzeichen bereinigen") },
            )
            ListItem(
                modifier = Modifier.clickable(enabled = enabled) { viewModel.run("admin/backfill-thumbnails") },
                leadingContent = { Icon(Icons.Outlined.Photo, contentDescription = null) },
                headlineContent = { Text("Thumbnails generieren") },
            )
            Column(Modifier.padding(horizontal = 16.dp)) { FormHint("Betrifft Werkzeug und Verbrauchsmaterial.") }
            if (viewModel.action.isRunning) CircularProgressIndicator(Modifier.padding(16.dp))
            viewModel.result?.let {
                SectionHeader("Ergebnis")
                SelectionContainer {
                    Text(it, fontFamily = FontFamily.Monospace, style = MaterialTheme.typography.bodySmall, modifier = Modifier.fillMaxWidth().padding(16.dp))
                }
            }
        }
    }
    ActionErrorDialog(viewModel.action)
}

// endregion
