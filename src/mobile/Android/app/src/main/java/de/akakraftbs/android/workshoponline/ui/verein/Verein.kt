package de.akakraftbs.android.workshoponline.ui.verein

import android.content.ClipData
import android.content.ClipboardManager
import android.content.Context
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.lazy.items
import androidx.compose.foundation.text.selection.SelectionContainer
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.outlined.Add
import androidx.compose.material.icons.outlined.Check
import androidx.compose.material.icons.outlined.ContentCopy
import androidx.compose.material.icons.outlined.DeleteOutline
import androidx.compose.material.icons.outlined.Edit
import androidx.compose.material.icons.outlined.Key
import androidx.compose.material.icons.outlined.Phone
import androidx.compose.material.icons.outlined.Place
import androidx.compose.material3.ExtendedFloatingActionButton
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
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.text.font.FontFamily
import androidx.compose.ui.text.input.KeyboardCapitalization
import androidx.compose.ui.text.input.KeyboardType
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
import de.akakraftbs.android.workshoponline.model.SchluesselInput
import de.akakraftbs.android.workshoponline.model.Schluesselhinterlegung
import de.akakraftbs.android.workshoponline.model.VereinInfo
import de.akakraftbs.android.workshoponline.model.VereinZugang
import de.akakraftbs.android.workshoponline.model.VereinZugangInput
import de.akakraftbs.android.workshoponline.model.nonEmpty
import de.akakraftbs.android.workshoponline.ui.components.ActionErrorDialog
import de.akakraftbs.android.workshoponline.ui.components.AppScaffold
import de.akakraftbs.android.workshoponline.ui.components.Avatar
import de.akakraftbs.android.workshoponline.ui.components.ConfirmDialog
import de.akakraftbs.android.workshoponline.ui.components.FeatureViewModel
import de.akakraftbs.android.workshoponline.ui.components.FormHint
import de.akakraftbs.android.workshoponline.ui.components.FormScaffold
import de.akakraftbs.android.workshoponline.ui.components.FormTextField
import de.akakraftbs.android.workshoponline.ui.components.LocalIsOnline
import de.akakraftbs.android.workshoponline.ui.components.LocalNavigator
import de.akakraftbs.android.workshoponline.ui.components.MenuAction
import de.akakraftbs.android.workshoponline.ui.components.OverflowMenu
import de.akakraftbs.android.workshoponline.ui.components.RemoteList
import de.akakraftbs.android.workshoponline.ui.components.dial
import de.akakraftbs.android.workshoponline.ui.components.emptyItem
import de.akakraftbs.android.workshoponline.ui.components.fromArg
import de.akakraftbs.android.workshoponline.ui.components.sectionHeader
import de.akakraftbs.android.workshoponline.ui.components.showOnMap
import de.akakraftbs.android.workshoponline.ui.components.toArg
import kotlinx.serialization.Serializable
import java.text.Collator
import javax.inject.Inject

@Serializable data object VereinInfoRoute
@Serializable data class SchluesselFormRoute(val schluessel: String? = null)
@Serializable data object ZugaengeRoute
@Serializable data class ZugangFormRoute(val zugang: String? = null)

fun NavGraphBuilder.vereinDestinations() {
    composable<VereinInfoRoute> { VereinInfoScreen() }
    composable<SchluesselFormRoute> { SchluesselFormScreen() }
    composable<ZugaengeRoute> { ZugaengeScreen() }
    composable<ZugangFormRoute> { ZugangFormScreen() }
    dokumenteDestinations()
    projekteDestinations()
}

// region Vereinsinfo

@HiltViewModel
class VereinInfoViewModel @Inject constructor(session: AppSession) : FeatureViewModel(session) {
    val info = remote<VereinInfo>("verein/info")
    override suspend fun reloadAll() = info.load(session)

    fun delete(schluessel: Schluesselhinterlegung) = act(onSuccess = { reloadAll() }) {
        send(HttpMethod.DELETE, "verein/info/schluessel/${schluessel.id}")
    }
}

