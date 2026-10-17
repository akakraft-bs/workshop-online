package de.akakraftbs.android.workshoponline.ui.inventar

import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.heightIn
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.verticalScroll
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.automirrored.outlined.Undo
import androidx.compose.material.icons.outlined.Build
import androidx.compose.material.icons.outlined.CalendarMonth
import androidx.compose.material.icons.outlined.CheckCircle
import androidx.compose.material.icons.outlined.DeleteOutline
import androidx.compose.material.icons.outlined.Description
import androidx.compose.material.icons.outlined.EditCalendar
import androidx.compose.material.icons.outlined.Edit
import androidx.compose.material.icons.outlined.ErrorOutline
import androidx.compose.material.icons.outlined.Info
import androidx.compose.material.icons.outlined.Person
import androidx.compose.material.icons.outlined.Place
import androidx.compose.material.icons.outlined.Schedule
import androidx.compose.material.icons.outlined.Straighten
import androidx.compose.material3.Button
import androidx.compose.material3.ButtonDefaults
import androidx.compose.material3.CircularProgressIndicator
import androidx.compose.material3.Icon
import androidx.compose.material3.ListItem
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.OutlinedButton
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.saveable.rememberSaveable
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.layout.ContentScale
import androidx.compose.ui.platform.LocalContext
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
import de.akakraftbs.android.workshoponline.core.network.ApiInstant
import de.akakraftbs.android.workshoponline.core.network.HttpMethod
import de.akakraftbs.android.workshoponline.model.Dokument
import de.akakraftbs.android.workshoponline.model.DokumentOrdner
import de.akakraftbs.android.workshoponline.model.StorageLocation
import de.akakraftbs.android.workshoponline.model.Tone
import de.akakraftbs.android.workshoponline.model.Werkzeug
import de.akakraftbs.android.workshoponline.model.WerkzeugAusleihe
import de.akakraftbs.android.workshoponline.model.WerkzeugInput
import de.akakraftbs.android.workshoponline.model.nonEmpty
import de.akakraftbs.android.workshoponline.ui.components.ActionErrorDialog
import de.akakraftbs.android.workshoponline.ui.components.AppScaffold
import de.akakraftbs.android.workshoponline.ui.components.ConfirmDialog
import de.akakraftbs.android.workshoponline.ui.components.DateField
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
import de.akakraftbs.android.workshoponline.ui.components.PhotoUpload
import de.akakraftbs.android.workshoponline.ui.components.PhotoUploadField
import de.akakraftbs.android.workshoponline.ui.components.RemoteImage
import de.akakraftbs.android.workshoponline.ui.components.SectionHeader
import de.akakraftbs.android.workshoponline.ui.components.StatusPill
import de.akakraftbs.android.workshoponline.ui.components.SuggestionTextField
import de.akakraftbs.android.workshoponline.ui.components.dayString
import de.akakraftbs.android.workshoponline.ui.components.fromArg
import de.akakraftbs.android.workshoponline.ui.components.openLink
import de.akakraftbs.android.workshoponline.ui.components.toArg
import de.akakraftbs.android.workshoponline.ui.components.toneColor
import de.akakraftbs.android.workshoponline.ui.components.utcDay
import de.akakraftbs.android.workshoponline.ui.components.utcMidnight
import kotlinx.coroutines.launch
import kotlinx.serialization.Serializable
import java.time.LocalDate
import javax.inject.Inject

@Serializable data class WerkzeugDetailRoute(val werkzeug: String)
@Serializable data class WerkzeugAusleihenRoute(val werkzeug: String, val changeDate: Boolean = false)
@Serializable data class WerkzeugFormRoute(val werkzeug: String? = null, val categories: String = "[]")

fun NavGraphBuilder.werkzeugDestinations() {
    composable<WerkzeugDetailRoute> { WerkzeugDetailScreen() }
    composable<WerkzeugAusleihenRoute> { AusleihenScreen() }
    composable<WerkzeugFormRoute> { WerkzeugFormScreen() }
}

// region Detail

@HiltViewModel
class WerkzeugDetailViewModel @Inject constructor(session: AppSession, saved: SavedStateHandle) : FeatureViewModel(session) {
    var item by mutableStateOf<Werkzeug>(saved.toRoute<WerkzeugDetailRoute>().werkzeug.fromArg())
        private set
    val historie = remote<List<WerkzeugAusleihe>>("werkzeug/${item.id}/historie")
    private val list = remote<List<Werkzeug>>("werkzeug")
    val locations = remote<List<StorageLocation>>("storage-locations")

