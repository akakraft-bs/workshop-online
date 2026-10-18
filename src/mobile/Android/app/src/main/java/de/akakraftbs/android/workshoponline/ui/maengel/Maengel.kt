package de.akakraftbs.android.workshoponline.ui.maengel

import androidx.compose.foundation.background
import androidx.compose.foundation.clickable
import androidx.compose.foundation.horizontalScroll
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.PaddingValues
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.heightIn
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.lazy.items
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.selection.selectable
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.foundation.verticalScroll
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.automirrored.filled.Send
import androidx.compose.material.icons.automirrored.outlined.Chat
import androidx.compose.material.icons.automirrored.outlined.Undo
import androidx.compose.material.icons.outlined.Add
import androidx.compose.material.icons.outlined.Build
import androidx.compose.material.icons.outlined.CalendarMonth
import androidx.compose.material.icons.outlined.DeleteOutline
import androidx.compose.material.icons.outlined.Edit
import androidx.compose.material.icons.automirrored.outlined.HelpOutline
import androidx.compose.material.icons.outlined.Image
import androidx.compose.material.icons.automirrored.outlined.Rule
import androidx.compose.material.icons.outlined.VerifiedUser
import androidx.compose.material.icons.outlined.Verified
import androidx.compose.material.icons.outlined.Warehouse
import androidx.compose.material3.AlertDialog
import androidx.compose.material3.DropdownMenu
import androidx.compose.material3.DropdownMenuItem
import androidx.compose.material3.ExtendedFloatingActionButton
import androidx.compose.material3.FilledIconButton
import androidx.compose.material3.FilterChip
import androidx.compose.material3.Icon
import androidx.compose.material3.ListItem
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.OutlinedTextField
import androidx.compose.material3.RadioButton
import androidx.compose.material3.SegmentedButton
import androidx.compose.material3.SegmentedButtonDefaults
import androidx.compose.material3.SingleChoiceSegmentedButtonRow
import androidx.compose.material3.Text
import androidx.compose.material3.TextButton
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.saveable.rememberSaveable
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.graphics.vector.ImageVector
import androidx.compose.ui.layout.ContentScale
import androidx.compose.ui.semantics.Role
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextOverflow
import androidx.compose.ui.unit.dp
import androidx.hilt.lifecycle.viewmodel.compose.hiltViewModel
import androidx.lifecycle.SavedStateHandle
import androidx.navigation.NavGraphBuilder
import androidx.navigation.compose.composable
import androidx.navigation.toRoute
import dagger.hilt.android.lifecycle.HiltViewModel
import de.akakraftbs.android.workshoponline.core.auth.AppSession
import de.akakraftbs.android.workshoponline.core.auth.fetch
import de.akakraftbs.android.workshoponline.core.cache.remote
import de.akakraftbs.android.workshoponline.core.network.HttpMethod
import de.akakraftbs.android.workshoponline.model.Mangel
import de.akakraftbs.android.workshoponline.model.MangelAnmerkung
import de.akakraftbs.android.workshoponline.model.MangelInput
import de.akakraftbs.android.workshoponline.model.MangelKategorie
import de.akakraftbs.android.workshoponline.model.MangelStatus
import de.akakraftbs.android.workshoponline.model.Tone
import de.akakraftbs.android.workshoponline.model.nonEmpty
import de.akakraftbs.android.workshoponline.ui.components.ActionErrorDialog
import de.akakraftbs.android.workshoponline.ui.components.AppScaffold
import de.akakraftbs.android.workshoponline.ui.components.ConfirmDialog
import de.akakraftbs.android.workshoponline.ui.components.DetailRow
import de.akakraftbs.android.workshoponline.ui.components.DropdownField
import de.akakraftbs.android.workshoponline.ui.components.FeatureViewModel
import de.akakraftbs.android.workshoponline.ui.components.FormScaffold
import de.akakraftbs.android.workshoponline.ui.components.FormTextField
import de.akakraftbs.android.workshoponline.ui.components.LocalIsOnline
import de.akakraftbs.android.workshoponline.ui.components.LocalNavigator
import de.akakraftbs.android.workshoponline.ui.components.MenuAction
import de.akakraftbs.android.workshoponline.ui.components.OverflowMenu
import de.akakraftbs.android.workshoponline.ui.components.PhotoUpload
import de.akakraftbs.android.workshoponline.ui.components.PhotoUploadField
import de.akakraftbs.android.workshoponline.ui.components.RemoteImage
import de.akakraftbs.android.workshoponline.ui.components.RemoteList
import de.akakraftbs.android.workshoponline.ui.components.SectionHeader
import de.akakraftbs.android.workshoponline.ui.components.StatusPill
import de.akakraftbs.android.workshoponline.ui.components.dayTimeString
import de.akakraftbs.android.workshoponline.ui.components.emptyItem
import de.akakraftbs.android.workshoponline.ui.components.fromArg
import de.akakraftbs.android.workshoponline.ui.components.relativeString
import de.akakraftbs.android.workshoponline.ui.components.toArg
import de.akakraftbs.android.workshoponline.ui.components.toneColor
import de.akakraftbs.android.workshoponline.ui.inventar.SearchField
import kotlinx.serialization.Serializable
import javax.inject.Inject

