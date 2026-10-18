package de.akakraftbs.android.workshoponline.ui.verein

import androidx.activity.compose.rememberLauncherForActivityResult
import androidx.activity.result.contract.ActivityResultContracts
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
import androidx.compose.foundation.verticalScroll
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.outlined.Add
import androidx.compose.material.icons.outlined.CalendarMonth
import androidx.compose.material.icons.outlined.CheckCircle
import androidx.compose.material.icons.outlined.Construction
import androidx.compose.material.icons.outlined.DeleteOutline
import androidx.compose.material.icons.outlined.Description
import androidx.compose.material.icons.outlined.Edit
import androidx.compose.material.icons.outlined.Event
import androidx.compose.material.icons.automirrored.outlined.HelpOutline
import androidx.compose.material.icons.outlined.HourglassEmpty
import androidx.compose.material.icons.outlined.Person
import androidx.compose.material.icons.outlined.PlayCircle
import androidx.compose.material.icons.outlined.SportsScore
import androidx.compose.material.icons.outlined.Upload
import androidx.compose.material3.CircularProgressIndicator
import androidx.compose.material3.ExtendedFloatingActionButton
import androidx.compose.material3.FilledTonalButton
import androidx.compose.material3.Icon
import androidx.compose.material3.LinearProgressIndicator
import androidx.compose.material3.ListItem
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Text
import androidx.compose.material3.TextButton
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableIntStateOf
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.rememberCoroutineScope
import androidx.compose.runtime.saveable.rememberSaveable
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.vector.ImageVector
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.text.font.FontWeight
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
import de.akakraftbs.android.workshoponline.core.network.ApiError
import de.akakraftbs.android.workshoponline.core.network.HttpMethod
import de.akakraftbs.android.workshoponline.core.network.PickedFile
import de.akakraftbs.android.workshoponline.core.network.Uploads
import de.akakraftbs.android.workshoponline.model.FileUploadResult
import de.akakraftbs.android.workshoponline.model.Projekt
import de.akakraftbs.android.workshoponline.model.ProjektInput
import de.akakraftbs.android.workshoponline.model.ProjektStatus
import de.akakraftbs.android.workshoponline.model.nonEmpty
import de.akakraftbs.android.workshoponline.ui.components.ActionErrorDialog
import de.akakraftbs.android.workshoponline.ui.components.AppScaffold
import de.akakraftbs.android.workshoponline.ui.components.ConfirmDialog
import de.akakraftbs.android.workshoponline.ui.components.DateField
import de.akakraftbs.android.workshoponline.ui.components.DetailRow
import de.akakraftbs.android.workshoponline.ui.components.DropdownField
import de.akakraftbs.android.workshoponline.ui.components.FeatureViewModel
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
import de.akakraftbs.android.workshoponline.ui.components.emptyItem
import de.akakraftbs.android.workshoponline.ui.components.fromArg
import de.akakraftbs.android.workshoponline.ui.components.openLink
import de.akakraftbs.android.workshoponline.ui.components.sectionHeader
import de.akakraftbs.android.workshoponline.ui.components.toArg
import de.akakraftbs.android.workshoponline.ui.components.toneColor
import de.akakraftbs.android.workshoponline.ui.components.utcDay
import de.akakraftbs.android.workshoponline.ui.components.utcDayString
import de.akakraftbs.android.workshoponline.ui.components.utcMidnight
import de.akakraftbs.android.workshoponline.ui.inventar.Stepper
import kotlinx.coroutines.launch
import kotlinx.serialization.Serializable
import java.time.Duration
import java.time.Instant
import java.time.LocalDate
import javax.inject.Inject

@Serializable data object ProjekteRoute
@Serializable data class ProjektDetailRoute(val projekt: String)
@Serializable data class ProjektFormRoute(val projekt: String? = null)

fun NavGraphBuilder.projekteDestinations() {
    composable<ProjekteRoute> { ProjekteScreen() }
    composable<ProjektDetailRoute> { ProjektDetailScreen() }
    composable<ProjektFormRoute> { ProjektFormScreen() }
}