    val isMyBorrow: Boolean get() = permissions.isOwner(item.borrowedByUserId)
    val canManageBorrow: Boolean get() = !item.isAvailable && (isMyBorrow || permissions.isPrivileged)

    /** Nach Ausleihe/Bearbeitung den aktuellen Stand aus der (zwischengespeicherten) Liste übernehmen. */
    override suspend fun reloadAll() {
        loadAll(historie, list, locations)
        list.value?.firstOrNull { it.id == item.id }?.let { item = it }
    }

    fun returnItem() = act(onSuccess = { reloadAll() }) {
        item = decode(Werkzeug.serializer(), send(HttpMethod.POST, "werkzeug/${item.id}/zurueckgeben"))
    }

    fun delete(onDone: () -> Unit) = act(onSuccess = { onDone() }) {
        send(HttpMethod.DELETE, "werkzeug/${item.id}")
    }
}

@Composable
private fun WerkzeugDetailScreen(viewModel: WerkzeugDetailViewModel = hiltViewModel()) {
    val nav = LocalNavigator.current
    val context = LocalContext.current
    val isOnline = LocalIsOnline.current
    val item = viewModel.item
    var confirmDelete by rememberSaveable { mutableStateOf(false) }
    LaunchedEffect(Unit) { viewModel.reload() }

    AppScaffold(
        title = item.name,
        onBack = nav::back,
        actions = {
            if (viewModel.permissions.isPrivileged) {
                OverflowMenu(
                    enabled = isOnline,
                    actions = listOf(
                        MenuAction("Bearbeiten", Icons.Outlined.Edit) {
                            nav.open(WerkzeugFormRoute(item.toArg(), listOf(item.category).toArg()))
                        },
                        MenuAction("Löschen", Icons.Outlined.DeleteOutline, destructive = true) { confirmDelete = true },
                    ),
                )
            }
        },
    ) { padding ->
        Column(Modifier.fillMaxSize().padding(top = padding.calculateTopPadding()).verticalScroll(rememberScrollState()).padding(bottom = 24.dp)) {
            RemoteImage(
                item.imageUrl,
                contentScale = ContentScale.Fit,
                placeholderIcon = Icons.Outlined.Build,
                modifier = Modifier.fillMaxWidth().heightIn(min = 200.dp, max = 320.dp),
            )
            Column(Modifier.padding(16.dp), verticalArrangement = Arrangement.spacedBy(6.dp)) {
                Row(verticalAlignment = Alignment.CenterVertically) {
                    Text(item.name, style = MaterialTheme.typography.headlineSmall, fontWeight = FontWeight.SemiBold, modifier = Modifier.weight(1f))
                    when {
                        item.isAvailable -> StatusPill("Verfügbar", Tone.Green, Icons.Outlined.CheckCircle)
                        item.isOverdue -> StatusPill("Überfällig", Tone.Red, Icons.Outlined.ErrorOutline)
                        else -> StatusPill("Ausgeliehen", Tone.Orange, Icons.Outlined.Schedule)
                    }
                }
                Text(item.category, color = MaterialTheme.colorScheme.onSurfaceVariant)
                item.description.nonEmpty?.let { Text(it, modifier = Modifier.padding(top = 4.dp)) }
            }
            item.dimensions.nonEmpty?.let { DetailRow("Maße", it, Icons.Outlined.Straighten) }
            item.storageLocation.nonEmpty?.let { location ->
                ListItem(
                    leadingContent = { Icon(Icons.Outlined.Place, contentDescription = null) },
                    overlineContent = { Text("Ablageort") },
                    headlineContent = { StorageLocationLabel(location, locationColor(viewModel.locations.value, location)) },
                )
            }
            item.anleitungFileUrl.nonEmpty?.let { url ->
                DetailRow("Anleitung", item.anleitungFileName ?: "Anleitung", Icons.Outlined.Description, Modifier.clickable { context.openLink(url) })
            }

            if (!item.isAvailable) {
                SectionHeader("Ausleihe")
                item.borrowedByName?.let { name ->
                    ListItem(
                        leadingContent = { Icon(Icons.Outlined.Person, contentDescription = null) },
                        overlineContent = { Text("Ausgeliehen von") },
                        headlineContent = { Text(name) },
                        trailingContent = { if (viewModel.isMyBorrow) StatusPill("Du", Tone.Blue) },
                    )
                }
                item.borrowedAt?.let { DetailRow("Seit", it.dayString(), Icons.Outlined.CalendarMonth) }
                item.expectedReturnAt?.let {
                    ListItem(
                        leadingContent = { Icon(Icons.Outlined.EditCalendar, contentDescription = null) },
                        overlineContent = { Text("Rückgabe bis") },
                        headlineContent = {
                            Text(it.utcDay().let { day -> day.atStartOfDay(java.time.ZoneId.systemDefault()).toInstant().dayString() }, color = if (item.isOverdue) toneColor(Tone.Red) else MaterialTheme.colorScheme.onSurface)
                        },
                    )
                }
            }

            Column(Modifier.fillMaxWidth().padding(16.dp), verticalArrangement = Arrangement.spacedBy(8.dp)) {
                when {
                    item.isAvailable -> Button(
                        onClick = { nav.open(WerkzeugAusleihenRoute(item.toArg())) },
                        enabled = isOnline,
                        modifier = Modifier.fillMaxWidth(),
                    ) { Text("Ausleihen") }
                    viewModel.canManageBorrow -> {
                        Button(onClick = viewModel::returnItem, enabled = isOnline && !viewModel.action.isRunning, modifier = Modifier.fillMaxWidth()) {
                            if (viewModel.action.isRunning) {
                                CircularProgressIndicator(Modifier.size(18.dp), strokeWidth = 2.dp)
                            } else {
                                Icon(Icons.AutoMirrored.Outlined.Undo, contentDescription = null, modifier = Modifier.size(ButtonDefaults.IconSize))
                                Text("  Zurückgeben")
                            }
                        }
                        OutlinedButton(
                            onClick = { nav.open(WerkzeugAusleihenRoute(item.toArg(), changeDate = true)) },
                            enabled = isOnline,
                            modifier = Modifier.fillMaxWidth(),
                        ) { Text("Rückgabedatum ändern") }
                    }
                    else -> Row(horizontalArrangement = Arrangement.spacedBy(8.dp)) {
                        Icon(Icons.Outlined.Info, contentDescription = null, tint = MaterialTheme.colorScheme.onSurfaceVariant)
                        FormHint("Nur die ausleihende Person oder der Vorstand kann das Werkzeug zurückgeben.")
                    }
                }
            }

            SectionHeader("Ausleih-Historie")
            val entries = viewModel.historie.value
            when {
                entries == null && viewModel.historie.failure != null ->
                    Text("Historie nicht verfügbar.", modifier = Modifier.padding(16.dp), color = MaterialTheme.colorScheme.onSurfaceVariant)
                entries == null -> CircularProgressIndicator(Modifier.padding(16.dp))
                entries.isEmpty() ->
                    Text("Noch keine Ausleihen erfasst.", modifier = Modifier.padding(16.dp), color = MaterialTheme.colorScheme.onSurfaceVariant)
                else -> entries.forEach { entry ->
                    ListItem(
                        headlineContent = { Text(entry.userName) },
                        supportingContent = {
                            Text("${entry.borrowedAt.dayString()} – ${entry.returnedAt?.dayString() ?: "noch ausgeliehen"}")
                        },
                        trailingContent = { if (viewModel.permissions.isOwner(entry.userId)) StatusPill("Du", Tone.Blue) },
                    )
                }
            }
        }
    }

    if (confirmDelete) {
        ConfirmDialog("„${item.name}“ löschen?", onConfirm = { viewModel.delete(nav::back) }, onDismiss = { confirmDelete = false })
    }
    ActionErrorDialog(viewModel.action)
}