/** Mängel als Unterseite (Smartphone: aus „Mehr“); auf Tablets ist es ein eigener Tab. */
@Serializable data object MaengelPageRoute
@Serializable data class MangelDetailRoute(val mangel: String)
@Serializable data class MangelFormRoute(val mangel: String? = null)
@Serializable data class MangelStatusRoute(val mangel: String)

fun NavGraphBuilder.maengelDestinations() {
    composable<MaengelPageRoute> {
        val nav = LocalNavigator.current
        MaengelScreen(onBack = nav::back)
    }
    composable<MangelDetailRoute> { MangelDetailScreen() }
    composable<MangelFormRoute> { MangelFormScreen() }
    composable<MangelStatusRoute> { MangelStatusScreen() }
}

val MangelKategorie.icon: ImageVector
    get() = when (this) {
        MangelKategorie.Halle -> Icons.Outlined.Warehouse
        MangelKategorie.Werkzeug -> Icons.Outlined.Build
        else -> Icons.AutoMirrored.Outlined.HelpOutline
    }

// region Liste

enum class MangelFilter(val label: String) { Aktiv("Offen"), Erledigt("Erledigt"), Alle("Alle") }

@HiltViewModel
class MaengelViewModel @Inject constructor(session: AppSession) : FeatureViewModel(session) {
    val maengel = remote<List<Mangel>>("mangel")
    var filter by mutableStateOf(MangelFilter.Aktiv)
    var kategorie by mutableStateOf<MangelKategorie?>(null)
    var search by mutableStateOf("")

    override suspend fun reloadAll() = maengel.load(session)

    fun filtered(items: List<Mangel>): List<Mangel> {
        val query = search.trim().lowercase()
        return items
            .filter {
                when (filter) {
                    MangelFilter.Aktiv -> it.status.isActive
                    MangelFilter.Erledigt -> !it.status.isActive
                    MangelFilter.Alle -> true
                }
            }
            .filter { kategorie == null || it.kategorie == kategorie }
            .filter { query.isEmpty() || it.title.lowercase().contains(query) || it.description.lowercase().contains(query) }
            .sortedByDescending { it.createdAt }
    }
}

