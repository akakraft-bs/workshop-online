package de.akakraftbs.android.workshoponline.ui.gemeinschaft

import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.widthIn
import androidx.compose.foundation.lazy.items
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.selection.selectable
import androidx.compose.foundation.verticalScroll
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.automirrored.outlined.KeyboardArrowRight
import androidx.compose.material.icons.filled.ThumbDown
import androidx.compose.material.icons.filled.ThumbUp
import androidx.compose.material.icons.outlined.Add
import androidx.compose.material.icons.outlined.AutoAwesome
import androidx.compose.material.icons.outlined.CalendarMonth
import androidx.compose.material.icons.outlined.Edit
import androidx.compose.material.icons.outlined.Euro
import androidx.compose.material.icons.outlined.ExpandMore
import androidx.compose.material.icons.outlined.Link
import androidx.compose.material.icons.outlined.Person
import androidx.compose.material.icons.outlined.ThumbDown
import androidx.compose.material.icons.outlined.ThumbUp
import androidx.compose.material.icons.outlined.Verified
import androidx.compose.material3.ExtendedFloatingActionButton
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButton
import androidx.compose.material3.ListItem
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.RadioButton
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
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.semantics.Role
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.input.KeyboardCapitalization
import androidx.compose.ui.text.input.KeyboardType
import androidx.compose.ui.text.style.TextAlign
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
import de.akakraftbs.android.workshoponline.model.Tone
import de.akakraftbs.android.workshoponline.model.Wunsch
import de.akakraftbs.android.workshoponline.model.WunschInput
import de.akakraftbs.android.workshoponline.model.WunschStatus
import de.akakraftbs.android.workshoponline.model.nonEmpty
import de.akakraftbs.android.workshoponline.ui.components.ActionErrorDialog
import de.akakraftbs.android.workshoponline.ui.components.AppScaffold
import de.akakraftbs.android.workshoponline.ui.components.DetailRow
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
import de.akakraftbs.android.workshoponline.ui.components.dayString
import de.akakraftbs.android.workshoponline.ui.components.emptyItem
import de.akakraftbs.android.workshoponline.ui.components.fromArg
import de.akakraftbs.android.workshoponline.ui.components.openLink
import de.akakraftbs.android.workshoponline.ui.components.toArg
import de.akakraftbs.android.workshoponline.ui.components.toneColor
import kotlinx.serialization.Serializable
import javax.inject.Inject

@Serializable data object WunschlisteRoute
@Serializable data class WunschDetailRoute(val wunsch: String)
@Serializable data class WunschFormRoute(val wunsch: String? = null)
@Serializable data class WunschCloseRoute(val wunsch: String)

fun NavGraphBuilder.wunschDestinations() {
    composable<WunschlisteRoute> { WunschlisteScreen() }
    composable<WunschDetailRoute> { WunschDetailScreen() }
    composable<WunschFormRoute> { WunschFormScreen() }
    composable<WunschCloseRoute> { WunschCloseScreen() }
}

@Serializable private data class VoteBody(val isUpvote: Boolean)

// region Liste

@HiltViewModel
class WunschlisteViewModel @Inject constructor(session: AppSession) : FeatureViewModel(session) {
    val wuensche = remote<List<Wunsch>>("wunsch")
    var showClosed by mutableStateOf(false)

    override suspend fun reloadAll() = wuensche.load(session)

    fun vote(wunsch: Wunsch, up: Boolean) = act {
        val updated = decode(Wunsch.serializer(), send(HttpMethod.POST, "wunsch/${wunsch.id}/vote", VoteBody(up)))
        wuensche.update { list -> list.map { if (it.id == updated.id) updated else it } }
    }
}

