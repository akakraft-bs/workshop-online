package de.akakraftbs.android.workshoponline.ui.gemeinschaft

import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.heightIn
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.lazy.items
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.verticalScroll
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.CheckCircle
import androidx.compose.material.icons.outlined.Add
import androidx.compose.material.icons.outlined.CalendarMonth
import androidx.compose.material.icons.outlined.CheckCircle
import androidx.compose.material.icons.outlined.Circle
import androidx.compose.material.icons.outlined.DeleteOutline
import androidx.compose.material.icons.outlined.Edit
import androidx.compose.material.icons.outlined.ExpandMore
import androidx.compose.material.icons.outlined.Flag
import androidx.compose.material.icons.outlined.Person
import androidx.compose.material3.Button
import androidx.compose.material3.ExtendedFloatingActionButton
import androidx.compose.material3.Icon
import androidx.compose.material3.ListItem
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.saveable.rememberSaveable
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.rotate
import androidx.compose.ui.layout.ContentScale
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextDecoration
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
import de.akakraftbs.android.workshoponline.model.Aufgabe
import de.akakraftbs.android.workshoponline.model.AufgabeCreateInput
import de.akakraftbs.android.workshoponline.model.AufgabePriority
import de.akakraftbs.android.workshoponline.model.AufgabeStatus
import de.akakraftbs.android.workshoponline.model.AufgabeUpdateInput
import de.akakraftbs.android.workshoponline.model.AssignableUser
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
import de.akakraftbs.android.workshoponline.ui.components.StatusPill
import de.akakraftbs.android.workshoponline.ui.components.SwitchRow
import de.akakraftbs.android.workshoponline.ui.components.dayString
import de.akakraftbs.android.workshoponline.ui.components.emptyItem
import de.akakraftbs.android.workshoponline.ui.components.fromArg
import de.akakraftbs.android.workshoponline.ui.components.toArg
import de.akakraftbs.android.workshoponline.ui.components.toneColor
import kotlinx.coroutines.launch
import kotlinx.serialization.Serializable
import javax.inject.Inject

@Serializable data object AufgabenRoute
@Serializable data class AufgabeDetailRoute(val aufgabe: String)
@Serializable data class AufgabeFormRoute(val aufgabe: String? = null)

fun NavGraphBuilder.aufgabenDestinations() {
    composable<AufgabenRoute> { AufgabenScreen() }
    composable<AufgabeDetailRoute> { AufgabeDetailScreen() }
    composable<AufgabeFormRoute> { AufgabeFormScreen() }
}

// region Liste

@HiltViewModel
class AufgabenViewModel @Inject constructor(session: AppSession) : FeatureViewModel(session) {
    val aufgaben = remote<List<Aufgabe>>("aufgaben")
    var showDone by mutableStateOf(false)
    override suspend fun reloadAll() = aufgaben.load(session)
}

@Composable
private fun AufgabenScreen(viewModel: AufgabenViewModel = hiltViewModel()) {
    val nav = LocalNavigator.current
    val isOnline = LocalIsOnline.current
    LaunchedEffect(Unit) { viewModel.reload() }

    AppScaffold(
        title = "Aufgaben",
        onBack = nav::back,
        floatingActionButton = {
            if (viewModel.permissions.isPrivileged && isOnline) {
                ExtendedFloatingActionButton(
                    text = { Text("Aufgabe") },
                    icon = { Icon(Icons.Outlined.Add, contentDescription = null) },
                    onClick = { nav.open(AufgabeFormRoute()) },
                )
            }
        },
    ) { padding ->
        RemoteList(viewModel.aufgaben, padding, viewModel.isRefreshing, viewModel::refresh) { items ->
            val open = items.filter { it.status != AufgabeStatus.Erledigt }
                .sortedWith(compareBy<Aufgabe> { it.priority }.thenByDescending { it.createdAt })
            val done = items.filter { it.status == AufgabeStatus.Erledigt }.sortedByDescending { it.createdAt }
            if (open.isEmpty()) emptyItem(Icons.Outlined.CheckCircle, "Alles erledigt", "Es gibt keine offenen Aufgaben.")
            items(open, key = { it.id }) { AufgabeRow(it) { nav.open(AufgabeDetailRoute(it.toArg())) } }
            if (done.isNotEmpty()) {
                item(key = "done-header") {
                    ListItem(
                        modifier = Modifier.clickable { viewModel.showDone = !viewModel.showDone },
                        headlineContent = { Text("Erledigt (${done.size})", color = MaterialTheme.colorScheme.primary) },
                        trailingContent = {
                            Icon(Icons.Outlined.ExpandMore, contentDescription = null, modifier = Modifier.rotate(if (viewModel.showDone) 180f else 0f))
                        },
                    )
                }
                if (viewModel.showDone) items(done, key = { it.id }) { AufgabeRow(it) { nav.open(AufgabeDetailRoute(it.toArg())) } }
            }
        }
    }
}