@Composable
fun MaengelScreen(onBack: (() -> Unit)? = null, viewModel: MaengelViewModel = hiltViewModel()) {
    val nav = LocalNavigator.current
    val isOnline = LocalIsOnline.current
    var kategorieMenu by remember { mutableStateOf(false) }
    LaunchedEffect(Unit) { viewModel.reload() }

    AppScaffold(
        title = "Mängel",
        onBack = onBack,
        floatingActionButton = {
            if (isOnline) {
                ExtendedFloatingActionButton(
                    text = { Text("Mangel melden") },
                    icon = { Icon(Icons.Outlined.Add, contentDescription = null) },
                    onClick = { nav.open(MangelFormRoute()) },
                )
            }
        },
    ) { padding ->
        Column(Modifier.fillMaxSize().padding(top = padding.calculateTopPadding())) {
            SingleChoiceSegmentedButtonRow(Modifier.fillMaxWidth().padding(horizontal = 16.dp)) {
                MangelFilter.entries.forEachIndexed { index, value ->
                    SegmentedButton(
                        selected = viewModel.filter == value,
                        onClick = { viewModel.filter = value },
                        shape = SegmentedButtonDefaults.itemShape(index, MangelFilter.entries.size),
                    ) { Text(value.label) }
                }
            }
            SearchField(viewModel.search, { viewModel.search = it }, "Mängel durchsuchen")
            Row(Modifier.horizontalScroll(rememberScrollState()).padding(horizontal = 16.dp)) {
                Box {
                    FilterChip(
                        selected = viewModel.kategorie != null,
                        onClick = { kategorieMenu = true },
                        label = { Text(viewModel.kategorie?.label ?: "Alle Kategorien") },
                        leadingIcon = viewModel.kategorie?.let { { Icon(it.icon, contentDescription = null, modifier = Modifier.size(18.dp)) } },
                    )
                    DropdownMenu(expanded = kategorieMenu, onDismissRequest = { kategorieMenu = false }) {
                        DropdownMenuItem(text = { Text("Alle Kategorien") }, onClick = { viewModel.kategorie = null; kategorieMenu = false })
                        MangelKategorie.selectable.forEach { kategorie ->
                            DropdownMenuItem(
                                text = { Text(kategorie.label) },
                                leadingIcon = { Icon(kategorie.icon, contentDescription = null) },
                                onClick = { viewModel.kategorie = kategorie; kategorieMenu = false },
                            )
                        }
                    }
                }
            }
            RemoteList(viewModel.maengel, PaddingValues(0.dp), viewModel.isRefreshing, viewModel::refresh) { items ->
                val visible = viewModel.filtered(items)
                if (visible.isEmpty()) {
                    emptyItem(
                        Icons.Outlined.Verified,
                        if (viewModel.search.isBlank()) "Keine Mängel" else "Keine Treffer",
                        if (viewModel.filter == MangelFilter.Aktiv && viewModel.search.isBlank()) "Aktuell ist nichts gemeldet." else "",
                    )
                }
                items(visible, key = { it.id }) { mangel ->
                    MangelRow(mangel) { nav.open(MangelDetailRoute(mangel.toArg())) }
                }
            }
        }
    }
}

@Composable
fun MangelRow(mangel: Mangel, onClick: () -> Unit) {
    val color = toneColor(mangel.status.tone)
    ListItem(
        modifier = Modifier.clickable(onClick = onClick),
        leadingContent = {
            Box(
                Modifier.size(40.dp).clip(RoundedCornerShape(10.dp)).background(color.copy(alpha = 0.14f)),
                contentAlignment = Alignment.Center,
            ) { Icon(mangel.kategorie.icon, contentDescription = null, tint = color) }
        },
        headlineContent = { Text(mangel.title, maxLines = 2, overflow = TextOverflow.Ellipsis) },
        supportingContent = {
            Column(verticalArrangement = Arrangement.spacedBy(4.dp)) {
                Row(horizontalArrangement = Arrangement.spacedBy(8.dp), verticalAlignment = Alignment.CenterVertically) {
                    StatusPill(mangel.status.label, mangel.status.tone)
                    if (mangel.anmerkungen.isNotEmpty()) {
                        Icon(Icons.AutoMirrored.Outlined.Chat, contentDescription = null, modifier = Modifier.size(14.dp))
                        Text("${mangel.anmerkungen.size}", style = MaterialTheme.typography.labelSmall)
                    }
                    if (mangel.imageUrl != null) Icon(Icons.Outlined.Image, contentDescription = "Mit Foto", modifier = Modifier.size(14.dp))
                }
                Text("${mangel.createdByName} · ${mangel.createdAt.relativeString()}", style = MaterialTheme.typography.bodySmall)
            }
        },
    )
}

// endregion

// region Detail

@HiltViewModel
class MangelDetailViewModel @Inject constructor(session: AppSession, saved: SavedStateHandle) : FeatureViewModel(session) {
    var mangel by mutableStateOf<Mangel>(saved.toRoute<MangelDetailRoute>().mangel.fromArg())
        private set
    var newComment by mutableStateOf("")

