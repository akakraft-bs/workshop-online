package de.akakraftbs.android.workshoponline.ui.gemeinschaft

import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.lazy.items
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.selection.toggleable
import androidx.compose.foundation.verticalScroll
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.outlined.Add
import androidx.compose.material.icons.outlined.Celebration
import androidx.compose.material.icons.outlined.Check
import androidx.compose.material.icons.outlined.Close
import androidx.compose.material.icons.outlined.DeleteOutline
import androidx.compose.material.icons.outlined.Edit
import androidx.compose.material.icons.outlined.Group
import androidx.compose.material.icons.outlined.Lock
import androidx.compose.material.icons.outlined.NotificationsActive
import androidx.compose.material.icons.outlined.PanTool
import androidx.compose.material.icons.outlined.Poll
import androidx.compose.material.icons.outlined.Schedule
import androidx.compose.material3.Button
import androidx.compose.material3.Checkbox
import androidx.compose.material3.ExtendedFloatingActionButton
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButton
import androidx.compose.material3.LinearProgressIndicator
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
import androidx.compose.runtime.mutableStateListOf
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.saveable.rememberSaveable
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.semantics.Role
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextOverflow
import androidx.compose.ui.unit.dp
import androidx.hilt.lifecycle.viewmodel.compose.hiltViewModel
import androidx.lifecycle.SavedStateHandle
import androidx.lifecycle.viewModelScope
import androidx.navigation.NavGraphBuilder
import androidx.navigation.compose.composable
import androidx.navigation.toRoute
import dagger.hilt.android.lifecycle.HiltViewModel
import de.akakraftbs.android.workshoponline.core.auth.AppSession
import de.akakraftbs.android.workshoponline.core.auth.fetch
import de.akakraftbs.android.workshoponline.core.cache.remote
import de.akakraftbs.android.workshoponline.core.network.HttpMethod
import de.akakraftbs.android.workshoponline.model.CalendarEvent
import de.akakraftbs.android.workshoponline.model.Tone
import de.akakraftbs.android.workshoponline.model.Umfrage
import de.akakraftbs.android.workshoponline.model.UmfrageCreateInput
import de.akakraftbs.android.workshoponline.model.UmfrageInput
import de.akakraftbs.android.workshoponline.model.UmfrageOption
import de.akakraftbs.android.workshoponline.model.UmfrageOptionInput
import de.akakraftbs.android.workshoponline.model.nonEmpty
import de.akakraftbs.android.workshoponline.ui.components.ActionErrorDialog
import de.akakraftbs.android.workshoponline.ui.components.AppScaffold
import de.akakraftbs.android.workshoponline.ui.components.ConfirmDialog
import de.akakraftbs.android.workshoponline.ui.components.DateTimeField
import de.akakraftbs.android.workshoponline.ui.components.DetailRow
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
import de.akakraftbs.android.workshoponline.ui.components.fromArg
import de.akakraftbs.android.workshoponline.ui.components.relativeString
import de.akakraftbs.android.workshoponline.ui.components.sectionHeader
import de.akakraftbs.android.workshoponline.ui.components.toArg
import de.akakraftbs.android.workshoponline.ui.components.toneColor
import kotlinx.coroutines.launch
import kotlinx.serialization.Serializable
import java.time.Duration
import java.time.Instant
import javax.inject.Inject

@Serializable data object UmfragenRoute
@Serializable data class UmfrageDetailRoute(val umfrage: String)
@Serializable data class UmfrageFormRoute(val umfrage: String? = null)

fun NavGraphBuilder.umfrageDestinations() {
    composable<UmfragenRoute> { UmfragenScreen() }
    composable<UmfrageDetailRoute> { UmfrageDetailScreen() }
    composable<UmfrageFormRoute> { UmfrageFormScreen() }
}

// region Liste

@HiltViewModel
class UmfragenViewModel @Inject constructor(session: AppSession) : FeatureViewModel(session) {
    val umfragen = remote<List<Umfrage>>("umfrage")
    override suspend fun reloadAll() = umfragen.load(session)
}