val ProjektStatus.icon: ImageVector
    get() = when (this) {
        ProjektStatus.Geplant -> Icons.Outlined.Event
        ProjektStatus.Gestartet -> Icons.Outlined.Construction
        ProjektStatus.Abgeschlossen -> Icons.Outlined.CheckCircle
        ProjektStatus.Unknown -> Icons.AutoMirrored.Outlined.HelpOutline
    }

// region Liste

@HiltViewModel
class ProjekteViewModel @Inject constructor(session: AppSession) : FeatureViewModel(session) {
    val projekte = remote<List<Projekt>>("verein/projekte")
    override suspend fun reloadAll() = projekte.load(session)
}

@Composable
private fun ProjekteScreen(viewModel: ProjekteViewModel = hiltViewModel()) {
    val nav = LocalNavigator.current
    val isOnline = LocalIsOnline.current
    LaunchedEffect(Unit) { viewModel.reload() }

    AppScaffold(
        title = "Projekte",
        onBack = nav::back,
        floatingActionButton = {
            if (viewModel.permissions.isPrivileged && isOnline) {
                ExtendedFloatingActionButton(
                    text = { Text("Projekt") },
                    icon = { Icon(Icons.Outlined.Add, contentDescription = null) },
                    onClick = { nav.open(ProjektFormRoute()) },
                )
            }
        },
    ) { padding ->
        RemoteList(viewModel.projekte, padding, viewModel.isRefreshing, viewModel::refresh) { items ->
            if (items.isEmpty()) emptyItem(Icons.Outlined.Construction, "Keine Projekte", "")
            ProjektStatus.selectable.forEach { status ->
                val projekte = items.filter { it.status == status }.sortedBy { it.plannedStartDate }
                if (projekte.isNotEmpty()) {
                    sectionHeader(status.label)
                    items(projekte, key = { it.id }) { projekt ->
                        ListItem(
                            modifier = Modifier.clickable { nav.open(ProjektDetailRoute(projekt.toArg())) },
                            leadingContent = { Icon(projekt.status.icon, contentDescription = null, tint = toneColor(projekt.status.tone)) },
                            headlineContent = { Text(projekt.name) },
                            supportingContent = {
                                Text("${(projekt.actualStartDate ?: projekt.plannedStartDate).utcDayString()} – ${(projekt.actualEndDate ?: projekt.expectedEndDate).utcDayString()}")
                            },
                        )
                    }
                }
            }
        }
    }
}

// endregion

// region Detail

@HiltViewModel
class ProjektDetailViewModel @Inject constructor(session: AppSession, saved: SavedStateHandle) : FeatureViewModel(session) {
    var projekt by mutableStateOf<Projekt>(saved.toRoute<ProjektDetailRoute>().projekt.fromArg())
        private set

    override suspend fun reloadAll() {
        runCatching { session.fetch<List<Projekt>>("verein/projekte").value }.getOrNull()
            ?.firstOrNull { it.id == projekt.id }
            ?.let { projekt = it }
    }

    /** Zeitlicher Fortschritt (nur für gestartete bzw. abgeschlossene Projekte). */
    val progress: Float?
        get() {
            if (projekt.status == ProjektStatus.Abgeschlossen) return 1f
            if (projekt.status != ProjektStatus.Gestartet) return null
            val start = projekt.actualStartDate ?: projekt.plannedStartDate
            val total = Duration.between(start, projekt.expectedEndDate).toMillis()
            if (total <= 0) return null
            return (Duration.between(start, Instant.now()).toMillis().toFloat() / total).coerceIn(0f, 1f)
        }

    fun delete(onDone: () -> Unit) = act(onSuccess = { onDone() }) { send(HttpMethod.DELETE, "verein/projekte/${projekt.id}") }
}