    val isOwner: Boolean get() = permissions.isOwner(mangel.createdByUserId)

    /** Aktuellen Stand aus der (zwischengespeicherten) Liste übernehmen, z. B. nach Bearbeitung. */
    override suspend fun reloadAll() {
        runCatching { session.fetch<List<Mangel>>("mangel").value }
            .getOrNull()
            ?.firstOrNull { it.id == mangel.id }
            ?.let { mangel = it }
    }

    @Serializable private data class TextBody(val text: String)

    fun addComment() = act(onSuccess = { newComment = ""; reloadAll() }) {
        send(HttpMethod.POST, "mangel/${mangel.id}/anmerkungen", TextBody(newComment.trim()))
    }

    fun editComment(anmerkung: MangelAnmerkung, text: String) = act(onSuccess = { reloadAll() }) {
        send(HttpMethod.PUT, "mangel/${mangel.id}/anmerkungen/${anmerkung.id}", TextBody(text.trim()))
    }

    fun deleteComment(anmerkung: MangelAnmerkung) = act(onSuccess = { reloadAll() }) {
        send(HttpMethod.DELETE, "mangel/${mangel.id}/anmerkungen/${anmerkung.id}")
    }

    fun withdraw() = act {
        mangel = decode(Mangel.serializer(), send(HttpMethod.POST, "mangel/${mangel.id}/zurueckziehen"))
    }
}