@Composable
private fun AufgabeRow(aufgabe: Aufgabe, onClick: () -> Unit) {
    val priority = AufgabePriority.of(aufgabe.priority)
    val done = aufgabe.status == AufgabeStatus.Erledigt
    ListItem(
        modifier = Modifier.clickable(onClick = onClick),
        leadingContent = {
            Icon(
                if (done) Icons.Filled.CheckCircle else Icons.Outlined.Circle,
                contentDescription = if (done) "Erledigt" else "Offen",
                tint = toneColor(if (done) Tone.Green else priority.tone),
            )
        },
        headlineContent = { Text(aufgabe.titel, textDecoration = if (done) TextDecoration.LineThrough else null) },
        supportingContent = {
            Row(horizontalArrangement = Arrangement.spacedBy(8.dp), verticalAlignment = Alignment.CenterVertically) {
                if (!done) StatusPill(priority.shortLabel, priority.tone)
                aufgabe.assignee?.let {
                    Icon(Icons.Outlined.Person, contentDescription = null, modifier = Modifier.size(14.dp))
                    Text(it, style = MaterialTheme.typography.bodySmall)
                }
            }
        },
    )
}

// endregion

// region Detail

@HiltViewModel
class AufgabeDetailViewModel @Inject constructor(session: AppSession, saved: SavedStateHandle) : FeatureViewModel(session) {
    var aufgabe by mutableStateOf<Aufgabe>(saved.toRoute<AufgabeDetailRoute>().aufgabe.fromArg())
        private set

    override suspend fun reloadAll() {
        runCatching { session.fetch<List<Aufgabe>>("aufgaben").value }.getOrNull()
            ?.firstOrNull { it.id == aufgabe.id }
            ?.let { aufgabe = it }
    }

    fun markDone() {
        val a = aufgabe
        val input = AufgabeUpdateInput(a.titel, a.beschreibung, a.fotoUrl, a.priority, a.assignedUserId, a.assignedName, erledigt = true)
        act(onSuccess = { reloadAll() }) { send(HttpMethod.PUT, "aufgaben/${a.id}", input) }
    }

    fun delete(onDone: () -> Unit) = act(onSuccess = { onDone() }) { send(HttpMethod.DELETE, "aufgaben/${aufgabe.id}") }
}

@Composable
private fun AufgabeDetailScreen(viewModel: AufgabeDetailViewModel = hiltViewModel()) {
    val nav = LocalNavigator.current
    val isOnline = LocalIsOnline.current
    val aufgabe = viewModel.aufgabe
    val priority = AufgabePriority.of(aufgabe.priority)
    var confirmDelete by rememberSaveable { mutableStateOf(false) }
    LaunchedEffect(Unit) { viewModel.reload() }

    AppScaffold(
        title = "Aufgabe",
        onBack = nav::back,
        actions = {
            if (viewModel.permissions.isPrivileged) {
                OverflowMenu(
                    enabled = isOnline,
                    actions = listOf(
                        MenuAction("Bearbeiten", Icons.Outlined.Edit) { nav.open(AufgabeFormRoute(aufgabe.toArg())) },
                        MenuAction("Löschen", Icons.Outlined.DeleteOutline, destructive = true) { confirmDelete = true },
                    ),
                )
            }
        },
    ) { padding ->
        Column(Modifier.fillMaxSize().padding(top = padding.calculateTopPadding()).verticalScroll(rememberScrollState()).padding(bottom = 24.dp)) {
            aufgabe.fotoUrl?.let {
                RemoteImage(it, contentScale = ContentScale.Fit, modifier = Modifier.fillMaxWidth().heightIn(min = 200.dp, max = 340.dp))
            }
            Column(Modifier.padding(16.dp), verticalArrangement = Arrangement.spacedBy(8.dp)) {
                Row(horizontalArrangement = Arrangement.spacedBy(8.dp)) {
                    StatusPill(aufgabe.status.label, aufgabe.status.tone)
                    StatusPill(priority.shortLabel, priority.tone, Icons.Outlined.Flag)
                }
                Text(aufgabe.titel, style = MaterialTheme.typography.headlineSmall, fontWeight = FontWeight.SemiBold)
                aufgabe.beschreibung.nonEmpty?.let { Text(it) }
            }
            DetailRow("Priorität", priority.label, Icons.Outlined.Flag)
            DetailRow("Zuständig", aufgabe.assignee ?: "Niemand", Icons.Outlined.Person)
            DetailRow("Erstellt", "${aufgabe.createdByName} · ${aufgabe.createdAt.dayString()}", Icons.Outlined.CalendarMonth)
            if (viewModel.permissions.isPrivileged && aufgabe.status != AufgabeStatus.Erledigt) {
                Button(onClick = viewModel::markDone, enabled = isOnline, modifier = Modifier.fillMaxWidth().padding(16.dp)) {
                    Text("Als erledigt markieren")
                }
            }
        }
    }

    if (confirmDelete) {
        ConfirmDialog("Aufgabe löschen?", onConfirm = { viewModel.delete(nav::back) }, onDismiss = { confirmDelete = false })
    }
    ActionErrorDialog(viewModel.action)
}

