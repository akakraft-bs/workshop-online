package de.akakraftbs.android.workshoponline.ui.halle

import android.content.Context
import android.content.Intent
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.FlowRow
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.lazy.items
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.automirrored.outlined.MenuBook
import androidx.compose.material.icons.outlined.Add
import androidx.compose.material.icons.outlined.BarChart
import androidx.compose.material.icons.outlined.DeleteOutline
import androidx.compose.material.icons.outlined.DirectionsCar
import androidx.compose.material.icons.outlined.FamilyRestroom
import androidx.compose.material.icons.outlined.PersonAdd
import androidx.compose.material.icons.outlined.Share
import androidx.compose.material.icons.outlined.TableChart
import androidx.compose.material3.Button
import androidx.compose.material3.ButtonDefaults
import androidx.compose.material3.CircularProgressIndicator
import androidx.compose.material3.ExtendedFloatingActionButton
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButton
import androidx.compose.material3.ListItem
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.OutlinedButton
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateListOf
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.saveable.rememberSaveable
import androidx.compose.runtime.setValue
import androidx.compose.ui.Modifier
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.text.style.TextOverflow
import androidx.compose.ui.unit.dp
import androidx.core.content.FileProvider
import androidx.hilt.lifecycle.viewmodel.compose.hiltViewModel
import androidx.lifecycle.SavedStateHandle
import androidx.lifecycle.viewModelScope
import androidx.navigation.NavGraphBuilder
import androidx.navigation.compose.composable
import androidx.navigation.toRoute
import dagger.hilt.android.lifecycle.HiltViewModel
import dagger.hilt.android.qualifiers.ApplicationContext
import de.akakraftbs.android.workshoponline.core.auth.AppSession
import de.akakraftbs.android.workshoponline.core.cache.Endpoints
import de.akakraftbs.android.workshoponline.core.cache.remote
import de.akakraftbs.android.workshoponline.core.network.DateParsing
import de.akakraftbs.android.workshoponline.core.network.HttpMethod
import de.akakraftbs.android.workshoponline.model.Fahrzeug
import de.akakraftbs.android.workshoponline.model.GastschraubenArt
import de.akakraftbs.android.workshoponline.model.HallenbuchEintrag
import de.akakraftbs.android.workshoponline.model.HallenbuchInput
import de.akakraftbs.android.workshoponline.model.PagedResult
import de.akakraftbs.android.workshoponline.model.Tone
import de.akakraftbs.android.workshoponline.model.nonEmpty
import de.akakraftbs.android.workshoponline.ui.components.ActionErrorDialog
import de.akakraftbs.android.workshoponline.ui.components.AppScaffold
import de.akakraftbs.android.workshoponline.ui.components.ConfirmDialog
import de.akakraftbs.android.workshoponline.ui.components.DateField
import de.akakraftbs.android.workshoponline.ui.components.DateTimeField
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
import de.akakraftbs.android.workshoponline.ui.components.StatusPill
import de.akakraftbs.android.workshoponline.ui.components.SwitchRow
import de.akakraftbs.android.workshoponline.ui.components.emptyItem
import de.akakraftbs.android.workshoponline.ui.components.fromArg
import de.akakraftbs.android.workshoponline.ui.components.hours
import de.akakraftbs.android.workshoponline.ui.components.shortWeekdayDay
import de.akakraftbs.android.workshoponline.ui.components.startInstant
import de.akakraftbs.android.workshoponline.ui.components.timeString
import de.akakraftbs.android.workshoponline.ui.components.toArg
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.launch
import kotlinx.coroutines.withContext
import kotlinx.serialization.Serializable
import java.io.File
import java.time.Duration
import java.time.Instant
import java.time.LocalDate
import javax.inject.Inject

@Serializable data class HallenbuchFormRoute(val eintrag: String? = null)
@Serializable data object HallenbuchStatistikRoute

fun NavGraphBuilder.hallenbuchDestinations() {
    composable<HallenbuchFormRoute> { HallenbuchFormScreen() }
    composable<HallenbuchStatistikRoute> { HallenbuchStatistikScreen() }
}

// region Liste

@HiltViewModel
class HallenbuchViewModel @Inject constructor(session: AppSession) : FeatureViewModel(session) {
    val firstPage = remote<PagedResult<HallenbuchEintrag>>("hallenbuch", Endpoints.hallenbuchFirstPage)
    val morePages = mutableStateListOf<HallenbuchEintrag>()
    private var nextPage = 1
    var isLoadingMore by mutableStateOf(false)
        private set

