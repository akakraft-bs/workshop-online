package de.akakraftbs.android.workshoponline.ui.halle

import androidx.compose.foundation.background
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.PaddingValues
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.lazy.items
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.foundation.verticalScroll
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.automirrored.filled.KeyboardArrowLeft
import androidx.compose.material.icons.automirrored.filled.KeyboardArrowRight
import androidx.compose.material.icons.outlined.Add
import androidx.compose.material.icons.outlined.CalendarMonth
import androidx.compose.material.icons.outlined.DeleteOutline
import androidx.compose.material.icons.outlined.DirectionsCar
import androidx.compose.material.icons.outlined.Edit
import androidx.compose.material.icons.outlined.Link
import androidx.compose.material.icons.outlined.Person
import androidx.compose.material.icons.outlined.Place
import androidx.compose.material.icons.outlined.Schedule
import androidx.compose.material3.ExtendedFloatingActionButton
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButton
import androidx.compose.material3.ListItem
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.SegmentedButton
import androidx.compose.material3.SegmentedButtonDefaults
import androidx.compose.material3.SingleChoiceSegmentedButtonRow
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
import androidx.compose.ui.draw.clip
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.input.KeyboardCapitalization
import androidx.compose.ui.text.input.KeyboardType
import androidx.compose.ui.unit.dp
import androidx.hilt.lifecycle.viewmodel.compose.hiltViewModel
import androidx.lifecycle.SavedStateHandle
import androidx.lifecycle.viewModelScope
import androidx.navigation.NavGraphBuilder
import androidx.navigation.compose.composable
import androidx.navigation.toRoute
import dagger.hilt.android.lifecycle.HiltViewModel
import de.akakraftbs.android.workshoponline.core.auth.AppSession
import de.akakraftbs.android.workshoponline.core.cache.Endpoints
import de.akakraftbs.android.workshoponline.core.cache.Endpoints.CalendarKind
import de.akakraftbs.android.workshoponline.core.cache.remote
import de.akakraftbs.android.workshoponline.core.network.HttpMethod
import de.akakraftbs.android.workshoponline.model.CalendarConfig
import de.akakraftbs.android.workshoponline.model.CalendarEvent
import de.akakraftbs.android.workshoponline.model.CalendarEventInput
import de.akakraftbs.android.workshoponline.model.Fahrzeug
import de.akakraftbs.android.workshoponline.model.nonEmpty
import de.akakraftbs.android.workshoponline.ui.components.ActionErrorDialog
import de.akakraftbs.android.workshoponline.ui.components.AppScaffold
import de.akakraftbs.android.workshoponline.ui.components.ConfirmDialog
import de.akakraftbs.android.workshoponline.ui.components.DateTimeField
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
import de.akakraftbs.android.workshoponline.ui.components.SwitchRow
import de.akakraftbs.android.workshoponline.ui.components.emptyItem
import de.akakraftbs.android.workshoponline.ui.components.fromArg
import de.akakraftbs.android.workshoponline.ui.components.hexColor
import de.akakraftbs.android.workshoponline.ui.components.localDate
import de.akakraftbs.android.workshoponline.ui.components.longDate
import de.akakraftbs.android.workshoponline.ui.components.monthYear
import de.akakraftbs.android.workshoponline.ui.components.openLink
import de.akakraftbs.android.workshoponline.ui.components.startInstant
import de.akakraftbs.android.workshoponline.ui.components.timeString
import de.akakraftbs.android.workshoponline.ui.components.toArg
import de.akakraftbs.android.workshoponline.ui.components.utcDay
import de.akakraftbs.android.workshoponline.ui.components.utcMidnight
import de.akakraftbs.android.workshoponline.ui.components.weekdayDayMonth
import kotlinx.coroutines.launch
import kotlinx.serialization.Serializable
import java.time.Duration
import java.time.Instant
import java.time.LocalDate
import java.time.YearMonth
import javax.inject.Inject

@Serializable data class CalendarEventRoute(val event: String, val config: String? = null)