@Composable
private fun ProjektDetailScreen(viewModel: ProjektDetailViewModel = hiltViewModel()) {
    val nav = LocalNavigator.current
    val context = LocalContext.current
    val isOnline = LocalIsOnline.current
    val projekt = viewModel.projekt
    var confirmDelete by rememberSaveable { mutableStateOf(false) }
    LaunchedEffect(Unit) { viewModel.reload() }

    AppScaffold(
        title = "Projekt",
        onBack = nav::back,
        actions = {
            if (viewModel.permissions.isPrivileged) {
                OverflowMenu(
                    enabled = isOnline,
                    actions = listOf(
                        MenuAction("Bearbeiten", Icons.Outlined.Edit) { nav.open(ProjektFormRoute(projekt.toArg())) },
                        MenuAction("Löschen", Icons.Outlined.DeleteOutline, destructive = true) { confirmDelete = true },
                    ),
                )
            }
        },
    ) { padding ->
        Column(Modifier.fillMaxSize().padding(top = padding.calculateTopPadding()).verticalScroll(rememberScrollState()).padding(bottom = 24.dp)) {
            Column(Modifier.padding(16.dp), verticalArrangement = Arrangement.spacedBy(8.dp)) {
                StatusPill(projekt.status.label, projekt.status.tone, projekt.status.icon)
                Text(projekt.name, style = MaterialTheme.typography.headlineSmall, fontWeight = FontWeight.SemiBold)
                projekt.description.nonEmpty?.let { Text(it) }
                viewModel.progress?.let { progress ->
                    Text("Zeitlicher Fortschritt", style = MaterialTheme.typography.labelMedium)
                    LinearProgressIndicator(progress = { progress }, modifier = Modifier.fillMaxWidth(), color = toneColor(projekt.status.tone))
                }
            }
            SectionHeader("Zeitplan")
            DetailRow("Geplanter Start", projekt.plannedStartDate.utcDayString(), Icons.Outlined.CalendarMonth)
            DetailRow("Dauer", "${projekt.durationWeeks} Wochen", Icons.Outlined.HourglassEmpty)
            projekt.actualStartDate?.let { DetailRow("Tatsächlicher Start", it.utcDayString(), Icons.Outlined.PlayCircle) }
            DetailRow("Erwartetes Ende", projekt.expectedEndDate.utcDayString(), Icons.Outlined.SportsScore)
            projekt.actualEndDate?.let { DetailRow("Abgeschlossen", it.utcDayString(), Icons.Outlined.CheckCircle) }
            projekt.projektplanUrl.nonEmpty?.let { url ->
                DetailRow("Projektplan", "Öffnen", Icons.Outlined.Description, Modifier.clickable { context.openLink(url) })
            }
            DetailRow("Angelegt von", "${projekt.createdByName} · ${projekt.createdAt.dayString()}", Icons.Outlined.Person)
        }
    }

    if (confirmDelete) {
        ConfirmDialog("Projekt löschen?", onConfirm = { viewModel.delete(nav::back) }, onDismiss = { confirmDelete = false })
    }
    ActionErrorDialog(viewModel.action)
}

// endregion

// region Formular

@HiltViewModel
class ProjektFormViewModel @Inject constructor(session: AppSession, saved: SavedStateHandle) : FeatureViewModel(session) {
    val projekt: Projekt? = saved.toRoute<ProjektFormRoute>().projekt?.fromArg()
    var name by mutableStateOf(projekt?.name.orEmpty())
    var description by mutableStateOf(projekt?.description.orEmpty())
    var status by mutableStateOf(projekt?.status?.takeIf { it != ProjektStatus.Unknown } ?: ProjektStatus.Geplant)
    var plannedStart by mutableStateOf(projekt?.plannedStartDate?.utcDay() ?: LocalDate.now())
    var durationWeeks by mutableIntStateOf(projekt?.durationWeeks ?: 4)
    var hasActualStart by mutableStateOf(projekt?.actualStartDate != null)
    var actualStart by mutableStateOf(projekt?.actualStartDate?.utcDay() ?: LocalDate.now())
    var projektplanUrl by mutableStateOf(projekt?.projektplanUrl)
    var isUploading by mutableStateOf(false)
        private set

    val canSave: Boolean get() = name.isNotBlank() && !isUploading

