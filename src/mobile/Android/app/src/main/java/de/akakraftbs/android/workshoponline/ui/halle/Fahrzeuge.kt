package de.akakraftbs.android.workshoponline.ui.halle

import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.Row
import androidx.compose.ui.Alignment
import androidx.compose.foundation.lazy.items
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.outlined.Add
import androidx.compose.material.icons.outlined.DeleteOutline
import androidx.compose.material.icons.outlined.DirectionsCar
import androidx.compose.material.icons.outlined.Star
import androidx.compose.material3.ExtendedFloatingActionButton
import androidx.compose.material3.Icon
import androidx.compose.material3.ListItem
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.saveable.rememberSaveable
import androidx.compose.runtime.setValue
import androidx.compose.ui.Modifier
import androidx.compose.ui.text.font.FontFamily
import androidx.compose.ui.text.input.KeyboardCapitalization
import androidx.hilt.lifecycle.viewmodel.compose.hiltViewModel
import androidx.lifecycle.SavedStateHandle
import androidx.navigation.NavGraphBuilder
import androidx.navigation.compose.composable
import androidx.navigation.toRoute
import dagger.hilt.android.lifecycle.HiltViewModel
import de.akakraftbs.android.workshoponline.core.auth.AppSession
import de.akakraftbs.android.workshoponline.core.cache.remote
import de.akakraftbs.android.workshoponline.core.network.HttpMethod
import de.akakraftbs.android.workshoponline.model.Fahrzeug
import de.akakraftbs.android.workshoponline.model.FahrzeugInput
import de.akakraftbs.android.workshoponline.model.Tone
import de.akakraftbs.android.workshoponline.model.nonEmpty
import de.akakraftbs.android.workshoponline.ui.components.ActionErrorDialog
import de.akakraftbs.android.workshoponline.ui.components.AppScaffold
import de.akakraftbs.android.workshoponline.ui.components.ConfirmDialog
import de.akakraftbs.android.workshoponline.ui.components.FeatureViewModel
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
import de.akakraftbs.android.workshoponline.ui.components.toArg
import kotlinx.serialization.Serializable
import javax.inject.Inject

@Serializable data object FahrzeugeRoute
@Serializable data class FahrzeugFormRoute(val fahrzeug: String? = null)

fun NavGraphBuilder.fahrzeugeDestinations() {
    composable<FahrzeugeRoute> { FahrzeugeScreen() }
    composable<FahrzeugFormRoute> { FahrzeugFormScreen() }
}

@HiltViewModel
class FahrzeugeViewModel @Inject constructor(session: AppSession) : FeatureViewModel(session) {
    val fahrzeuge = remote<List<Fahrzeug>>("fahrzeuge")

    override suspend fun reloadAll() = fahrzeuge.load(session)

    fun delete(fahrzeug: Fahrzeug) = act(onSuccess = { reloadAll() }) {
        send(HttpMethod.DELETE, "fahrzeuge/${fahrzeug.id}")
    }
}

@Composable
private fun FahrzeugeScreen(viewModel: FahrzeugeViewModel = hiltViewModel()) {
    val nav = LocalNavigator.current
    val isOnline = LocalIsOnline.current
    var deleting by rememberSaveable { mutableStateOf<String?>(null) }
    LaunchedEffect(Unit) { viewModel.reload() }

    AppScaffold(
        title = "Meine Fahrzeuge",
        onBack = nav::back,
        floatingActionButton = {
            if (isOnline) {
                ExtendedFloatingActionButton(
                    text = { Text("Fahrzeug") },
                    icon = { Icon(Icons.Outlined.Add, contentDescription = null) },
                    onClick = { nav.open(FahrzeugFormRoute()) },
                )
            }
        },
    ) { padding ->
        RemoteList(viewModel.fahrzeuge, padding, viewModel.isRefreshing, viewModel::refresh) { items ->
            if (items.isEmpty()) {
                emptyItem(
                    Icons.Outlined.DirectionsCar,
                    "Keine Fahrzeuge",
                    "Fahrzeuge kannst du im Hallenbuch, bei der Hallenbelegung und beim Campus-Parken auswählen.",
                )
            }
            items(items, key = { it.id }) { fahrzeug ->
                ListItem(
                    modifier = Modifier.clickable(enabled = isOnline) { nav.open(FahrzeugFormRoute(fahrzeug.toArg())) },
                    leadingContent = { Icon(Icons.Outlined.DirectionsCar, contentDescription = null) },
                    headlineContent = { Text(listOfNotNull(fahrzeug.marke, fahrzeug.modell.nonEmpty).joinToString(" ")) },
                    supportingContent = { Text(fahrzeug.kennzeichen, fontFamily = FontFamily.Monospace) },
                    trailingContent = {
                        Row(verticalAlignment = Alignment.CenterVertically) {
                            if (fahrzeug.istStandard) StatusPill("Standard", Tone.Blue, Icons.Outlined.Star)
                            if (isOnline) {
                                OverflowMenu(listOf(MenuAction("Löschen", Icons.Outlined.DeleteOutline, destructive = true) { deleting = fahrzeug.id }))
                            }
                        }
                    },
                )
            }
        }
    }

    deleting?.let { id ->
        ConfirmDialog(
            "Fahrzeug löschen?",
            onConfirm = { viewModel.fahrzeuge.value?.firstOrNull { it.id == id }?.let(viewModel::delete) },
            onDismiss = { deleting = null },
        )
    }
    ActionErrorDialog(viewModel.action)
}

@HiltViewModel
class FahrzeugFormViewModel @Inject constructor(session: AppSession, saved: SavedStateHandle) : FeatureViewModel(session) {
    val fahrzeug: Fahrzeug? = saved.toRoute<FahrzeugFormRoute>().fahrzeug?.fromArg()
    var marke by mutableStateOf(fahrzeug?.marke.orEmpty())
    var modell by mutableStateOf(fahrzeug?.modell.orEmpty())
    var kennzeichen by mutableStateOf(fahrzeug?.kennzeichen.orEmpty())
    var istStandard by mutableStateOf(fahrzeug?.istStandard ?: false)

    val canSave: Boolean get() = marke.isNotBlank() && kennzeichen.isNotBlank()

    fun save(onDone: () -> Unit) {
        val input = FahrzeugInput(marke.trim(), modell.nonEmpty, kennzeichen.trim().uppercase(), istStandard)
        act(onSuccess = { onDone() }) {
            if (fahrzeug != null) send(HttpMethod.PUT, "fahrzeuge/${fahrzeug.id}", input)
            else send(HttpMethod.POST, "fahrzeuge", input)
        }
    }
}

@Composable
private fun FahrzeugFormScreen(viewModel: FahrzeugFormViewModel = hiltViewModel()) {
    val nav = LocalNavigator.current
    FormScaffold(
        title = if (viewModel.fahrzeug == null) "Neues Fahrzeug" else "Fahrzeug bearbeiten",
        onClose = nav::back,
        canSave = viewModel.canSave,
        onSave = { viewModel.save(nav::back) },
        action = viewModel.action,
    ) {
        FormTextField(viewModel.marke, { viewModel.marke = it }, "Marke")
        FormTextField(viewModel.modell, { viewModel.modell = it }, "Modell (optional)")
        FormTextField(viewModel.kennzeichen, { viewModel.kennzeichen = it }, "Kennzeichen", capitalization = KeyboardCapitalization.Characters)
        SwitchRow("Standardfahrzeug", viewModel.istStandard, { viewModel.istStandard = it })
    }
}
