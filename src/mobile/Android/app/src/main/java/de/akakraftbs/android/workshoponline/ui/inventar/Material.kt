package de.akakraftbs.android.workshoponline.ui.inventar

import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.heightIn
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.verticalScroll
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.outlined.Add
import androidx.compose.material.icons.outlined.AddShoppingCart
import androidx.compose.material.icons.outlined.DeleteOutline
import androidx.compose.material.icons.outlined.Edit
import androidx.compose.material.icons.outlined.Inventory2
import androidx.compose.material.icons.outlined.LocalShipping
import androidx.compose.material.icons.outlined.Place
import androidx.compose.material.icons.outlined.Remove
import androidx.compose.material.icons.outlined.Speed
import androidx.compose.material.icons.outlined.WarningAmber
import androidx.compose.material3.FilledTonalIconButton
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButton
import androidx.compose.material3.ListItem
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.OutlinedButton
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableIntStateOf
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.saveable.rememberSaveable
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.layout.ContentScale
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
import de.akakraftbs.android.workshoponline.core.network.HttpMethod
import de.akakraftbs.android.workshoponline.model.StorageLocation
import de.akakraftbs.android.workshoponline.model.Tone
import de.akakraftbs.android.workshoponline.model.Verbrauchsmaterial
import de.akakraftbs.android.workshoponline.model.VerbrauchsmaterialInput
import de.akakraftbs.android.workshoponline.model.nonEmpty
import de.akakraftbs.android.workshoponline.ui.components.ActionErrorDialog
import de.akakraftbs.android.workshoponline.ui.components.AppScaffold
import de.akakraftbs.android.workshoponline.ui.components.ConfirmDialog
import de.akakraftbs.android.workshoponline.ui.components.DetailRow
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
import de.akakraftbs.android.workshoponline.ui.components.SectionHeader
import de.akakraftbs.android.workshoponline.ui.components.SuggestionTextField
import de.akakraftbs.android.workshoponline.ui.components.SwitchRow
import de.akakraftbs.android.workshoponline.ui.components.dayString
import de.akakraftbs.android.workshoponline.ui.components.fromArg
import de.akakraftbs.android.workshoponline.ui.components.toArg
import de.akakraftbs.android.workshoponline.ui.components.toneColor
import kotlinx.coroutines.launch
import kotlinx.serialization.Serializable
import java.time.Instant
import javax.inject.Inject

@Serializable data class MaterialDetailRoute(val item: String)
@Serializable data class MaterialFormRoute(val item: String? = null, val categories: String = "[]")

fun NavGraphBuilder.materialDestinations() {
    composable<MaterialDetailRoute> { MaterialDetailScreen() }
    composable<MaterialFormRoute> { MaterialFormScreen() }
}

// region Detail

@HiltViewModel
class MaterialDetailViewModel @Inject constructor(session: AppSession, saved: SavedStateHandle) : FeatureViewModel(session) {
    var item by mutableStateOf<Verbrauchsmaterial>(saved.toRoute<MaterialDetailRoute>().item.fromArg())
        private set
    private val list = remote<List<Verbrauchsmaterial>>("verbrauchsmaterial")
    val locations = remote<List<StorageLocation>>("storage-locations")

    val canManage: Boolean get() = permissions.isPrivileged

    override suspend fun reloadAll() {
        loadAll(list, locations)
        list.value?.firstOrNull { it.id == item.id }?.let { item = it }
    }

    @Serializable private data class DeltaBody(val delta: Int)

    fun adjust(delta: Int) = act {
        item = decode(Verbrauchsmaterial.serializer(), send(HttpMethod.PATCH, "verbrauchsmaterial/${item.id}/quantity", DeltaBody(delta)))
    }

    fun markOrdered() = act {
        send(HttpMethod.POST, "verbrauchsmaterial/${item.id}/nachbestellen")
        item = item.copy(isNachbestellt = true, nachbestelltVonName = session.user.value?.shownName, nachbestelltAt = Instant.now())
    }

    fun delete(onDone: () -> Unit) = act(onSuccess = { onDone() }) {
        send(HttpMethod.DELETE, "verbrauchsmaterial/${item.id}")
    }
}