@Composable
private fun VereinInfoScreen(viewModel: VereinInfoViewModel = hiltViewModel()) {
    val nav = LocalNavigator.current
    val isOnline = LocalIsOnline.current
    var deleting by rememberSaveable { mutableStateOf<String?>(null) }
    LaunchedEffect(Unit) { viewModel.reload() }

    AppScaffold(
        title = "Verein",
        onBack = nav::back,
        floatingActionButton = {
            if (viewModel.permissions.isPrivileged && isOnline) {
                ExtendedFloatingActionButton(
                    text = { Text("Hinterlegung") },
                    icon = { Icon(Icons.Outlined.Add, contentDescription = null) },
                    onClick = { nav.open(SchluesselFormRoute()) },
                )
            }
        },
    ) { padding ->
        RemoteList(viewModel.info, padding, viewModel.isRefreshing, viewModel::refresh) { info ->
            sectionHeader("Vorstand & Ämter")
            items(info.amtstraeger, key = { it.role + it.userId }) { person ->
                ListItem(
                    leadingContent = { Avatar(person.userName, null, size = 40.dp) },
                    headlineContent = { Text(person.userName) },
                    supportingContent = {
                        Column {
                            Text(person.roleLabel)
                            ContactLinks(person.phone, person.address)
                        }
                    },
                )
            }
            sectionHeader("Schlüsselhinterlegung")
            if (info.schluessel.isEmpty()) {
                item(key = "keine-schluessel") {
                    Column(Modifier.padding(horizontal = 16.dp, vertical = 8.dp)) { FormHint("Keine Hinterlegungen eingetragen.") }
                }
            }
            items(info.schluessel.sortedBy { it.sortOrder }, key = { it.id }) { schluessel ->
                ListItem(
                    leadingContent = { Icon(Icons.Outlined.Key, contentDescription = null) },
                    headlineContent = { Text(schluessel.name) },
                    supportingContent = { ContactLinks(schluessel.phone, schluessel.address) },
                    trailingContent = {
                        if (viewModel.permissions.isPrivileged && isOnline) {
                            OverflowMenu(
                                listOf(
                                    MenuAction("Bearbeiten", Icons.Outlined.Edit) { nav.open(SchluesselFormRoute(schluessel.toArg())) },
                                    MenuAction("Löschen", Icons.Outlined.DeleteOutline, destructive = true) { deleting = schluessel.id },
                                ),
                            )
                        }
                    },
                )
            }
        }
    }

    deleting?.let { id ->
        ConfirmDialog(
            "Hinterlegung löschen?",
            onConfirm = { viewModel.info.value?.schluessel?.firstOrNull { it.id == id }?.let(viewModel::delete) },
            onDismiss = { deleting = null },
        )
    }
    ActionErrorDialog(viewModel.action)
}

/** Telefon- und Adress-Links (Anrufen, Karte). */
@Composable
fun ContactLinks(phone: String?, address: String?) {
    val context = LocalContext.current
    Column {
        phone.nonEmpty?.let { number ->
            Row(
                Modifier.clickable { context.dial(number) }.padding(vertical = 4.dp),
                verticalAlignment = Alignment.CenterVertically,
                horizontalArrangement = Arrangement.spacedBy(6.dp),
            ) {
                Icon(Icons.Outlined.Phone, contentDescription = null, modifier = Modifier.size(16.dp), tint = MaterialTheme.colorScheme.primary)
                Text(number, color = MaterialTheme.colorScheme.primary)
            }
        }
        address.nonEmpty?.let { text ->
            Row(
                Modifier.clickable { context.showOnMap(text) }.padding(vertical = 4.dp),
                verticalAlignment = Alignment.CenterVertically,
                horizontalArrangement = Arrangement.spacedBy(6.dp),
            ) {
                Icon(Icons.Outlined.Place, contentDescription = null, modifier = Modifier.size(16.dp), tint = MaterialTheme.colorScheme.primary)
                Text(text, color = MaterialTheme.colorScheme.primary)
            }
        }
    }
}

@HiltViewModel
class SchluesselFormViewModel @Inject constructor(session: AppSession, saved: SavedStateHandle) : FeatureViewModel(session) {
    val schluessel: Schluesselhinterlegung? = saved.toRoute<SchluesselFormRoute>().schluessel?.fromArg()
    var name by mutableStateOf(schluessel?.name.orEmpty())
    var address by mutableStateOf(schluessel?.address.orEmpty())
    var phone by mutableStateOf(schluessel?.phone.orEmpty())

    val canSave: Boolean get() = name.isNotBlank() && address.isNotBlank()

    fun save(onDone: () -> Unit) {
        val input = SchluesselInput(name.trim(), address.trim(), phone.nonEmpty)
        act(onSuccess = { onDone() }) {
            if (schluessel != null) send(HttpMethod.PUT, "verein/info/schluessel/${schluessel.id}", input)
            else send(HttpMethod.POST, "verein/info/schluessel", input)
        }
    }
}

@Composable
private fun SchluesselFormScreen(viewModel: SchluesselFormViewModel = hiltViewModel()) {
    val nav = LocalNavigator.current
    FormScaffold(
        title = if (viewModel.schluessel == null) "Neue Hinterlegung" else "Hinterlegung bearbeiten",
        onClose = nav::back,
        canSave = viewModel.canSave,
        onSave = { viewModel.save(nav::back) },
        action = viewModel.action,
    ) {
        FormTextField(viewModel.name, { viewModel.name = it }, "Name")
        FormTextField(viewModel.address, { viewModel.address = it }, "Adresse", minLines = 2)
        FormTextField(viewModel.phone, { viewModel.phone = it }, "Telefon", keyboardType = KeyboardType.Phone)
    }
}

// endregion

// region Zugänge

@HiltViewModel
class ZugaengeViewModel @Inject constructor(session: AppSession) : FeatureViewModel(session) {
    val zugaenge = remote<List<VereinZugang>>("verein/zugaenge")
    var copiedId by mutableStateOf<String?>(null)

    override suspend fun reloadAll() = zugaenge.load(session)

    fun delete(zugang: VereinZugang) = act(onSuccess = { reloadAll() }) { send(HttpMethod.DELETE, "verein/zugaenge/${zugang.id}") }
}