    fun upload(file: PickedFile?) {
        isUploading = true
        act {
            try {
                if (file == null) throw ApiError.Transport("Die Datei konnte nicht gelesen werden.")
                val response = upload("uploads/projektplan", file.bytes, file.name, file.mimeType)
                projektplanUrl = decode(FileUploadResult.serializer(), response).resolvedUrl
            } finally {
                isUploading = false
            }
        }
    }

    fun save(onDone: () -> Unit) {
        val input = ProjektInput(
            name = name.trim(),
            description = description.nonEmpty,
            plannedStartDate = plannedStart.utcMidnight(),
            durationWeeks = durationWeeks,
            actualStartDate = if (hasActualStart) actualStart.utcMidnight() else null,
            status = status,
            projektplanUrl = projektplanUrl,
        )
        act(onSuccess = { onDone() }) {
            if (projekt != null) send(HttpMethod.PUT, "verein/projekte/${projekt.id}", input)
            else send(HttpMethod.POST, "verein/projekte", input)
        }
    }
}

@Composable
private fun ProjektFormScreen(viewModel: ProjektFormViewModel = hiltViewModel()) {
    val nav = LocalNavigator.current
    val context = LocalContext.current
    val scope = rememberCoroutineScope()
    val isOnline = LocalIsOnline.current
    val picker = rememberLauncherForActivityResult(ActivityResultContracts.OpenDocument()) { uri ->
        if (uri != null) scope.launch { viewModel.upload(Uploads.read(context, uri)) }
    }

    FormScaffold(
        title = if (viewModel.projekt == null) "Neues Projekt" else "Projekt bearbeiten",
        onClose = nav::back,
        canSave = viewModel.canSave,
        onSave = { viewModel.save(nav::back) },
        action = viewModel.action,
    ) {
        FormTextField(viewModel.name, { viewModel.name = it }, "Name")
        FormTextField(viewModel.description, { viewModel.description = it }, "Beschreibung", minLines = 3)
        DropdownField(
            label = "Status",
            options = ProjektStatus.selectable,
            selected = viewModel.status,
            optionLabel = { it.label },
            onSelect = { it?.let { status -> viewModel.status = status } },
        )
        Text("Zeitplan", style = MaterialTheme.typography.titleSmall)
        DateField("Geplanter Start", viewModel.plannedStart, { viewModel.plannedStart = it })
        Stepper("Dauer (Wochen)", viewModel.durationWeeks, 1..260) { viewModel.durationWeeks = it }
        SwitchRow("Bereits gestartet", viewModel.hasActualStart, { viewModel.hasActualStart = it })
        if (viewModel.hasActualStart) DateField("Tatsächlicher Start", viewModel.actualStart, { viewModel.actualStart = it })

        Text("Projektplan", style = MaterialTheme.typography.titleSmall)
        viewModel.projektplanUrl?.let { url ->
            Row(verticalAlignment = Alignment.CenterVertically) {
                TextButton(onClick = { context.openLink(url) }, modifier = Modifier.weight(1f)) {
                    Icon(Icons.Outlined.Description, contentDescription = null)
                    Text("  " + url.substringAfterLast('/'), maxLines = 1)
                }
                TextButton(onClick = { viewModel.projektplanUrl = null }) {
                    Text("Entfernen", color = MaterialTheme.colorScheme.error)
                }
            }
        }
        Row(verticalAlignment = Alignment.CenterVertically, horizontalArrangement = Arrangement.spacedBy(8.dp)) {
            FilledTonalButton(onClick = { picker.launch(arrayOf("*/*")) }, enabled = isOnline && !viewModel.isUploading) {
                Icon(Icons.Outlined.Upload, contentDescription = null, modifier = Modifier.size(18.dp))
                Text(if (viewModel.projektplanUrl == null) "  Datei hochladen" else "  Datei ersetzen")
            }
            if (viewModel.isUploading) CircularProgressIndicator(Modifier.size(20.dp), strokeWidth = 2.dp)
        }
    }
}

// endregion