@Composable
private fun MaterialDetailScreen(viewModel: MaterialDetailViewModel = hiltViewModel()) {
    val nav = LocalNavigator.current
    val isOnline = LocalIsOnline.current
    val item = viewModel.item
    var confirmDelete by rememberSaveable { mutableStateOf(false) }
    LaunchedEffect(Unit) { viewModel.reload() }

    AppScaffold(
        title = item.name,
        onBack = nav::back,
        actions = {
            if (viewModel.canManage) {
                OverflowMenu(
                    enabled = isOnline,
                    actions = listOf(
                        MenuAction("Bearbeiten", Icons.Outlined.Edit) { nav.open(MaterialFormRoute(item.toArg(), listOf(item.category).toArg())) },
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
                placeholderIcon = Icons.Outlined.Inventory2,
                modifier = Modifier.fillMaxWidth().heightIn(min = 200.dp, max = 320.dp),
            )
            Column(Modifier.padding(16.dp), verticalArrangement = Arrangement.spacedBy(6.dp)) {
                Text(item.name, style = MaterialTheme.typography.headlineSmall, fontWeight = FontWeight.SemiBold)
                Text(item.category, color = MaterialTheme.colorScheme.onSurfaceVariant)
                item.description.nonEmpty?.let { Text(it, modifier = Modifier.padding(top = 4.dp)) }
            }
            item.storageLocation.nonEmpty?.let { location ->
                ListItem(
                    leadingContent = { Icon(Icons.Outlined.Place, contentDescription = null) },
                    overlineContent = { Text("Ablageort") },
                    headlineContent = { StorageLocationLabel(location, locationColor(viewModel.locations.value, location)) },
                )
            }

            SectionHeader("Bestand")
            Row(Modifier.fillMaxWidth().padding(horizontal = 16.dp, vertical = 8.dp), verticalAlignment = Alignment.CenterVertically) {
                if (viewModel.canManage) {
                    FilledTonalIconButton(onClick = { viewModel.adjust(-1) }, enabled = isOnline && item.quantity > 0 && !viewModel.action.isRunning) {
                        Icon(Icons.Outlined.Remove, contentDescription = "Weniger")
                    }
                }
                Column(Modifier.weight(1f), horizontalAlignment = Alignment.CenterHorizontally) {
                    Text(
                        "${item.quantity}",
                        style = MaterialTheme.typography.displaySmall,
                        fontWeight = FontWeight.Bold,
                        color = if (item.isLowStock) toneColor(Tone.Red) else MaterialTheme.colorScheme.onSurface,
                    )
                    Text(item.unit, color = MaterialTheme.colorScheme.onSurfaceVariant)
                }
                if (viewModel.canManage) {
                    FilledTonalIconButton(onClick = { viewModel.adjust(1) }, enabled = isOnline && !viewModel.action.isRunning) {
                        Icon(Icons.Outlined.Add, contentDescription = "Mehr")
                    }
                }
            }
            item.minQuantity?.let { DetailRow("Mindestbestand", "$it ${item.unit}", Icons.Outlined.Speed) }
            if (item.isLowStock) {
                ListItem(
                    leadingContent = { Icon(Icons.Outlined.WarningAmber, contentDescription = null, tint = toneColor(Tone.Red)) },
                    headlineContent = { Text("Bestand niedrig", color = toneColor(Tone.Red)) },
                )
            }

            SectionHeader("Nachbestellung")
            when {
                item.isNachbestellt -> ListItem(
                    leadingContent = { Icon(Icons.Outlined.LocalShipping, contentDescription = null, tint = toneColor(Tone.Blue)) },
                    headlineContent = {
                        Text(
                            "Nachbestellt" + (item.nachbestelltVonName?.let { " von $it" } ?: "") +
                                (item.nachbestelltAt?.let { " am ${it.dayString()}" } ?: ""),
                        )
                    },
                )
                viewModel.canManage -> OutlinedButton(
                    onClick = viewModel::markOrdered,
                    enabled = isOnline,
                    modifier = Modifier.padding(horizontal = 16.dp, vertical = 8.dp),
                ) {
                    Icon(Icons.Outlined.AddShoppingCart, contentDescription = null)
                    Text("  Als nachbestellt markieren")
                }
                else -> Text("Noch nicht nachbestellt.", modifier = Modifier.padding(16.dp), color = MaterialTheme.colorScheme.onSurfaceVariant)
            }
        }
    }

    if (confirmDelete) {
        ConfirmDialog("„${item.name}“ löschen?", onConfirm = { viewModel.delete(nav::back) }, onDismiss = { confirmDelete = false })
    }
    ActionErrorDialog(viewModel.action)
}

// endregion

// region Formular

@HiltViewModel
class MaterialFormViewModel @Inject constructor(session: AppSession, saved: SavedStateHandle) : FeatureViewModel(session) {
    private val route = saved.toRoute<MaterialFormRoute>()
    val item: Verbrauchsmaterial? = route.item?.fromArg()
    val categories: List<String> = route.categories.fromArg()
    val units = remote<List<String>>("verbrauchsmaterial/units")
    val locations = remote<List<StorageLocation>>("storage-locations")

    var name by mutableStateOf(item?.name.orEmpty())
    var category by mutableStateOf(item?.category.orEmpty())
    var description by mutableStateOf(item?.description.orEmpty())
    var unit by mutableStateOf(item?.unit ?: "Stück")
    var quantity by mutableIntStateOf(item?.quantity ?: 0)
    var hasMinimum by mutableStateOf(item?.minQuantity != null)
    var minQuantity by mutableIntStateOf(item?.minQuantity ?: 0)
    var storageLocation by mutableStateOf(item?.storageLocation.orEmpty())
    val photo = PhotoUpload(session, "uploads/verbrauchsmaterial", item?.imageUrl, item?.thumbnailUrl)

    val canSave: Boolean get() = name.isNotBlank() && category.isNotBlank() && unit.isNotBlank() && !photo.isUploading

    init {
        viewModelScope.launch { loadAll(units, locations) }
    }

    fun save(onDone: () -> Unit) {
        val input = VerbrauchsmaterialInput(
            name = name.trim(),
            description = description.trim(),
            category = category.trim(),
            unit = unit.trim(),
            quantity = quantity,
            minQuantity = if (hasMinimum) minQuantity else null,
            imageUrl = photo.imageUrl,
            thumbnailUrl = photo.thumbnailUrl,
            storageLocation = storageLocation.nonEmpty,
        )
        act(onSuccess = { onDone() }) {
            if (item != null) send(HttpMethod.PUT, "verbrauchsmaterial/${item.id}", input)
            else send(HttpMethod.POST, "verbrauchsmaterial", input)
        }
    }
}

@Composable
private fun MaterialFormScreen(viewModel: MaterialFormViewModel = hiltViewModel()) {
    val nav = LocalNavigator.current
    FormScaffold(
        title = if (viewModel.item == null) "Neues Material" else "Material bearbeiten",
        onClose = nav::back,
        canSave = viewModel.canSave,
        onSave = { viewModel.save(nav::back) },
        action = viewModel.action,
    ) {
        FormTextField(viewModel.name, { viewModel.name = it }, "Name")
        SuggestionTextField(viewModel.category, { viewModel.category = it }, "Kategorie", viewModel.categories)
        FormTextField(viewModel.description, { viewModel.description = it }, "Beschreibung", minLines = 3)
        Stepper("Menge", viewModel.quantity) { viewModel.quantity = it }
        SuggestionTextField(viewModel.unit, { viewModel.unit = it }, "Einheit", viewModel.units.value.orEmpty())
        SwitchRow("Mindestbestand", viewModel.hasMinimum, { viewModel.hasMinimum = it })
        if (viewModel.hasMinimum) Stepper("Mindestens", viewModel.minQuantity) { viewModel.minQuantity = it }
        SuggestionTextField(viewModel.storageLocation, { viewModel.storageLocation = it }, "Ablageort", viewModel.locations.value.orEmpty().map { it.name })
        PhotoUploadField(viewModel.photo)
    }
}

/** Zahlenfeld mit −/+ (Gegenstück zum iOS-Stepper). */
@Composable
fun Stepper(label: String, value: Int, range: IntRange = 0..100_000, onValueChange: (Int) -> Unit) {
    Row(Modifier.fillMaxWidth(), verticalAlignment = Alignment.CenterVertically) {
        Text("$label: $value", style = MaterialTheme.typography.bodyLarge, modifier = Modifier.weight(1f))
        IconButton(onClick = { onValueChange((value - 1).coerceIn(range)) }, enabled = value > range.first) {
            Icon(Icons.Outlined.Remove, contentDescription = "Weniger")
        }
        IconButton(onClick = { onValueChange((value + 1).coerceIn(range)) }, enabled = value < range.last) {
            Icon(Icons.Outlined.Add, contentDescription = "Mehr")
        }
    }
}

// endregion