@Composable
private fun MangelDetailScreen(viewModel: MangelDetailViewModel = hiltViewModel()) {
    val nav = LocalNavigator.current
    val isOnline = LocalIsOnline.current
    val mangel = viewModel.mangel
    val permissions = viewModel.permissions
    var confirmWithdraw by rememberSaveable { mutableStateOf(false) }
    var editing by remember { mutableStateOf<MangelAnmerkung?>(null) }
    var deleting by remember { mutableStateOf<MangelAnmerkung?>(null) }
    LaunchedEffect(Unit) { viewModel.reload() }

    AppScaffold(
        title = "Mangel",
        onBack = nav::back,
        actions = {
            OverflowMenu(
                enabled = isOnline,
                actions = listOfNotNull(
                    if (permissions.isPrivileged) MenuAction("Status ändern", Icons.AutoMirrored.Outlined.Rule) { nav.open(MangelStatusRoute(mangel.toArg())) } else null,
                    if (permissions.isOwnerOrPrivileged(mangel.createdByUserId)) {
                        MenuAction("Bearbeiten", Icons.Outlined.Edit) { nav.open(MangelFormRoute(mangel.toArg())) }
                    } else null,
                    if (viewModel.isOwner && mangel.status.isActive) {
                        MenuAction("Zurückziehen", Icons.AutoMirrored.Outlined.Undo, destructive = true) { confirmWithdraw = true }
                    } else null,
                ),
            )
        },
    ) { padding ->
        Column(Modifier.fillMaxSize().padding(top = padding.calculateTopPadding()).verticalScroll(rememberScrollState()).padding(bottom = 24.dp)) {
            mangel.imageUrl?.let {
                RemoteImage(it, contentScale = ContentScale.Fit, modifier = Modifier.fillMaxWidth().heightIn(min = 200.dp, max = 360.dp))
            }
            Column(Modifier.padding(16.dp), verticalArrangement = Arrangement.spacedBy(8.dp)) {
                Row(horizontalArrangement = Arrangement.spacedBy(8.dp)) {
                    StatusPill(mangel.status.label, mangel.status.tone)
                    StatusPill(mangel.kategorie.label, Tone.Gray, mangel.kategorie.icon)
                }
                Text(mangel.title, style = MaterialTheme.typography.headlineSmall, fontWeight = FontWeight.SemiBold)
                mangel.description.nonEmpty?.let { Text(it) }
                Text(
                    "Gemeldet von ${mangel.createdByName} am ${mangel.createdAt.dayTimeString()}",
                    style = MaterialTheme.typography.bodySmall,
                    color = MaterialTheme.colorScheme.onSurfaceVariant,
                )
            }

            if (mangel.resolvedByName != null || mangel.note.nonEmpty != null) {
                SectionHeader("Bearbeitung")
                mangel.resolvedByName?.let { DetailRow("Bearbeitet von", it, Icons.Outlined.VerifiedUser) }
                mangel.resolvedAt?.let { DetailRow("Am", it.dayTimeString(), Icons.Outlined.CalendarMonth) }
                mangel.note.nonEmpty?.let { Text(it, modifier = Modifier.padding(horizontal = 16.dp, vertical = 8.dp)) }
            }

            SectionHeader("Anmerkungen")
            if (mangel.anmerkungen.isEmpty()) {
                Text("Noch keine Anmerkungen.", modifier = Modifier.padding(horizontal = 16.dp, vertical = 8.dp), color = MaterialTheme.colorScheme.onSurfaceVariant)
            }
            mangel.anmerkungen.sortedBy { it.createdAt }.forEach { anmerkung ->
                ListItem(
                    overlineContent = { Text("${anmerkung.createdByName} · ${anmerkung.createdAt.relativeString()}") },
                    headlineContent = { Text(anmerkung.text) },
                    supportingContent = anmerkung.updatedAt?.let { { Text("bearbeitet", style = MaterialTheme.typography.labelSmall) } },
                    trailingContent = {
                        if (isOnline && permissions.isOwnerOrPrivileged(anmerkung.createdByUserId)) {
                            OverflowMenu(
                                listOf(
                                    MenuAction("Bearbeiten", Icons.Outlined.Edit) { editing = anmerkung },
                                    MenuAction("Löschen", Icons.Outlined.DeleteOutline, destructive = true) { deleting = anmerkung },
                                ),
                            )
                        }
                    },
                )
            }
            Row(
                Modifier.fillMaxWidth().padding(horizontal = 16.dp, vertical = 8.dp),
                verticalAlignment = Alignment.CenterVertically,
                horizontalArrangement = Arrangement.spacedBy(8.dp),
            ) {
                OutlinedTextField(
                    value = viewModel.newComment,
                    onValueChange = { viewModel.newComment = it },
                    placeholder = { Text("Anmerkung schreiben …") },
                    enabled = isOnline,
                    maxLines = 5,
                    modifier = Modifier.weight(1f),
                )
                FilledIconButton(
                    onClick = viewModel::addComment,
                    enabled = isOnline && viewModel.newComment.isNotBlank() && !viewModel.action.isRunning,
                ) { Icon(Icons.AutoMirrored.Filled.Send, contentDescription = "Senden") }
            }
        }
    }

    if (confirmWithdraw) {
        ConfirmDialog(
            "Mangel zurückziehen?",
            "Der Mangel wird als zurückgezogen markiert.",
            confirmLabel = "Zurückziehen",
            onConfirm = viewModel::withdraw,
            onDismiss = { confirmWithdraw = false },
        )
    }
    deleting?.let { anmerkung ->
        ConfirmDialog("Anmerkung löschen?", onConfirm = { viewModel.deleteComment(anmerkung) }, onDismiss = { deleting = null })
    }
    editing?.let { anmerkung ->
        TextEditDialog(
            title = "Anmerkung bearbeiten",
            initial = anmerkung.text,
            onSave = { viewModel.editComment(anmerkung, it) },
            onDismiss = { editing = null },
        )
    }
    ActionErrorDialog(viewModel.action)
}

/** Kleiner Dialog zum Bearbeiten eines Textes. */
@Composable
fun TextEditDialog(title: String, initial: String, onSave: (String) -> Unit, onDismiss: () -> Unit, label: String? = null) {
    var text by rememberSaveable { mutableStateOf(initial) }
    AlertDialog(
        onDismissRequest = onDismiss,
        title = { Text(title) },
        text = {
            OutlinedTextField(
                value = text,
                onValueChange = { text = it },
                label = label?.let { { Text(it) } },
                minLines = 3,
                modifier = Modifier.fillMaxWidth(),
            )
        },
        confirmButton = {
            TextButton(onClick = { onDismiss(); onSave(text) }, enabled = text.isNotBlank() && LocalIsOnline.current) { Text("Sichern") }
        },
        dismissButton = { TextButton(onClick = onDismiss) { Text("Abbrechen") } },
    )
}