@Composable
private fun ZugaengeScreen(viewModel: ZugaengeViewModel = hiltViewModel()) {
    val nav = LocalNavigator.current
    val context = LocalContext.current
    val isOnline = LocalIsOnline.current
    var deleting by rememberSaveable { mutableStateOf<String?>(null) }
    LaunchedEffect(Unit) { viewModel.reload() }

    AppScaffold(
        title = "Zugänge",
        onBack = nav::back,
        floatingActionButton = {
            if (viewModel.permissions.isPrivileged && isOnline) {
                ExtendedFloatingActionButton(
                    text = { Text("Zugang") },
                    icon = { Icon(Icons.Outlined.Add, contentDescription = null) },
                    onClick = { nav.open(ZugangFormRoute()) },
                )
            }
        },
    ) { padding ->
        RemoteList(viewModel.zugaenge, padding, viewModel.isRefreshing, viewModel::refresh) { items ->
            if (items.isEmpty()) emptyItem(Icons.Outlined.Key, "Keine Zugänge", "")
            items(items.sortedWith(compareBy(Collator.getInstance()) { it.anbieter }), key = { it.id }) { zugang ->
                ListItem(
                    headlineContent = { Text(zugang.anbieter) },
                    supportingContent = { SelectionContainer { Text(zugang.zugangsdaten, fontFamily = FontFamily.Monospace) } },
                    trailingContent = {
                        Row(verticalAlignment = Alignment.CenterVertically) {
                            val copied = viewModel.copiedId == zugang.id
                            OutlinedButton(onClick = {
                                context.copyToClipboard(zugang.anbieter, zugang.zugangsdaten)
                                viewModel.copiedId = zugang.id
                            }) {
                                Icon(if (copied) Icons.Outlined.Check else Icons.Outlined.ContentCopy, contentDescription = null, modifier = Modifier.size(16.dp))
                                Text(if (copied) " Kopiert" else " Kopieren")
                            }
                            if (viewModel.permissions.isPrivileged && isOnline) {
                                OverflowMenu(
                                    listOf(
                                        MenuAction("Bearbeiten", Icons.Outlined.Edit) { nav.open(ZugangFormRoute(zugang.toArg())) },
                                        MenuAction("Löschen", Icons.Outlined.DeleteOutline, destructive = true) { deleting = zugang.id },
                                    ),
                                )
                            }
                        }
                    },
                )
            }
            item(key = "hinweis") {
                Column(Modifier.padding(16.dp)) { FormHint("Zugangsdaten sind vertraulich und nur für Vereinsmitglieder bestimmt.") }
            }
        }
    }

    deleting?.let { id ->
        ConfirmDialog(
            "Zugang löschen?",
            onConfirm = { viewModel.zugaenge.value?.firstOrNull { it.id == id }?.let(viewModel::delete) },
            onDismiss = { deleting = null },
        )
    }
    ActionErrorDialog(viewModel.action)
}

/** Kopiert vertrauliche Daten (als „sensitiv“ markiert, damit Android sie nicht in der Vorschau zeigt). */
fun Context.copyToClipboard(label: String, text: String) {
    val clip = ClipData.newPlainText(label, text).apply {
        description.extras = android.os.PersistableBundle().apply { putBoolean("android.content.extra.IS_SENSITIVE", true) }
    }
    getSystemService(ClipboardManager::class.java).setPrimaryClip(clip)
}

@HiltViewModel
class ZugangFormViewModel @Inject constructor(session: AppSession, saved: SavedStateHandle) : FeatureViewModel(session) {
    val zugang: VereinZugang? = saved.toRoute<ZugangFormRoute>().zugang?.fromArg()
    var anbieter by mutableStateOf(zugang?.anbieter.orEmpty())
    var zugangsdaten by mutableStateOf(zugang?.zugangsdaten.orEmpty())

    val canSave: Boolean get() = anbieter.isNotBlank() && zugangsdaten.isNotBlank()

    fun save(onDone: () -> Unit) {
        val input = VereinZugangInput(anbieter, zugangsdaten)
        act(onSuccess = { onDone() }) {
            if (zugang != null) send(HttpMethod.PUT, "verein/zugaenge/${zugang.id}", input)
            else send(HttpMethod.POST, "verein/zugaenge", input)
        }
    }
}

@Composable
private fun ZugangFormScreen(viewModel: ZugangFormViewModel = hiltViewModel()) {
    val nav = LocalNavigator.current
    FormScaffold(
        title = if (viewModel.zugang == null) "Neuer Zugang" else "Zugang bearbeiten",
        onClose = nav::back,
        canSave = viewModel.canSave,
        onSave = { viewModel.save(nav::back) },
        action = viewModel.action,
    ) {
        FormTextField(viewModel.anbieter, { viewModel.anbieter = it }, "Anbieter")
        FormTextField(viewModel.zugangsdaten, { viewModel.zugangsdaten = it }, "Zugangsdaten", minLines = 3, capitalization = KeyboardCapitalization.None)
    }
}

// endregion