@Serializable
data class CalendarEventFormRoute(
    val event: String? = null,
    val calendars: String,
    val kind: String,
    val suggestedDay: String,
    /** Wie viele Seiten nach dem Sichern geschlossen werden (Formular bzw. Formular + Detail). */
    val closeCount: Int = 1,
)

fun NavGraphBuilder.kalenderDestinations() {
    composable<CalendarEventRoute> { CalendarEventDetailScreen() }
    composable<CalendarEventFormRoute> { CalendarEventFormScreen() }
}

fun CalendarEvent.timeText(): String {
    val start = start ?: return ""
    if (isAllDay) return "Ganztägig"
    return end?.let { "${start.timeString()} – ${it.timeString()}" } ?: start.timeString()
}

// region Monatsliste

@HiltViewModel
class KalenderViewModel @Inject constructor(session: AppSession, private val saved: SavedStateHandle) : FeatureViewModel(session) {
    var kind by mutableStateOf(saved.get<String>("kind")?.let(CalendarKind::valueOf) ?: CalendarKind.Hallenbelegung)
        private set
    var month by mutableStateOf(Endpoints.currentMonth())
        private set

    val events = remote<List<CalendarEvent>>("calendar/events", Endpoints.calendarEvents(month, kind))
    val configs = remote<List<CalendarConfig>>("calendar/configs")

    val writableCalendars: List<CalendarConfig>
        get() = configs.value.orEmpty()
            .filter { it.calendarType == kind.raw && it.canWrite(permissions) }
            .sortedBy { it.sortOrder }

    fun config(event: CalendarEvent) = configs.value?.firstOrNull { it.googleCalendarId == event.calendarId }

    override suspend fun reloadAll() {
        val list = viewModelScope.launch { events.load(session, "calendar/events", Endpoints.calendarEvents(month, kind)) }
        configs.load(session)
        list.join()
    }

    fun select(newKind: CalendarKind) {
        kind = newKind
        saved["kind"] = newKind.name
        reload()
    }

    fun shift(months: Long) {
        month = month.plusMonths(months)
        reload()
    }

    fun today() {
        month = Endpoints.currentMonth()
        reload()
    }
}

@Composable
fun KalenderScreen(viewModel: KalenderViewModel = hiltViewModel()) {
    val nav = LocalNavigator.current
    val isOnline = LocalIsOnline.current
    LaunchedEffect(Unit) { viewModel.reload() }
    val writable = viewModel.writableCalendars

    AppScaffold(
        title = "Kalender",
        floatingActionButton = {
            if (writable.isNotEmpty() && isOnline) {
                ExtendedFloatingActionButton(
                    text = { Text("Termin") },
                    icon = { Icon(Icons.Outlined.Add, contentDescription = null) },
                    onClick = {
                        nav.open(
                            CalendarEventFormRoute(
                                calendars = writable.toArg(),
                                kind = viewModel.kind.name,
                                suggestedDay = viewModel.month.atDay(1).toString(),
                            ),
                        )
                    },
                )
            }
        },
    ) { padding ->
        Column(Modifier.fillMaxSize().padding(top = padding.calculateTopPadding())) {
            SingleChoiceSegmentedButtonRow(Modifier.fillMaxWidth().padding(horizontal = 16.dp)) {
                CalendarKind.entries.forEachIndexed { index, kind ->
                    SegmentedButton(
                        selected = viewModel.kind == kind,
                        onClick = { viewModel.select(kind) },
                        shape = SegmentedButtonDefaults.itemShape(index, CalendarKind.entries.size),
                    ) { Text(kind.raw) }
                }
            }
            Row(Modifier.fillMaxWidth().padding(horizontal = 8.dp, vertical = 4.dp), verticalAlignment = Alignment.CenterVertically) {
                IconButton(onClick = { viewModel.shift(-1) }) {
                    Icon(Icons.AutoMirrored.Filled.KeyboardArrowLeft, contentDescription = "Vorheriger Monat")
                }
                TextButton(onClick = viewModel::today, modifier = Modifier.weight(1f)) {
                    Text(viewModel.month.atDay(1).monthYear(), style = MaterialTheme.typography.titleMedium)
                }
                IconButton(onClick = { viewModel.shift(1) }) {
                    Icon(Icons.AutoMirrored.Filled.KeyboardArrowRight, contentDescription = "Nächster Monat")
                }
            }
            RemoteList(viewModel.events, PaddingValues(0.dp), viewModel.isRefreshing, viewModel::refresh) { items ->
                val days = items.filter { it.start != null }
                    .groupBy { it.start!!.localDate() }
                    .toSortedMap()
                if (days.isEmpty()) {
                    emptyItem(Icons.Outlined.CalendarMonth, "Keine Termine", "Im ${viewModel.month.atDay(1).monthYear()} ist nichts eingetragen.")
                }
                days.forEach { (day, events) ->
                    item(key = "day-$day") { SectionHeader(day.startInstant().weekdayDayMonth()) }
                    items(events.sortedWith(compareBy({ !it.isAllDay }, { it.start })), key = { it.id }) { event ->
                        CalendarEventRow(event) {
                            nav.open(CalendarEventRoute(event.toArg(), viewModel.config(event)?.toArg()))
                        }
                    }
                }
            }
        }
    }
}