@Composable
private fun WunschlisteScreen(viewModel: WunschlisteViewModel = hiltViewModel()) {
    val nav = LocalNavigator.current
    val isOnline = LocalIsOnline.current
    LaunchedEffect(Unit) { viewModel.reload() }

    AppScaffold(
        title = "Wunschliste",
        onBack = nav::back,
        floatingActionButton = {
            if (isOnline) {
                ExtendedFloatingActionButton(
                    text = { Text("Wunsch") },
                    icon = { Icon(Icons.Outlined.Add, contentDescription = null) },
                    onClick = { nav.open(WunschFormRoute()) },
                )
            }
        },
    ) { padding ->
        RemoteList(viewModel.wuensche, padding, viewModel.isRefreshing, viewModel::refresh) { items ->
            val open = items.filter { it.status == WunschStatus.Offen }
                .sortedWith(compareByDescending<Wunsch> { it.score }.thenByDescending { it.createdAt })
            val closed = items.filter { it.status != WunschStatus.Offen }.sortedByDescending { it.closedAt ?: it.createdAt }
            if (open.isEmpty()) emptyItem(Icons.Outlined.AutoAwesome, "Keine offenen Wünsche", "Was fehlt in der Halle? Schlag es vor!")
            items(open, key = { it.id }) { wunsch ->
                WunschRow(wunsch, onVote = { up -> viewModel.vote(wunsch, up) }) { nav.open(WunschDetailRoute(wunsch.toArg())) }
            }
            if (closed.isNotEmpty()) {
                item(key = "closed-header") {
                    ListItem(
                        modifier = Modifier.clickable { viewModel.showClosed = !viewModel.showClosed },
                        headlineContent = { Text("Erledigt (${closed.size})", color = MaterialTheme.colorScheme.primary) },
                        trailingContent = {
                            Icon(
                                Icons.Outlined.ExpandMore,
                                contentDescription = if (viewModel.showClosed) "Einklappen" else "Aufklappen",
                                modifier = Modifier.rotate(if (viewModel.showClosed) 180f else 0f),
                            )
                        },
                    )
                }
                if (viewModel.showClosed) {
                    items(closed, key = { it.id }) { wunsch ->
                        WunschRow(wunsch, onVote = null) { nav.open(WunschDetailRoute(wunsch.toArg())) }
                    }
                }
            }
        }
    }
    ActionErrorDialog(viewModel.action)
}

@Composable
private fun WunschRow(wunsch: Wunsch, onVote: ((Boolean) -> Unit)?, onClick: () -> Unit) {
    ListItem(
        modifier = Modifier.clickable(onClick = onClick),
        headlineContent = { Text(wunsch.title) },
        supportingContent = {
            Column(verticalArrangement = Arrangement.spacedBy(4.dp)) {
                wunsch.priceText?.let { Text(it) }
                if (wunsch.status != WunschStatus.Offen) StatusPill(wunsch.status.label, wunsch.status.tone)
            }
        },
        trailingContent = {
            if (onVote != null) VoteControl(wunsch, onVote)
            else Text("${wunsch.score}", style = MaterialTheme.typography.titleMedium, color = MaterialTheme.colorScheme.onSurfaceVariant)
        },
    )
}

@Composable
private fun VoteControl(wunsch: Wunsch, onVote: (Boolean) -> Unit) {
    val isOnline = LocalIsOnline.current
    Row(verticalAlignment = Alignment.CenterVertically) {
        IconButton(onClick = { onVote(true) }, enabled = isOnline) {
            Icon(
                if (wunsch.currentUserVote == true) Icons.Filled.ThumbUp else Icons.Outlined.ThumbUp,
                contentDescription = "Dafür",
                tint = if (wunsch.currentUserVote == true) toneColor(Tone.Green) else MaterialTheme.colorScheme.onSurfaceVariant,
            )
        }
        Text("${wunsch.score}", style = MaterialTheme.typography.titleMedium, textAlign = TextAlign.Center, modifier = Modifier.widthIn(min = 24.dp))
        IconButton(onClick = { onVote(false) }, enabled = isOnline) {
            Icon(
                if (wunsch.currentUserVote == false) Icons.Filled.ThumbDown else Icons.Outlined.ThumbDown,
                contentDescription = "Dagegen",
                tint = if (wunsch.currentUserVote == false) toneColor(Tone.Red) else MaterialTheme.colorScheme.onSurfaceVariant,
            )
        }
    }
}

// endregion

// region Detail