    override suspend fun reloadAll() {
        morePages.clear()
        nextPage = 1
        firstPage.load(session)
    }

    fun loadMore() {
        viewModelScope.launch {
            isLoadingMore = true
            runCatching {
                session.client.get<PagedResult<HallenbuchEintrag>>(
                    "hallenbuch",
                    listOf("page" to "$nextPage", "pageSize" to "${Endpoints.HALLENBUCH_PAGE_SIZE}"),
                )
            }.onSuccess {
                morePages += it.items
                nextPage += 1
            }
            isLoadingMore = false
        }
    }

    /** Mitglieder: eigene Einträge innerhalb von 7 Tagen. Vorstand: alle. */
    fun canEdit(eintrag: HallenbuchEintrag): Boolean =
        permissions.isPrivileged ||
            (permissions.isOwner(eintrag.userId) && Duration.between(eintrag.createdAt, Instant.now()) < Duration.ofDays(7))

    fun delete(eintrag: HallenbuchEintrag) = act(onSuccess = { reloadAll() }) {
        send(HttpMethod.DELETE, "hallenbuch/${eintrag.id}")
    }
}

@Composable
fun HallenbuchScreen(viewModel: HallenbuchViewModel = hiltViewModel()) {
    val nav = LocalNavigator.current
    val isOnline = LocalIsOnline.current
    var deleting by rememberSaveable { mutableStateOf<String?>(null) }
    LaunchedEffect(Unit) { viewModel.reload() }

    AppScaffold(
        title = "Hallenbuch",
        actions = {
            if (viewModel.permissions.isPrivileged) {
                IconButton(onClick = { nav.open(HallenbuchStatistikRoute) }) {
                    Icon(Icons.Outlined.BarChart, contentDescription = "Statistik")
                }
            }
        },
        floatingActionButton = {
            if (isOnline) {
                ExtendedFloatingActionButton(
                    text = { Text("Eintrag") },
                    icon = { Icon(Icons.Outlined.Add, contentDescription = null) },
                    onClick = { nav.open(HallenbuchFormRoute()) },
                )
            }
        },
    ) { padding ->
        RemoteList(viewModel.firstPage, padding, viewModel.isRefreshing, viewModel::refresh) { page ->
            val entries = page.items + viewModel.morePages
            if (entries.isEmpty()) {
                emptyItem(Icons.AutoMirrored.Outlined.MenuBook, "Noch keine Einträge", "Trag ein, wann du in der Halle geschraubt hast.")
            }
            items(entries, key = { it.id }) { eintrag ->
                val editable = viewModel.canEdit(eintrag)
                HallenbuchRow(
                    eintrag = eintrag,
                    onClick = if (editable && isOnline) ({ nav.open(HallenbuchFormRoute(eintrag.toArg())) }) else null,
                    trailing = {
                        if (editable && isOnline) {
                            OverflowMenu(
                                listOf(MenuAction("Löschen", Icons.Outlined.DeleteOutline, destructive = true) { deleting = eintrag.id }),
                            )
                        }
                    },
                )
            }
            if (entries.size < page.total) {
                item(key = "more") {
                    OutlinedButton(
                        onClick = viewModel::loadMore,
                        enabled = isOnline && !viewModel.isLoadingMore,
                        modifier = Modifier.fillMaxWidth().padding(16.dp),
                    ) {
                        if (viewModel.isLoadingMore) CircularProgressIndicator(Modifier.size(18.dp), strokeWidth = 2.dp)
                        else Text("Weitere laden (${page.total - entries.size})")
                    }
                }
            }
        }
    }

    deleting?.let { id ->
        val eintrag = (viewModel.firstPage.value?.items.orEmpty() + viewModel.morePages).firstOrNull { it.id == id }
        ConfirmDialog(
            "Eintrag löschen?",
            onConfirm = { eintrag?.let(viewModel::delete) },
            onDismiss = { deleting = null },
        )
    }
    ActionErrorDialog(viewModel.action)
}