@Composable
fun CalendarEventRow(event: CalendarEvent, onClick: () -> Unit) {
    val color = hexColor(event.calendarColor) ?: MaterialTheme.colorScheme.primary
    ListItem(
        modifier = Modifier.clickable(onClick = onClick),
        leadingContent = {
            Box(Modifier.width(4.dp).height(40.dp).clip(RoundedCornerShape(2.dp)).background(color))
        },
        headlineContent = { Text(event.title) },
        supportingContent = {
            Column {
                Text(listOfNotNull(event.timeText(), event.creatorName.nonEmpty).joinToString(" · "))
                event.fahrzeugLabel.nonEmpty?.let {
                    Row(verticalAlignment = Alignment.CenterVertically, horizontalArrangement = Arrangement.spacedBy(4.dp)) {
                        Icon(Icons.Outlined.DirectionsCar, contentDescription = null, modifier = Modifier.size(14.dp))
                        Text(it, style = MaterialTheme.typography.bodySmall)
                    }
                }
            }
        },
    )
}

// endregion

// region Detail

@HiltViewModel
class CalendarEventDetailViewModel @Inject constructor(session: AppSession, saved: SavedStateHandle) : FeatureViewModel(session) {
    private val route = saved.toRoute<CalendarEventRoute>()
    val event: CalendarEvent = route.event.fromArg()
    val config: CalendarConfig? = route.config?.fromArg()

    val canWrite: Boolean get() = config?.canWrite(permissions) ?: permissions.isPrivileged

    fun delete(onDone: () -> Unit) = act(onSuccess = { onDone() }) {
        send(HttpMethod.DELETE, "calendar/events/${event.calendarId}/${event.id}")
    }
}