@HiltViewModel
class WunschDetailViewModel @Inject constructor(session: AppSession, saved: SavedStateHandle) : FeatureViewModel(session) {
    var wunsch by mutableStateOf<Wunsch>(saved.toRoute<WunschDetailRoute>().wunsch.fromArg())
        private set

    override suspend fun reloadAll() {
        runCatching { session.fetch<List<Wunsch>>("wunsch").value }.getOrNull()
            ?.firstOrNull { it.id == wunsch.id }
            ?.let { wunsch = it }
    }

    fun vote(up: Boolean) = act {
        wunsch = decode(Wunsch.serializer(), send(HttpMethod.POST, "wunsch/${wunsch.id}/vote", VoteBody(up)))
    }
}

@Composable
private fun WunschDetailScreen(viewModel: WunschDetailViewModel = hiltViewModel()) {
    val nav = LocalNavigator.current
    val context = LocalContext.current
    val isOnline = LocalIsOnline.current
    val wunsch = viewModel.wunsch
    val permissions = viewModel.permissions
    LaunchedEffect(Unit) { viewModel.reload() }

    AppScaffold(
        title = "Wunsch",
        onBack = nav::back,
        actions = {
            if (wunsch.status == WunschStatus.Offen) {
                OverflowMenu(
                    enabled = isOnline,
                    actions = listOfNotNull(
                        if (permissions.isOwnerOrPrivileged(wunsch.createdByUserId)) {
                            MenuAction("Bearbeiten", Icons.Outlined.Edit) { nav.open(WunschFormRoute(wunsch.toArg())) }
                        } else null,
                        if (permissions.isPrivileged) {
                            MenuAction("Abschließen", Icons.Outlined.Verified) { nav.open(WunschCloseRoute(wunsch.toArg())) }
                        } else null,
                    ),
                )
            }
        },
    ) { padding ->
        Column(Modifier.fillMaxSize().padding(top = padding.calculateTopPadding()).verticalScroll(rememberScrollState()).padding(bottom = 24.dp)) {
            Column(Modifier.padding(16.dp), verticalArrangement = Arrangement.spacedBy(8.dp)) {
                Text(wunsch.title, style = MaterialTheme.typography.headlineSmall, fontWeight = FontWeight.SemiBold)
                wunsch.description.nonEmpty?.let { Text(it) }
                Text(
                    "Vorgeschlagen von ${wunsch.createdByName} am ${wunsch.createdAt.dayString()}",
                    style = MaterialTheme.typography.bodySmall,
                    color = MaterialTheme.colorScheme.onSurfaceVariant,
                )
            }
            wunsch.priceText?.let { DetailRow("Preis", it, Icons.Outlined.Euro) }
            wunsch.link.nonEmpty?.let { url ->
                ListItem(
                    modifier = Modifier.clickable { context.openLink(url) },
                    leadingContent = { Icon(Icons.Outlined.Link, contentDescription = null) },
                    headlineContent = { Text(url, maxLines = 1) },
                    trailingContent = { Icon(Icons.AutoMirrored.Outlined.KeyboardArrowRight, contentDescription = null) },
                )
            }

            SectionHeader("Abstimmung")
            ListItem(headlineContent = { Text("Dafür") }, trailingContent = { Text("${wunsch.upVotes}", style = MaterialTheme.typography.titleMedium) })
            ListItem(headlineContent = { Text("Dagegen") }, trailingContent = { Text("${wunsch.downVotes}", style = MaterialTheme.typography.titleMedium) })
            if (wunsch.status == WunschStatus.Offen) {
                Row(Modifier.fillMaxWidth(), horizontalArrangement = Arrangement.Center) { VoteControl(wunsch, viewModel::vote) }
            }

            if (wunsch.status != WunschStatus.Offen) {
                SectionHeader("Abschluss")
                Column(Modifier.padding(horizontal = 16.dp)) { StatusPill(wunsch.status.label, wunsch.status.tone) }
                wunsch.closedByName?.let { DetailRow("Von", it, Icons.Outlined.Person) }
                wunsch.closedAt?.let { DetailRow("Am", it.dayString(), Icons.Outlined.CalendarMonth) }
                wunsch.closeNote.nonEmpty?.let { Text(it, modifier = Modifier.padding(16.dp)) }
            }
        }
    }
    ActionErrorDialog(viewModel.action)
}