@Composable
private fun UmfragenScreen(viewModel: UmfragenViewModel = hiltViewModel()) {
    val nav = LocalNavigator.current
    val isOnline = LocalIsOnline.current
    LaunchedEffect(Unit) { viewModel.reload() }

    AppScaffold(
        title = "Umfragen",
        onBack = nav::back,
        floatingActionButton = {
            if (isOnline) {
                ExtendedFloatingActionButton(
                    text = { Text("Umfrage") },
                    icon = { Icon(Icons.Outlined.Add, contentDescription = null) },
                    onClick = { nav.open(UmfrageFormRoute()) },
                )
            }
        },
    ) { padding ->
        RemoteList(viewModel.umfragen, padding, viewModel.isRefreshing, viewModel::refresh) { items ->
            val open = items.filter { it.isOpen }.sortedBy { it.deadline ?: Instant.MAX }
            val closed = items.filterNot { it.isOpen }.sortedByDescending { it.closedAt ?: it.createdAt }
            sectionHeader("Offen")
            if (open.isEmpty()) {
                item(key = "none-open") {
                    Text("Keine offenen Umfragen.", modifier = Modifier.padding(16.dp), color = MaterialTheme.colorScheme.onSurfaceVariant)
                }
            }
            items(open, key = { it.id }) { UmfrageRow(it) { nav.open(UmfrageDetailRoute(it.toArg())) } }
            if (closed.isNotEmpty()) {
                sectionHeader("Abgeschlossen")
                items(closed, key = { it.id }) { UmfrageRow(it) { nav.open(UmfrageDetailRoute(it.toArg())) } }
            }
        }
    }
}

@Composable
fun UmfrageRow(umfrage: Umfrage, onClick: () -> Unit) {
    ListItem(
        modifier = Modifier.clickable(onClick = onClick),
        leadingContent = { Icon(Icons.Outlined.Poll, contentDescription = null) },
        headlineContent = { Text(umfrage.question, maxLines = 2, overflow = TextOverflow.Ellipsis) },
        supportingContent = {
            Row(horizontalArrangement = Arrangement.spacedBy(8.dp), verticalAlignment = Alignment.CenterVertically) {
                when {
                    !umfrage.isOpen -> StatusPill("Geschlossen", Tone.Gray)
                    umfrage.hasParticipated -> StatusPill(if (umfrage.currentUserAbstained) "Enthalten" else "Abgestimmt", Tone.Green, Icons.Outlined.Check)
                    else -> StatusPill("Noch offen", Tone.Orange, Icons.Outlined.PanTool)
                }
                Icon(Icons.Outlined.Group, contentDescription = null, modifier = Modifier.size(14.dp))
                Text("${umfrage.participantCount}", style = MaterialTheme.typography.labelSmall)
                val deadline = umfrage.deadline
                if (deadline != null && umfrage.isOpen) {
                    Icon(Icons.Outlined.Schedule, contentDescription = null, modifier = Modifier.size(14.dp))
                    Text(
                        deadline.relativeString(),
                        style = MaterialTheme.typography.labelSmall,
                        color = if (deadline.isBefore(Instant.now())) toneColor(Tone.Red) else MaterialTheme.colorScheme.onSurfaceVariant,
                    )
                }
            }
        },
    )
}

// endregion

// region Detail

@HiltViewModel
class UmfrageDetailViewModel @Inject constructor(session: AppSession, saved: SavedStateHandle) : FeatureViewModel(session) {
    var umfrage by mutableStateOf<Umfrage>(saved.toRoute<UmfrageDetailRoute>().umfrage.fromArg())
        private set
    val selection = mutableStateListOf<String>().apply { addAll(umfrage.currentUserOptionIds.map { it.lowercase() }) }
    var reminderSent by mutableStateOf(false)
        private set

    val canManage: Boolean get() = permissions.isOwnerOrPrivileged(umfrage.createdByUserId)
    val totalVotes: Int get() = umfrage.options.sumOf { it.voteCount ?: 0 }
    val selectionChanged: Boolean get() = selection.toSet() != umfrage.currentUserOptionIds.map { it.lowercase() }.toSet()

    override suspend fun reloadAll() {
        runCatching { session.fetch<List<Umfrage>>("umfrage").value }.getOrNull()
            ?.firstOrNull { it.id == umfrage.id }
            ?.let { apply(it) }
    }

    private fun apply(updated: Umfrage) {
        umfrage = updated
        selection.clear()
        selection.addAll(updated.currentUserOptionIds.map { it.lowercase() })
    }

    fun toggle(option: UmfrageOption) {
        val id = option.id.lowercase()
        if (umfrage.isMultipleChoice) {
            if (id in selection) selection.remove(id) else selection.add(id)
        } else {
            selection.clear()
            selection.add(id)
        }
    }

    @Serializable private data class VoteBody(val optionIds: List<String>)