// endregion

// region Ausleihen / Rückgabedatum

@HiltViewModel
class AusleihenViewModel @Inject constructor(session: AppSession, saved: SavedStateHandle) : FeatureViewModel(session) {
    private val route = saved.toRoute<WerkzeugAusleihenRoute>()
    val werkzeug: Werkzeug = route.werkzeug.fromArg()
    val changeDate = route.changeDate
    private val tomorrow: LocalDate = LocalDate.now().plusDays(1)
    var returnDate by mutableStateOf(if (changeDate) werkzeug.expectedReturnAt?.utcDay() ?: tomorrow else tomorrow)

    val canSave: Boolean get() = !returnDate.isBefore(tomorrow)

    @Serializable private data class ReturnDateBody(val expectedReturnAt: ApiInstant)

    fun submit(onDone: () -> Unit) {
        val body = ReturnDateBody(returnDate.utcMidnight())
        act(onSuccess = { onDone() }) {
            if (changeDate) send(HttpMethod.PATCH, "werkzeug/${werkzeug.id}/rueckgabedatum", body)
            else send(HttpMethod.POST, "werkzeug/${werkzeug.id}/ausleihen", body)
        }
    }
}

@Composable
private fun AusleihenScreen(viewModel: AusleihenViewModel = hiltViewModel()) {
    val nav = LocalNavigator.current
    FormScaffold(
        title = if (viewModel.changeDate) "Rückgabedatum ändern" else "${viewModel.werkzeug.name} ausleihen",
        onClose = nav::back,
        canSave = viewModel.canSave,
        onSave = { viewModel.submit(nav::back) },
        action = viewModel.action,
        saveLabel = if (viewModel.changeDate) "Speichern" else "Ausleihen",
    ) {
        DateField("Rückgabe bis", viewModel.returnDate, { viewModel.returnDate = it })
        FormHint(
            when {
                !viewModel.canSave -> "Das Rückgabedatum muss frühestens morgen sein."
                viewModel.changeDate -> "Das neue Datum ist für alle sichtbar."
                else -> "Bitte gib das Werkzeug spätestens an diesem Tag zurück."
            },
        )
    }
}