// endregion

// region Formular & Abschluss

@HiltViewModel
class WunschFormViewModel @Inject constructor(session: AppSession, saved: SavedStateHandle) : FeatureViewModel(session) {
    val wunsch: Wunsch? = saved.toRoute<WunschFormRoute>().wunsch?.fromArg()
    var title by mutableStateOf(wunsch?.title.orEmpty())
    var description by mutableStateOf(wunsch?.description.orEmpty())
    var link by mutableStateOf(wunsch?.link.orEmpty())
    var priceMin by mutableStateOf(wunsch?.priceMin?.formatPrice().orEmpty())
    var priceMax by mutableStateOf(wunsch?.priceMax?.formatPrice().orEmpty())

    val canSave: Boolean get() = title.isNotBlank()

    fun save(onDone: () -> Unit) {
        val input = WunschInput(title.trim(), description.trim(), link.nonEmpty, priceMin.parsePrice(), priceMax.parsePrice())
        act(onSuccess = { onDone() }) {
            if (wunsch != null) send(HttpMethod.PUT, "wunsch/${wunsch.id}", input)
            else send(HttpMethod.POST, "wunsch", input)
        }
    }
}

private fun Double.formatPrice(): String = if (this % 1.0 == 0.0) "%.0f".format(this) else "%.2f".format(this).replace('.', ',')
private fun String.parsePrice(): Double? = trim().replace(',', '.').toDoubleOrNull()

@Composable
private fun WunschFormScreen(viewModel: WunschFormViewModel = hiltViewModel()) {
    val nav = LocalNavigator.current
    FormScaffold(
        title = if (viewModel.wunsch == null) "Neuer Wunsch" else "Wunsch bearbeiten",
        onClose = nav::back,
        canSave = viewModel.canSave,
        onSave = { viewModel.save(nav::back) },
        action = viewModel.action,
    ) {
        FormTextField(viewModel.title, { viewModel.title = it }, "Titel")
        FormTextField(viewModel.description, { viewModel.description = it }, "Beschreibung", minLines = 3)
        FormTextField(viewModel.link, { viewModel.link = it }, "Link (optional)", keyboardType = KeyboardType.Uri, capitalization = KeyboardCapitalization.None)
        Text("Preisspanne", style = MaterialTheme.typography.titleSmall)
        Row(horizontalArrangement = Arrangement.spacedBy(8.dp)) {
            FormTextField(viewModel.priceMin, { viewModel.priceMin = it }, "Von (€)", keyboardType = KeyboardType.Decimal, modifier = Modifier.weight(1f))
            FormTextField(viewModel.priceMax, { viewModel.priceMax = it }, "Bis (€)", keyboardType = KeyboardType.Decimal, modifier = Modifier.weight(1f))
        }
    }
}

@HiltViewModel
class WunschCloseViewModel @Inject constructor(session: AppSession, saved: SavedStateHandle) : FeatureViewModel(session) {
    val wunsch: Wunsch = saved.toRoute<WunschCloseRoute>().wunsch.fromArg()
    var status by mutableStateOf(WunschStatus.Angeschafft)
    var note by mutableStateOf("")

    @Serializable private data class CloseBody(val status: WunschStatus, val closeNote: String?)

    fun save(onDone: () -> Unit) = act(onSuccess = { onDone() }) {
        send(HttpMethod.POST, "wunsch/${wunsch.id}/close", CloseBody(status, note.nonEmpty))
    }
}

@Composable
private fun WunschCloseScreen(viewModel: WunschCloseViewModel = hiltViewModel()) {
    val nav = LocalNavigator.current
    FormScaffold(
        title = "Wunsch abschließen",
        onClose = nav::back,
        canSave = true,
        onSave = { viewModel.save(nav::back) },
        action = viewModel.action,
        saveLabel = "Abschließen",
    ) {
        listOf(WunschStatus.Angeschafft, WunschStatus.Abgelehnt).forEach { status ->
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
        FormTextField(viewModel.note, { viewModel.note = it }, "Notiz (optional)", minLines = 2)
    }
}

// endregion