    fun vote() = act {
        val ids = umfrage.options.map { it.id }.filter { it.lowercase() in selection }
        apply(decode(Umfrage.serializer(), send(HttpMethod.POST, "umfrage/${umfrage.id}/vote", VoteBody(ids))))
    }

    fun abstain() = act { apply(decode(Umfrage.serializer(), send(HttpMethod.POST, "umfrage/${umfrage.id}/abstain"))) }

    fun close() = act { apply(decode(Umfrage.serializer(), send(HttpMethod.POST, "umfrage/${umfrage.id}/close"))) }

    fun remind() = act(onSuccess = { reminderSent = true }) { send(HttpMethod.POST, "umfrage/${umfrage.id}/remind") }

    fun delete(onDone: () -> Unit) = act(onSuccess = { onDone() }) { send(HttpMethod.DELETE, "umfrage/${umfrage.id}") }
}

@Composable
private fun UmfrageDetailScreen(viewModel: UmfrageDetailViewModel = hiltViewModel()) {
    val nav = LocalNavigator.current
    val isOnline = LocalIsOnline.current
    val umfrage = viewModel.umfrage
    var confirmClose by rememberSaveable { mutableStateOf(false) }
    var confirmDelete by rememberSaveable { mutableStateOf(false) }
    LaunchedEffect(Unit) { viewModel.reload() }

    AppScaffold(
        title = "Umfrage",
        onBack = nav::back,
        actions = {
            if (viewModel.canManage) {
                OverflowMenu(
                    enabled = isOnline,
                    actions = buildList {
                        if (umfrage.isOpen) {
                            add(MenuAction("Bearbeiten", Icons.Outlined.Edit) { nav.open(UmfrageFormRoute(umfrage.toArg())) })
                            if (!viewModel.reminderSent) add(MenuAction("Erinnerung senden", Icons.Outlined.NotificationsActive, onClick = viewModel::remind))
                            add(MenuAction("Schließen", Icons.Outlined.Lock) { confirmClose = true })
                        }
                        add(MenuAction("Löschen", Icons.Outlined.DeleteOutline, destructive = true) { confirmDelete = true })
                    },
                )
            }
        },
    ) { padding ->
        Column(Modifier.fillMaxSize().padding(top = padding.calculateTopPadding()).verticalScroll(rememberScrollState()).padding(bottom = 24.dp)) {
            Column(Modifier.padding(16.dp), verticalArrangement = Arrangement.spacedBy(8.dp)) {
                Text(umfrage.question, style = MaterialTheme.typography.headlineSmall, fontWeight = FontWeight.SemiBold)
                umfrage.description.nonEmpty?.let { Text(it) }
                Text(
                    "Von ${umfrage.createdByName} · ${umfrage.createdAt.dayString()}",
                    style = MaterialTheme.typography.bodySmall,
                    color = MaterialTheme.colorScheme.onSurfaceVariant,
                )
            }
            umfrage.linkedEventTitle?.let { title ->
                DetailRow("Veranstaltung", listOfNotNull(title, umfrage.linkedEventStart?.dayTimeString()).joinToString(" · "), Icons.Outlined.Celebration)
            }
            umfrage.deadline?.let { DetailRow("Frist", it.dayTimeString(), Icons.Outlined.Schedule) }
            DetailRow("Teilnahmen", "${umfrage.participantCount} (davon ${umfrage.enthaltungCount} Enthaltungen)", Icons.Outlined.Group)

            SectionHeader(if (umfrage.isMultipleChoice) "Mehrfachauswahl möglich" else "Eine Antwort wählen")
            umfrage.sortedOptions.forEach { option -> OptionRow(viewModel, option) }
            if (!umfrage.resultsAvailable) {
                Column(Modifier.padding(horizontal = 16.dp, vertical = 4.dp)) {
                    FormHint(
                        if (umfrage.revealAfterClose) "Die Ergebnisse werden nach dem Schließen sichtbar."
                        else "Die Ergebnisse sind nur für die erstellende Person und den Vorstand sichtbar.",
                    )
                }
            }

            if (umfrage.isOpen) {
                Row(Modifier.fillMaxWidth().padding(16.dp), horizontalArrangement = Arrangement.spacedBy(8.dp)) {
                    Button(
                        onClick = viewModel::vote,
                        enabled = isOnline && viewModel.selection.isNotEmpty() && viewModel.selectionChanged && !viewModel.action.isRunning,
                        modifier = Modifier.weight(1f),
                    ) { Text(if (umfrage.currentUserOptionIds.isEmpty()) "Abstimmen" else "Stimme ändern") }
                    OutlinedButton(
                        onClick = viewModel::abstain,
                        enabled = isOnline && !umfrage.currentUserAbstained && !viewModel.action.isRunning,
                    ) { Text("Enthalten") }
                }
            } else {
                umfrage.closedByName?.let { name ->
                    Text(
                        "Geschlossen von $name" + (umfrage.closedAt?.let { " am ${it.dayString()}" } ?: ""),
                        modifier = Modifier.padding(16.dp),
                        color = MaterialTheme.colorScheme.onSurfaceVariant,
                    )
                }
            }
        }
    }

    if (confirmClose) {
        ConfirmDialog("Umfrage schließen?", "Danach kann niemand mehr abstimmen.", confirmLabel = "Schließen", onConfirm = viewModel::close, onDismiss = { confirmClose = false })
    }
    if (confirmDelete) {
        ConfirmDialog("Umfrage löschen?", onConfirm = { viewModel.delete(nav::back) }, onDismiss = { confirmDelete = false })
    }
    ActionErrorDialog(viewModel.action)
}