@Composable
private fun CalendarEventDetailScreen(viewModel: CalendarEventDetailViewModel = hiltViewModel()) {
    val nav = LocalNavigator.current
    val context = LocalContext.current
    val isOnline = LocalIsOnline.current
    val event = viewModel.event
    var confirmDelete by rememberSaveable { mutableStateOf(false) }

    AppScaffold(
        title = "Termin",
        onBack = nav::back,
        actions = {
            if (viewModel.canWrite) {
                OverflowMenu(
                    enabled = isOnline,
                    actions = listOf(
                        MenuAction("Bearbeiten", Icons.Outlined.Edit) {
                            val kind = CalendarKind.entries.firstOrNull { it.raw == viewModel.config?.calendarType } ?: CalendarKind.Hallenbelegung
                            nav.open(
                                CalendarEventFormRoute(
                                    event = event.toArg(),
                                    calendars = listOfNotNull(viewModel.config).toArg(),
                                    kind = kind.name,
                                    suggestedDay = (event.start ?: Instant.now()).localDate().toString(),
                                    closeCount = 2,
                                ),
                            )
                        },
                        MenuAction("Löschen", Icons.Outlined.DeleteOutline, destructive = true) { confirmDelete = true },
                    ),
                )
            }
        },
    ) { padding ->
        Column(Modifier.fillMaxSize().padding(top = padding.calculateTopPadding()).verticalScroll(rememberScrollState())) {
            Column(Modifier.padding(16.dp), verticalArrangement = Arrangement.spacedBy(6.dp)) {
                Row(verticalAlignment = Alignment.CenterVertically, horizontalArrangement = Arrangement.spacedBy(6.dp)) {
                    Box(Modifier.size(10.dp).clip(CircleShape).background(hexColor(event.calendarColor) ?: MaterialTheme.colorScheme.primary))
                    Text(event.calendarName, style = MaterialTheme.typography.bodyMedium, color = MaterialTheme.colorScheme.onSurfaceVariant)
                }
                Text(event.title, style = MaterialTheme.typography.headlineSmall, fontWeight = FontWeight.SemiBold)
            }
            event.start?.let {
                DetailRow("Datum", it.longDate(), Icons.Outlined.CalendarMonth)
                DetailRow("Zeit", event.timeText(), Icons.Outlined.Schedule)
            }
            event.location.nonEmpty?.let { DetailRow("Ort", it, Icons.Outlined.Place) }
            event.fahrzeugLabel.nonEmpty?.let { DetailRow("Fahrzeug", it, Icons.Outlined.DirectionsCar) }
            event.creatorName.nonEmpty?.let { DetailRow("Eingetragen von", it, Icons.Outlined.Person) }
            event.url.nonEmpty?.let { url ->
                DetailRow("Link", url, Icons.Outlined.Link, Modifier.clickable { context.openLink(url) })
            }
            event.description.nonEmpty?.let {
                SectionHeader("Beschreibung")
                Text(it, modifier = Modifier.padding(horizontal = 16.dp, vertical = 8.dp))
            }
        }
    }

    if (confirmDelete) {
        ConfirmDialog("Termin löschen?", onConfirm = { viewModel.delete { nav.back() } }, onDismiss = { confirmDelete = false })
    }
    ActionErrorDialog(viewModel.action)
}

// endregion

// region Formular

@HiltViewModel
class CalendarEventFormViewModel @Inject constructor(session: AppSession, saved: SavedStateHandle) : FeatureViewModel(session) {
    private val route = saved.toRoute<CalendarEventFormRoute>()
    val event: CalendarEvent? = route.event?.fromArg()
    val calendars: List<CalendarConfig> = route.calendars.fromArg()
    val kind = CalendarKind.valueOf(route.kind)
    val closeCount = route.closeCount
    val fahrzeuge = remote<List<Fahrzeug>>("fahrzeuge")

    private val defaultStart: Instant = run {
        val suggested = LocalDate.parse(route.suggestedDay)
        val base = if (YearMonth.from(suggested) == YearMonth.now()) LocalDate.now() else suggested
        base.atTime(18, 0).atZone(java.time.ZoneId.systemDefault()).toInstant()
    }

    var calendarId by mutableStateOf(event?.calendarId ?: calendars.firstOrNull()?.googleCalendarId.orEmpty())
    var title by mutableStateOf(event?.title.orEmpty())
    var isAllDay by mutableStateOf(event?.isAllDay ?: false)
    var start by mutableStateOf(
        event?.start?.let { if (event.isAllDay) it.utcDay().atStartOfDay(java.time.ZoneId.systemDefault()).toInstant() else it } ?: defaultStart,
    )
        private set
    var end by mutableStateOf(
        event?.end?.let {
            // Ganztägige Termine enden exklusiv um 0 Uhr des Folgetags.
            if (event.isAllDay) it.utcDay().minusDays(1).atStartOfDay(java.time.ZoneId.systemDefault()).toInstant() else it
        } ?: defaultStart.plus(Duration.ofHours(3)),
    )
    var location by mutableStateOf(event?.location.orEmpty())
    var details by mutableStateOf(event?.description.orEmpty())
    var link by mutableStateOf(event?.url.orEmpty())
    var fahrzeugId by mutableStateOf(event?.fahrzeugId?.lowercase())