// endregion

// region Status

@HiltViewModel
class MangelStatusViewModel @Inject constructor(session: AppSession, saved: SavedStateHandle) : FeatureViewModel(session) {
    val mangel: Mangel = saved.toRoute<MangelStatusRoute>().mangel.fromArg()
    var status by mutableStateOf(if (mangel.status == MangelStatus.Zurueckgezogen) MangelStatus.Offen else mangel.status)
    var note by mutableStateOf(mangel.note.orEmpty())

    @Serializable private data class StatusBody(val status: MangelStatus, val note: String?)

    fun save(onDone: () -> Unit) = act(onSuccess = { onDone() }) {
        send(HttpMethod.PATCH, "mangel/${mangel.id}/status", StatusBody(status, note.nonEmpty))
    }
}

@Composable
private fun MangelStatusScreen(viewModel: MangelStatusViewModel = hiltViewModel()) {
    val nav = LocalNavigator.current
    FormScaffold(
        title = "Status ändern",
        onClose = nav::back,
        canSave = true,
        onSave = { viewModel.save(nav::back) },
        action = viewModel.action,
    ) {
        MangelStatus.settableByManagers.forEach { status ->
            Row(
                Modifier
                    .fillMaxWidth()
                    .selectable(selected = viewModel.status == status, role = Role.RadioButton) { viewModel.status = status }
                    .padding(vertical = 4.dp),
                verticalAlignment = Alignment.CenterVertically,
            ) {
                RadioButton(selected = viewModel.status == status, onClick = null)
                Text(status.label, modifier = Modifier.padding(start = 12.dp))
            }
        }
        FormTextField(viewModel.note, { viewModel.note = it }, "Notiz (optional)", minLines = 2, placeholder = "z. B. was erledigt wurde")
    }
}

// endregion

// region Formular

@HiltViewModel
class MangelFormViewModel @Inject constructor(session: AppSession, saved: SavedStateHandle) : FeatureViewModel(session) {
    val mangel: Mangel? = saved.toRoute<MangelFormRoute>().mangel?.fromArg()
    var title by mutableStateOf(mangel?.title.orEmpty())
    var description by mutableStateOf(mangel?.description.orEmpty())
    var kategorie by mutableStateOf(mangel?.kategorie?.takeIf { it != MangelKategorie.Unknown } ?: MangelKategorie.Halle)
    val photo = PhotoUpload(session, "uploads/mangel", mangel?.imageUrl)

    val canSave: Boolean get() = title.isNotBlank() && !photo.isUploading

    fun save(onDone: () -> Unit) {
        val input = MangelInput(title.trim(), description.trim(), kategorie, photo.imageUrl)
        act(onSuccess = { onDone() }) {
            if (mangel != null) send(HttpMethod.PATCH, "mangel/${mangel.id}", input)
            else send(HttpMethod.POST, "mangel", input)
        }
    }
}

@Composable
private fun MangelFormScreen(viewModel: MangelFormViewModel = hiltViewModel()) {
    val nav = LocalNavigator.current
    FormScaffold(
        title = if (viewModel.mangel == null) "Mangel melden" else "Mangel bearbeiten",
        onClose = nav::back,
        canSave = viewModel.canSave,
        onSave = { viewModel.save(nav::back) },
        action = viewModel.action,
        saveLabel = if (viewModel.mangel == null) "Melden" else "Sichern",
    ) {
        FormTextField(viewModel.title, { viewModel.title = it }, "Was ist kaputt?")
        FormTextField(viewModel.description, { viewModel.description = it }, "Beschreibung", minLines = 3)
        DropdownField(
            label = "Kategorie",
            options = MangelKategorie.selectable,
            selected = viewModel.kategorie,
            optionLabel = { it.label },
            onSelect = { it?.let { kategorie -> viewModel.kategorie = kategorie } },
        )
        PhotoUploadField(viewModel.photo)
    }
}

// endregion