@Composable
private fun OptionRow(viewModel: UmfrageDetailViewModel, option: UmfrageOption) {
    val umfrage = viewModel.umfrage
    val selected = option.id.lowercase() in viewModel.selection
    Column(
        Modifier
            .fillMaxWidth()
            .toggleable(
                value = selected,
                enabled = umfrage.isOpen,
                role = if (umfrage.isMultipleChoice) Role.Checkbox else Role.RadioButton,
                onValueChange = { viewModel.toggle(option) },
            )
            .padding(horizontal = 16.dp, vertical = 8.dp),
        verticalArrangement = Arrangement.spacedBy(6.dp),
    ) {
        Row(verticalAlignment = Alignment.CenterVertically) {
            if (umfrage.isMultipleChoice) Checkbox(checked = selected, onCheckedChange = null, enabled = umfrage.isOpen)
            else RadioButton(selected = selected, onClick = null, enabled = umfrage.isOpen)
            Text(option.text, modifier = Modifier.weight(1f).padding(start = 12.dp))
            option.voteCount?.let { Text("$it", style = MaterialTheme.typography.titleMedium) }
        }
        option.voteCount?.let { count ->
            LinearProgressIndicator(
                progress = { count.toFloat() / maxOf(viewModel.totalVotes, 1) },
                modifier = Modifier.fillMaxWidth(),
                color = if (selected) MaterialTheme.colorScheme.primary else MaterialTheme.colorScheme.outline,
            )
            option.voters?.takeIf { it.isNotEmpty() }?.let {
                Text(it.joinToString(", "), style = MaterialTheme.typography.bodySmall, color = MaterialTheme.colorScheme.onSurfaceVariant)
            }
        }
    }
}

// endregion

// region Formular

class EditableOption(val serverId: String?, text: String) {
    var text by mutableStateOf(text)
}

@HiltViewModel
class UmfrageFormViewModel @Inject constructor(session: AppSession, saved: SavedStateHandle) : FeatureViewModel(session) {
    val umfrage: Umfrage? = saved.toRoute<UmfrageFormRoute>().umfrage?.fromArg()
    val events = remote<List<CalendarEvent>>("calendar/upcoming-events")

    var question by mutableStateOf(umfrage?.question.orEmpty())
    var description by mutableStateOf(umfrage?.description.orEmpty())
    val options = mutableStateListOf<EditableOption>().apply {
        addAll(umfrage?.sortedOptions?.map { EditableOption(it.id, it.text) } ?: listOf(EditableOption(null, ""), EditableOption(null, "")))
    }
    var isMultipleChoice by mutableStateOf(umfrage?.isMultipleChoice ?: false)
    var resultsVisible by mutableStateOf(umfrage?.resultsVisible ?: true)
    var revealAfterClose by mutableStateOf(umfrage?.revealAfterClose ?: true)
    var hasDeadline by mutableStateOf(umfrage?.deadline != null)
    var deadline by mutableStateOf(umfrage?.deadline ?: Instant.now().plus(Duration.ofDays(7)))
    var linkedEvent by mutableStateOf<CalendarEvent?>(null)

    private val filledOptions get() = options.filter { it.text.isNotBlank() }
    val canSave: Boolean get() = question.isNotBlank() && filledOptions.size >= 2

    init {
        viewModelScope.launch {
            events.load(session)
            umfrage?.linkedEventId?.let { id -> linkedEvent = events.value?.firstOrNull { it.id == id } }
        }
    }