// endregion

// region Formular

@HiltViewModel
class WerkzeugFormViewModel @Inject constructor(session: AppSession, saved: SavedStateHandle) : FeatureViewModel(session) {
    private val route = saved.toRoute<WerkzeugFormRoute>()
    val werkzeug: Werkzeug? = route.werkzeug?.fromArg()
    val categories: List<String> = route.categories.fromArg()
    val dokumente = remote<List<DokumentOrdner>>("verein/dokumente")
    val locations = remote<List<StorageLocation>>("storage-locations")

    var name by mutableStateOf(werkzeug?.name.orEmpty())
    var category by mutableStateOf(werkzeug?.category.orEmpty())
    var description by mutableStateOf(werkzeug?.description.orEmpty())
    var dimensions by mutableStateOf(werkzeug?.dimensions.orEmpty())
    var storageLocation by mutableStateOf(werkzeug?.storageLocation.orEmpty())
    var anleitungDokumentId by mutableStateOf(werkzeug?.anleitungDokumentId)
    val photo = PhotoUpload(session, "uploads/werkzeug", werkzeug?.imageUrl, werkzeug?.thumbnailUrl)

    val canSave: Boolean get() = name.isNotBlank() && category.isNotBlank() && !photo.isUploading

    /** Alle Dokumente als „Ordner / Datei“ für die Auswahl der Anleitung. */
    val documentOptions: List<Pair<String, Dokument>>
        get() = dokumente.value.orEmpty().flatMap { ordner -> ordner.dokumente.map { "${ordner.name} / ${it.fileName}" to it } }

    init {
        viewModelScope.launch { loadAll(dokumente, locations) }
    }

    fun save(onDone: () -> Unit) {
        val input = WerkzeugInput(
            name = name.trim(),
            description = description.trim(),
            category = category.trim(),
            imageUrl = photo.imageUrl,
            thumbnailUrl = photo.thumbnailUrl,
            dimensions = dimensions.nonEmpty,
            storageLocation = storageLocation.nonEmpty,
            anleitungDokumentId = anleitungDokumentId,
        )
        act(onSuccess = { onDone() }) {
            if (werkzeug != null) send(HttpMethod.PUT, "werkzeug/${werkzeug.id}", input)
            else send(HttpMethod.POST, "werkzeug", input)
        }
    }
}

@Composable
private fun WerkzeugFormScreen(viewModel: WerkzeugFormViewModel = hiltViewModel()) {
    val nav = LocalNavigator.current
    FormScaffold(
        title = if (viewModel.werkzeug == null) "Neues Werkzeug" else "Werkzeug bearbeiten",
        onClose = nav::back,
        canSave = viewModel.canSave,
        onSave = { viewModel.save(nav::back) },
        action = viewModel.action,
    ) {
        FormTextField(viewModel.name, { viewModel.name = it }, "Name")
        SuggestionTextField(viewModel.category, { viewModel.category = it }, "Kategorie", viewModel.categories)
        FormTextField(viewModel.description, { viewModel.description = it }, "Beschreibung", minLines = 3)
        FormTextField(viewModel.dimensions, { viewModel.dimensions = it }, "Maße")
        SuggestionTextField(
            viewModel.storageLocation,
            { viewModel.storageLocation = it },
            "Ablageort",
            viewModel.locations.value.orEmpty().map { it.name },
        )
        PhotoUploadField(viewModel.photo)
        val options = viewModel.documentOptions
        DropdownField(
            label = "Anleitung",
            options = options,
            selected = options.firstOrNull { it.second.id == viewModel.anleitungDokumentId },
            optionLabel = { it.first },
            onSelect = { viewModel.anleitungDokumentId = it?.second?.id },
            noneLabel = "Keine",
        )
    }
}

// endregion