@Composable
fun HallenbuchRow(eintrag: HallenbuchEintrag, onClick: (() -> Unit)?, trailing: @Composable () -> Unit = {}) {
    ListItem(
        modifier = if (onClick != null) Modifier.clickable(onClick = onClick) else Modifier,
        overlineContent = {
            Text("${eintrag.start.shortWeekdayDay()}, ${eintrag.start.timeString()} – ${eintrag.end.timeString()} · ${hours(eintrag.duration)}")
        },
        headlineContent = { Text(eintrag.userName) },
        supportingContent = {
            Column(verticalArrangement = Arrangement.spacedBy(6.dp)) {
                eintrag.description.nonEmpty?.let { Text(it, maxLines = 3, overflow = TextOverflow.Ellipsis) }
                if (eintrag.hatGastgeschraubt || eintrag.hatFamiliegeschraubt || eintrag.fahrzeugLabel.nonEmpty != null) {
                    FlowRow(horizontalArrangement = Arrangement.spacedBy(6.dp), verticalArrangement = Arrangement.spacedBy(4.dp)) {
                        if (eintrag.hatGastgeschraubt) {
                            StatusPill("Gast", if (eintrag.gastschraubenBezahlt == true) Tone.Green else Tone.Orange, Icons.Outlined.PersonAdd)
                        }
                        if (eintrag.hatFamiliegeschraubt) StatusPill("Familie", Tone.Purple, Icons.Outlined.FamilyRestroom)
                        eintrag.fahrzeugLabel.nonEmpty?.let { StatusPill(it, Tone.Gray, Icons.Outlined.DirectionsCar) }
                    }
                }
            }
        },
        trailingContent = trailing,
    )
}

// endregion

// region Formular

@HiltViewModel
class HallenbuchFormViewModel @Inject constructor(session: AppSession, saved: SavedStateHandle) : FeatureViewModel(session) {
    val eintrag: HallenbuchEintrag? = saved.toRoute<HallenbuchFormRoute>().eintrag?.fromArg()
    val fahrzeuge = remote<List<Fahrzeug>>("fahrzeuge")

    var start by mutableStateOf(eintrag?.start ?: Instant.now().minus(Duration.ofHours(3)))
    var end by mutableStateOf(eintrag?.end ?: Instant.now())
    var description by mutableStateOf(eintrag?.description.orEmpty())
    var hatGastgeschraubt by mutableStateOf(eintrag?.hatGastgeschraubt ?: false)
    var gastschraubenArt by mutableStateOf(eintrag?.gastschraubenArt ?: GastschraubenArt.KastenPremiumbier)
    var gastschraubenBezahlt by mutableStateOf(eintrag?.gastschraubenBezahlt ?: false)
    var hatFamiliegeschraubt by mutableStateOf(eintrag?.hatFamiliegeschraubt ?: false)
    var fahrzeugId by mutableStateOf(eintrag?.fahrzeugId?.lowercase())

    val canSave: Boolean get() = end.isAfter(start)

    init {
        viewModelScope.launch {
            fahrzeuge.load(session)
            if (eintrag == null && fahrzeugId == null) {
                fahrzeugId = fahrzeuge.value?.firstOrNull { it.istStandard }?.id?.lowercase()
            }
        }
    }

    fun save(onDone: () -> Unit) {
        val input = HallenbuchInput(
            start = start,
            end = end,
            description = description.trim(),
            hatGastgeschraubt = hatGastgeschraubt,
            gastschraubenArt = if (hatGastgeschraubt) gastschraubenArt else null,
            gastschraubenBezahlt = if (hatGastgeschraubt) gastschraubenBezahlt else null,
            hatFamiliegeschraubt = hatFamiliegeschraubt,
            fahrzeugId = fahrzeugId,
        )
        act(onSuccess = { onDone() }) {
            if (eintrag != null) send(HttpMethod.PUT, "hallenbuch/${eintrag.id}", input)
            else send(HttpMethod.POST, "hallenbuch", input)
        }
    }
}