    val showsFahrzeug: Boolean get() = kind == CalendarKind.Hallenbelegung || event?.fahrzeugId != null
    val canSave: Boolean get() = title.isNotBlank() && calendarId.isNotEmpty() && !end.isBefore(start)

    init {
        viewModelScope.launch { fahrzeuge.load(session) }
    }

    fun updateStart(value: Instant) {
        val previous = start
        start = value
        if (!end.isAfter(value)) end = value.plus(maxOf(Duration.between(previous, end), Duration.ofHours(1)))
    }

    fun save(onDone: () -> Unit) {
        val zone = java.time.ZoneId.systemDefault()
        val startValue = if (isAllDay) start.atZone(zone).toLocalDate().utcMidnight() else start
        val endValue = if (isAllDay) end.atZone(zone).toLocalDate().plusDays(1).utcMidnight() else end
        val input = CalendarEventInput(
            calendarId = if (event == null) calendarId else null,
            title = title.trim(),
            start = startValue,
            end = endValue,
            isAllDay = isAllDay,
            description = details.nonEmpty,
            location = location.nonEmpty,
            url = link.nonEmpty,
            fahrzeugId = fahrzeugId,
        )
        act(onSuccess = { onDone() }) {
            if (event != null) send(HttpMethod.PUT, "calendar/events/${event.calendarId}/${event.id}", input)
            else send(HttpMethod.POST, "calendar/events", input)
        }
    }
}

@Composable
private fun CalendarEventFormScreen(viewModel: CalendarEventFormViewModel = hiltViewModel()) {
    val nav = LocalNavigator.current
    FormScaffold(
        title = if (viewModel.event == null) "Neuer Termin" else "Termin bearbeiten",
        onClose = nav::back,
        canSave = viewModel.canSave,
        onSave = { viewModel.save { nav.back(viewModel.closeCount) } },
        action = viewModel.action,
    ) {
        FormTextField(viewModel.title, { viewModel.title = it }, "Titel")
        if (viewModel.event == null && viewModel.calendars.size > 1) {
            DropdownField(
                label = "Kalender",
                options = viewModel.calendars,
                selected = viewModel.calendars.firstOrNull { it.googleCalendarId == viewModel.calendarId },
                optionLabel = { it.name },
                onSelect = { it?.let { config -> viewModel.calendarId = config.googleCalendarId } },
            )
        }
        SwitchRow("Ganztägig", viewModel.isAllDay, { viewModel.isAllDay = it })
        DateTimeField("Beginn", viewModel.start, viewModel::updateStart, withTime = !viewModel.isAllDay)
        DateTimeField("Ende", viewModel.end, { viewModel.end = it }, withTime = !viewModel.isAllDay)
        if (viewModel.showsFahrzeug) {
            DropdownField(
                label = "Fahrzeug",
                options = viewModel.fahrzeuge.value.orEmpty(),
                selected = viewModel.fahrzeuge.value?.firstOrNull { it.id.lowercase() == viewModel.fahrzeugId },
                optionLabel = { it.label },
                onSelect = { viewModel.fahrzeugId = it?.id?.lowercase() },
                noneLabel = "Keins",
            )
        }
        FormTextField(viewModel.location, { viewModel.location = it }, "Ort")
        FormTextField(viewModel.link, { viewModel.link = it }, "Link", keyboardType = KeyboardType.Uri, capitalization = KeyboardCapitalization.None)
        FormTextField(viewModel.details, { viewModel.details = it }, "Beschreibung", minLines = 3)
    }
}

// endregion