    fun save(onDone: () -> Unit) {
        val event = linkedEvent
        val keepExistingLink = umfrage?.linkedEventId != null && event == null && events.value == null
        act(onSuccess = { onDone() }) {
            if (umfrage != null) {
                val input = UmfrageInput(
                    question = question.trim(),
                    description = description.nonEmpty,
                    options = filledOptions.map { UmfrageOptionInput(it.serverId, it.text.trim()) },
                    isMultipleChoice = isMultipleChoice,
                    resultsVisible = resultsVisible,
                    revealAfterClose = if (resultsVisible) false else revealAfterClose,
                    deadline = if (hasDeadline) deadline else null,
                    linkedEventId = if (keepExistingLink) umfrage.linkedEventId else event?.id,
                    linkedCalendarId = if (keepExistingLink) umfrage.linkedCalendarId else event?.calendarId,
                    linkedEventTitle = if (keepExistingLink) umfrage.linkedEventTitle else event?.title,
                    linkedEventStart = if (keepExistingLink) umfrage.linkedEventStart else event?.start,
                )
                send(HttpMethod.PUT, "umfrage/${umfrage.id}", input)
            } else {
                val input = UmfrageCreateInput(
                    question = question.trim(),
                    description = description.nonEmpty,
                    options = filledOptions.map { it.text.trim() },
                    isMultipleChoice = isMultipleChoice,
                    resultsVisible = resultsVisible,
                    revealAfterClose = if (resultsVisible) false else revealAfterClose,
                    deadline = if (hasDeadline) deadline else null,
                    linkedEventId = event?.id,
                    linkedCalendarId = event?.calendarId,
                    linkedEventTitle = event?.title,
                    linkedEventStart = event?.start,
                )
                send(HttpMethod.POST, "umfrage", input)
            }
        }
    }
}

@Composable
private fun UmfrageFormScreen(viewModel: UmfrageFormViewModel = hiltViewModel()) {
    val nav = LocalNavigator.current
    FormScaffold(
        title = if (viewModel.umfrage == null) "Neue Umfrage" else "Umfrage bearbeiten",
        onClose = nav::back,
        canSave = viewModel.canSave,
        onSave = { viewModel.save(nav::back) },
        action = viewModel.action,
    ) {
        FormTextField(viewModel.question, { viewModel.question = it }, "Frage", minLines = 2)
        FormTextField(viewModel.description, { viewModel.description = it }, "Beschreibung (optional)", minLines = 2)

        Text("Antworten", style = MaterialTheme.typography.titleSmall)
        viewModel.options.forEachIndexed { index, option ->
            Row(verticalAlignment = Alignment.CenterVertically) {
                OutlinedTextField(
                    value = option.text,
                    onValueChange = { option.text = it },
                    label = { Text("Antwort ${index + 1}") },
                    singleLine = true,
                    modifier = Modifier.weight(1f),
                )
                IconButton(onClick = { viewModel.options.removeAt(index) }, enabled = viewModel.options.size > 2) {
                    Icon(Icons.Outlined.Close, contentDescription = "Antwort entfernen")
                }
            }
        }
        TextButton(onClick = { viewModel.options.add(EditableOption(null, "")) }) {
            Icon(Icons.Outlined.Add, contentDescription = null)
            Text("  Antwort hinzufügen")
        }

        Text("Einstellungen", style = MaterialTheme.typography.titleSmall)
        SwitchRow("Mehrfachauswahl", viewModel.isMultipleChoice, { viewModel.isMultipleChoice = it })
        SwitchRow("Ergebnisse für alle sichtbar", viewModel.resultsVisible, { viewModel.resultsVisible = it })
        if (!viewModel.resultsVisible) {
            SwitchRow("Nach dem Schließen anzeigen", viewModel.revealAfterClose, { viewModel.revealAfterClose = it })
        }
        SwitchRow("Frist", viewModel.hasDeadline, { viewModel.hasDeadline = it })
        if (viewModel.hasDeadline) DateTimeField("Endet am", viewModel.deadline, { viewModel.deadline = it })

        DropdownField(
            label = "Veranstaltung verknüpfen",
            options = viewModel.events.value.orEmpty(),
            selected = viewModel.linkedEvent,
            optionLabel = { event -> event.title + (event.start?.let { " · ${it.dayString()}" } ?: "") },
            onSelect = { viewModel.linkedEvent = it },
            noneLabel = "Keine",
        )
    }
}

// endregion