@Composable
private fun HallenbuchFormScreen(viewModel: HallenbuchFormViewModel = hiltViewModel()) {
    val nav = LocalNavigator.current
    FormScaffold(
        title = if (viewModel.eintrag == null) "Neuer Eintrag" else "Eintrag bearbeiten",
        onClose = nav::back,
        canSave = viewModel.canSave,
        onSave = { viewModel.save(nav::back) },
        action = viewModel.action,
    ) {
        DateTimeField("Beginn", viewModel.start, { viewModel.start = it })
        DateTimeField("Ende", viewModel.end, { viewModel.end = it })
        FormHint(
            if (viewModel.canSave) "Dauer: ${hours(Duration.between(viewModel.start, viewModel.end))}"
            else "Das Ende muss nach dem Beginn liegen.",
        )
        FormTextField(viewModel.description, { viewModel.description = it }, "Was hast du gemacht?", minLines = 3)
        DropdownField(
            label = "Fahrzeug",
            options = viewModel.fahrzeuge.value.orEmpty(),
            selected = viewModel.fahrzeuge.value?.firstOrNull { it.id.lowercase() == viewModel.fahrzeugId },
            optionLabel = { it.label },
            onSelect = { viewModel.fahrzeugId = it?.id?.lowercase() },
            noneLabel = "Keins",
        )
        SwitchRow("Gast hat geschraubt", viewModel.hatGastgeschraubt, { viewModel.hatGastgeschraubt = it })
        if (viewModel.hatGastgeschraubt) {
            DropdownField(
                label = "Gegenleistung",
                options = GastschraubenArt.selectable,
                selected = viewModel.gastschraubenArt,
                optionLabel = { it.label },
                onSelect = { it?.let { art -> viewModel.gastschraubenArt = art } },
            )
            SwitchRow("Bezahlt", viewModel.gastschraubenBezahlt, { viewModel.gastschraubenBezahlt = it })
        }
        SwitchRow("Familie hat geschraubt", viewModel.hatFamiliegeschraubt, { viewModel.hatFamiliegeschraubt = it })
    }
}

// endregion

// region Statistik

/**
 * Wie in der Web-App: Stunden je Nutzer (eigen/Gast/Familie) als CSV-Datei für einen Zeitraum.
 * Der Server liefert die Statistik nur als CSV, daher Teilen/Sichern statt Anzeige.
 */
@HiltViewModel
class HallenbuchStatistikViewModel @Inject constructor(
    session: AppSession,
    @param:ApplicationContext private val context: Context,
) : FeatureViewModel(session) {
    var from by mutableStateOf(LocalDate.now().withDayOfYear(1))
    var to by mutableStateOf(LocalDate.now())
    var file by mutableStateOf<File?>(null)
        private set

    fun export() = act {
        val csv = getData(
            "hallenbuch/statistik",
            listOf(
                "from" to DateParsing.format(from.startInstant()),
                "to" to DateParsing.format(to.plusDays(1).startInstant()),
            ),
        )
        file = withContext(Dispatchers.IO) {
            File(context.cacheDir, "exports").apply { mkdirs() }
                .resolve("hallenbuch-statistik_${from}_$to.csv")
                .apply { writeText(csv) }
        }
    }

    fun resetFile() {
        file = null
    }
}

@Composable
private fun HallenbuchStatistikScreen(viewModel: HallenbuchStatistikViewModel = hiltViewModel()) {
    val nav = LocalNavigator.current
    val context = LocalContext.current
    val isOnline = LocalIsOnline.current

    AppScaffold(title = "Statistik", onBack = nav::back) { padding ->
        Column(
            Modifier.fillMaxSize().padding(top = padding.calculateTopPadding()).padding(16.dp),
            verticalArrangement = Arrangement.spacedBy(12.dp),
        ) {
            DateField("Von", viewModel.from, { viewModel.from = it; viewModel.resetFile() })
            DateField("Bis", viewModel.to, { viewModel.to = it; viewModel.resetFile() })
            FormHint("Die CSV-Datei enthält je Mitglied die eigenen Stunden sowie Gast- und Familien-Stunden.")
            val file = viewModel.file
            if (file != null) {
                Button(onClick = { context.shareFile(file, "text/csv") }) {
                    Icon(Icons.Outlined.Share, contentDescription = null, modifier = Modifier.size(ButtonDefaults.IconSize))
                    Text("  CSV teilen oder sichern")
                }
            } else {
                Button(onClick = viewModel::export, enabled = isOnline && !viewModel.action.isRunning && !viewModel.to.isBefore(viewModel.from)) {
                    if (viewModel.action.isRunning) {
                        CircularProgressIndicator(Modifier.size(18.dp), strokeWidth = 2.dp)
                    } else {
                        Icon(Icons.Outlined.TableChart, contentDescription = null, modifier = Modifier.size(ButtonDefaults.IconSize))
                        Text("  CSV erstellen")
                    }
                }
            }
        }
    }
    ActionErrorDialog(viewModel.action)
}

/** Teilt eine Datei aus `cacheDir/exports` über den FileProvider. */
fun Context.shareFile(file: File, mimeType: String) {
    val uri = FileProvider.getUriForFile(this, "$packageName.files", file)
    val intent = Intent(Intent.ACTION_SEND)
        .setType(mimeType)
        .putExtra(Intent.EXTRA_STREAM, uri)
        .addFlags(Intent.FLAG_GRANT_READ_URI_PERMISSION)
    startActivity(Intent.createChooser(intent, file.name))
}

// endregion