// endregion

// region Formular

@HiltViewModel
class AufgabeFormViewModel @Inject constructor(session: AppSession, saved: SavedStateHandle) : FeatureViewModel(session) {
    val aufgabe: Aufgabe? = saved.toRoute<AufgabeFormRoute>().aufgabe?.fromArg()
    val users = remote<List<AssignableUser>>("users/assignable")

    var titel by mutableStateOf(aufgabe?.titel.orEmpty())
    var beschreibung by mutableStateOf(aufgabe?.beschreibung.orEmpty())
    var priority by mutableStateOf(AufgabePriority.of(aufgabe?.priority ?: 3))
    var assignedUserId by mutableStateOf(aufgabe?.assignedUserId?.lowercase())
    var assignedName by mutableStateOf(if (aufgabe?.assignedUserId == null) aufgabe?.assignedName.orEmpty() else "")
    var erledigt by mutableStateOf(aufgabe?.status == AufgabeStatus.Erledigt)
    val photo = PhotoUpload(session, "uploads/aufgabe", aufgabe?.fotoUrl)

    val canSave: Boolean get() = titel.isNotBlank() && !photo.isUploading

    init {
        if (aufgabe != null) viewModelScope.launch { users.load(session) }
    }

    fun save(onDone: () -> Unit) = act(onSuccess = { onDone() }) {
        if (aufgabe != null) {
            val input = AufgabeUpdateInput(
                titel = titel.trim(),
                beschreibung = beschreibung.trim(),
                fotoUrl = photo.imageUrl,
                priority = priority.value,
                assignedUserId = assignedUserId,
                assignedName = if (assignedUserId == null) assignedName.nonEmpty else null,
                erledigt = erledigt,
            )
            send(HttpMethod.PUT, "aufgaben/${aufgabe.id}", input)
        } else {
            send(HttpMethod.POST, "aufgaben", AufgabeCreateInput(titel.trim(), beschreibung.trim(), photo.imageUrl, priority.value))
        }
    }
}

@Composable
private fun AufgabeFormScreen(viewModel: AufgabeFormViewModel = hiltViewModel()) {
    val nav = LocalNavigator.current
    FormScaffold(
        title = if (viewModel.aufgabe == null) "Neue Aufgabe" else "Aufgabe bearbeiten",
        onClose = nav::back,
        canSave = viewModel.canSave,
        onSave = { viewModel.save(nav::back) },
        action = viewModel.action,
    ) {
        FormTextField(viewModel.titel, { viewModel.titel = it }, "Titel")
        FormTextField(viewModel.beschreibung, { viewModel.beschreibung = it }, "Beschreibung", minLines = 3)
        DropdownField(
            label = "Priorität",
            options = AufgabePriority.entries,
            selected = viewModel.priority,
            optionLabel = { it.label },
            onSelect = { it?.let { p -> viewModel.priority = p } },
        )
        if (viewModel.aufgabe != null) {
            Text("Zuständig", style = MaterialTheme.typography.titleSmall)
            DropdownField(
                label = "Mitglied",
                options = viewModel.users.value.orEmpty(),
                selected = viewModel.users.value?.firstOrNull { it.id.lowercase() == viewModel.assignedUserId },
                optionLabel = { it.name },
                onSelect = { viewModel.assignedUserId = it?.id?.lowercase() },
                noneLabel = "Niemand / extern",
            )
            if (viewModel.assignedUserId == null) {
                FormTextField(viewModel.assignedName, { viewModel.assignedName = it }, "Name (extern, optional)")
            }
            SwitchRow("Erledigt", viewModel.erledigt, { viewModel.erledigt = it })
        }
        PhotoUploadField(viewModel.photo)
    }
}

// endregion
