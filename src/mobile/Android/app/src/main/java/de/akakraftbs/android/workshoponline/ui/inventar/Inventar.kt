package de.akakraftbs.android.workshoponline.ui.inventar

import androidx.compose.foundation.background
import androidx.compose.foundation.clickable
import androidx.compose.foundation.horizontalScroll
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.PaddingValues
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.lazy.items
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.outlined.Add
import androidx.compose.material.icons.outlined.Build
import androidx.compose.material.icons.outlined.Close
import androidx.compose.material.icons.outlined.Inventory2
import androidx.compose.material.icons.outlined.Search
import androidx.compose.material.icons.automirrored.outlined.Sort
import androidx.compose.material3.DropdownMenu
import androidx.compose.material3.DropdownMenuItem
import androidx.compose.material3.ExtendedFloatingActionButton
import androidx.compose.material3.FilterChip
import androidx.compose.material3.FilterChipDefaults
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButton
import androidx.compose.material3.ListItem
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.OutlinedTextField
import androidx.compose.material3.SegmentedButton
import androidx.compose.material3.SegmentedButtonDefaults
import androidx.compose.material3.SingleChoiceSegmentedButtonRow
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.saveable.rememberSaveable
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextOverflow
import androidx.compose.ui.unit.dp
import androidx.hilt.lifecycle.viewmodel.compose.hiltViewModel
import androidx.lifecycle.SavedStateHandle
import androidx.navigation.NavGraphBuilder
import androidx.navigation.compose.composable
import dagger.hilt.android.lifecycle.HiltViewModel
import de.akakraftbs.android.workshoponline.core.auth.AppSession
import de.akakraftbs.android.workshoponline.core.cache.remote
import de.akakraftbs.android.workshoponline.model.StorageLocation
import de.akakraftbs.android.workshoponline.model.Tone
import de.akakraftbs.android.workshoponline.model.Verbrauchsmaterial
import de.akakraftbs.android.workshoponline.model.Werkzeug
import de.akakraftbs.android.workshoponline.model.nonEmpty
import de.akakraftbs.android.workshoponline.ui.components.AppScaffold
import de.akakraftbs.android.workshoponline.ui.components.FeatureViewModel
import de.akakraftbs.android.workshoponline.ui.components.LocalIsOnline
import de.akakraftbs.android.workshoponline.ui.components.LocalNavigator
import de.akakraftbs.android.workshoponline.ui.components.RemoteImage
import de.akakraftbs.android.workshoponline.ui.components.RemoteList
import de.akakraftbs.android.workshoponline.ui.components.StatusPill
import de.akakraftbs.android.workshoponline.ui.components.emptyItem
import de.akakraftbs.android.workshoponline.ui.components.hexColor
import de.akakraftbs.android.workshoponline.ui.components.toArg
import de.akakraftbs.android.workshoponline.ui.components.toneColor
import kotlinx.serialization.Serializable
import java.text.Collator
import javax.inject.Inject

/** Inventar als Unterseite (Smartphone: aus „Mehr“); auf Tablets ist es ein eigener Tab. */
@Serializable data object InventarPageRoute

fun NavGraphBuilder.inventarDestinations() {
    composable<InventarPageRoute> {
        val nav = LocalNavigator.current
        InventarScreen(onBack = nav::back)
    }
    werkzeugDestinations()
    materialDestinations()
}

enum class InventarSegment(val label: String) { Werkzeug("Werkzeug"), Material("Material") }

enum class WerkzeugSort(val label: String) {
    Name("Name"), CreatedAt("Neueste zuerst"), Category("Kategorie"), StorageLocation("Ablageort")
}

private val collator: Collator = Collator.getInstance()

@HiltViewModel
class InventarViewModel @Inject constructor(session: AppSession, private val saved: SavedStateHandle) : FeatureViewModel(session) {
    val werkzeug = remote<List<Werkzeug>>("werkzeug")
    val material = remote<List<Verbrauchsmaterial>>("verbrauchsmaterial")
    val locations = remote<List<StorageLocation>>("storage-locations")

    var segment by mutableStateOf(saved.get<String>("segment")?.let(InventarSegment::valueOf) ?: InventarSegment.Werkzeug)
        private set
    var search by mutableStateOf("")
    var category by mutableStateOf<String?>(null)
    var onlyBorrowed by mutableStateOf(false)
    var onlyLowStock by mutableStateOf(false)
    var sort by mutableStateOf(WerkzeugSort.Name)

    override suspend fun reloadAll() {
        loadAll(werkzeug, material, locations)
    }

    fun select(value: InventarSegment) {
        segment = value
        saved["segment"] = value.name
        category = null
    }

    val categories: List<String>
        get() = when (segment) {
            InventarSegment.Werkzeug -> werkzeug.value.orEmpty().map { it.category }
            InventarSegment.Material -> material.value.orEmpty().map { it.category }
        }.filter { it.isNotBlank() }.distinct().sortedWith(collator)

    private fun matches(vararg fields: String): Boolean {
        val query = search.trim().lowercase()
        return query.isEmpty() || fields.any { it.lowercase().contains(query) }
    }

    fun filteredWerkzeug(items: List<Werkzeug>): List<Werkzeug> = items
        .filter { !onlyBorrowed || !it.isAvailable }
        .filter { category == null || it.category == category }
        .filter { matches(it.name, it.description, it.category) }
        .sortedWith(
            when (sort) {
                WerkzeugSort.Name -> compareBy(collator) { it.name }
                WerkzeugSort.CreatedAt -> compareByDescending { it.createdAt }
                WerkzeugSort.Category -> compareBy<Werkzeug, String>(collator) { it.category }.thenBy(collator) { it.name }
                WerkzeugSort.StorageLocation -> compareBy<Werkzeug, String>(collator) { it.storageLocation.orEmpty() }.thenBy(collator) { it.name }
            },
        )

    fun filteredMaterial(items: List<Verbrauchsmaterial>): List<Verbrauchsmaterial> = items
        .filter { !onlyLowStock || it.isLowStock }
        .filter { category == null || it.category == category }
        .filter { matches(it.name, it.description, it.category) }
        .sortedWith(compareBy(collator) { it.name })

    fun locationColor(name: String?): Color? = locationColor(locations.value, name)
}

fun locationColor(locations: List<StorageLocation>?, name: String?): Color? {
    val key = name?.lowercase() ?: return null
    return hexColor(locations?.firstOrNull { it.name.lowercase() == key }?.color)
}

@Composable
fun InventarScreen(onBack: (() -> Unit)? = null, viewModel: InventarViewModel = hiltViewModel()) {
    val nav = LocalNavigator.current
    val isOnline = LocalIsOnline.current
    LaunchedEffect(Unit) { viewModel.reload() }
    val segment = viewModel.segment

    AppScaffold(
        title = "Inventar",
        onBack = onBack,
        floatingActionButton = {
            if (viewModel.permissions.isPrivileged && isOnline) {
                ExtendedFloatingActionButton(
                    text = { Text(if (segment == InventarSegment.Werkzeug) "Werkzeug" else "Material") },
                    icon = { Icon(Icons.Outlined.Add, contentDescription = null) },
                    onClick = {
                        val categories = viewModel.categories.toArg()
                        if (segment == InventarSegment.Werkzeug) nav.open(WerkzeugFormRoute(categories = categories))
                        else nav.open(MaterialFormRoute(categories = categories))
                    },
                )
            }
        },
    ) { padding ->
        Column(Modifier.fillMaxSize().padding(top = padding.calculateTopPadding())) {
            SingleChoiceSegmentedButtonRow(Modifier.fillMaxWidth().padding(horizontal = 16.dp)) {
                InventarSegment.entries.forEachIndexed { index, value ->
                    SegmentedButton(
                        selected = segment == value,
                        onClick = { viewModel.select(value) },
                        shape = SegmentedButtonDefaults.itemShape(index, InventarSegment.entries.size),
                    ) { Text(value.label) }
                }
            }
            SearchField(
                value = viewModel.search,
                onValueChange = { viewModel.search = it },
                placeholder = if (segment == InventarSegment.Werkzeug) "Werkzeug suchen" else "Material suchen",
            )
            FilterRow(viewModel)
            when (segment) {
                InventarSegment.Werkzeug -> RemoteList(viewModel.werkzeug, PaddingValues(0.dp), viewModel.isRefreshing, viewModel::refresh) { items ->
                    val visible = viewModel.filteredWerkzeug(items)
                    if (visible.isEmpty()) emptyItem(Icons.Outlined.Build, "Keine Werkzeuge", "Für diese Suche bzw. diesen Filter gibt es keine Einträge.")
                    items(visible, key = { it.id }) { item ->
                        WerkzeugRow(item, viewModel.locationColor(item.storageLocation)) { nav.open(WerkzeugDetailRoute(item.toArg())) }
                    }
                }
                InventarSegment.Material -> RemoteList(viewModel.material, PaddingValues(0.dp), viewModel.isRefreshing, viewModel::refresh) { items ->
                    val visible = viewModel.filteredMaterial(items)
                    if (visible.isEmpty()) emptyItem(Icons.Outlined.Inventory2, "Kein Material", "Für diese Suche bzw. diesen Filter gibt es keine Einträge.")
                    items(visible, key = { it.id }) { item ->
                        MaterialRow(item, viewModel.locationColor(item.storageLocation)) { nav.open(MaterialDetailRoute(item.toArg())) }
                    }
                }
            }
        }
    }
}

@Composable
fun SearchField(value: String, onValueChange: (String) -> Unit, placeholder: String, modifier: Modifier = Modifier) {
    OutlinedTextField(
        value = value,
        onValueChange = onValueChange,
        placeholder = { Text(placeholder) },
        leadingIcon = { Icon(Icons.Outlined.Search, contentDescription = null) },
        trailingIcon = {
            if (value.isNotEmpty()) {
                IconButton(onClick = { onValueChange("") }) { Icon(Icons.Outlined.Close, contentDescription = "Suche leeren") }
            }
        },
        singleLine = true,
        shape = CircleShape,
        modifier = modifier.fillMaxWidth().padding(horizontal = 16.dp, vertical = 8.dp),
    )
}

@Composable
private fun FilterRow(viewModel: InventarViewModel) {
    var categoryMenu by remember { mutableStateOf(false) }
    var sortMenu by remember { mutableStateOf(false) }
    Row(
        Modifier.fillMaxWidth().horizontalScroll(rememberScrollState()).padding(horizontal = 16.dp),
        horizontalArrangement = Arrangement.spacedBy(8.dp),
    ) {
        if (viewModel.segment == InventarSegment.Werkzeug) {
            FilterChip(selected = viewModel.onlyBorrowed, onClick = { viewModel.onlyBorrowed = !viewModel.onlyBorrowed }, label = { Text("Nur ausgeliehene") })
        } else {
            FilterChip(selected = viewModel.onlyLowStock, onClick = { viewModel.onlyLowStock = !viewModel.onlyLowStock }, label = { Text("Bestand niedrig") })
        }
        Box {
            FilterChip(
                selected = viewModel.category != null,
                onClick = { categoryMenu = true },
                label = { Text(viewModel.category ?: "Kategorie") },
                trailingIcon = { Icon(Icons.AutoMirrored.Outlined.Sort, contentDescription = null, modifier = Modifier.size(FilterChipDefaults.IconSize)) },
            )
            DropdownMenu(expanded = categoryMenu, onDismissRequest = { categoryMenu = false }) {
                DropdownMenuItem(text = { Text("Alle Kategorien") }, onClick = { viewModel.category = null; categoryMenu = false })
                viewModel.categories.forEach { category ->
                    DropdownMenuItem(text = { Text(category) }, onClick = { viewModel.category = category; categoryMenu = false })
                }
            }
        }
        if (viewModel.segment == InventarSegment.Werkzeug) {
            Box {
                FilterChip(
                    selected = viewModel.sort != WerkzeugSort.Name,
                    onClick = { sortMenu = true },
                    label = { Text("Sortierung: ${viewModel.sort.label}") },
                )
                DropdownMenu(expanded = sortMenu, onDismissRequest = { sortMenu = false }) {
                    WerkzeugSort.entries.forEach { sort ->
                        DropdownMenuItem(text = { Text(sort.label) }, onClick = { viewModel.sort = sort; sortMenu = false })
                    }
                }
            }
        }
    }
}

/** Farbpunkt + Name des Ablageorts. */
@Composable
fun StorageLocationLabel(name: String, color: Color?) {
    Row(verticalAlignment = Alignment.CenterVertically, horizontalArrangement = Arrangement.spacedBy(6.dp)) {
        Box(Modifier.size(8.dp).clip(CircleShape).background(color ?: MaterialTheme.colorScheme.outline))
        Text(name, maxLines = 1, overflow = TextOverflow.Ellipsis)
    }
}

@Composable
private fun Thumbnail(url: String?, icon: androidx.compose.ui.graphics.vector.ImageVector) {
    RemoteImage(url, placeholderIcon = icon, modifier = Modifier.size(56.dp).clip(RoundedCornerShape(12.dp)))
}

@Composable
fun WerkzeugRow(item: Werkzeug, locationColor: Color?, onClick: () -> Unit) {
    ListItem(
        modifier = Modifier.clickable(onClick = onClick),
        leadingContent = { Thumbnail(item.thumbnailUrl ?: item.imageUrl, Icons.Outlined.Build) },
        headlineContent = { Text(item.name, maxLines = 1, overflow = TextOverflow.Ellipsis) },
        supportingContent = {
            Column(verticalArrangement = Arrangement.spacedBy(4.dp)) {
                Row(horizontalArrangement = Arrangement.spacedBy(8.dp), verticalAlignment = Alignment.CenterVertically) {
                    Text(item.category, maxLines = 1)
                    item.storageLocation.nonEmpty?.let { StorageLocationLabel(it, locationColor) }
                }
                if (!item.isAvailable) {
                    Row(horizontalArrangement = Arrangement.spacedBy(6.dp), verticalAlignment = Alignment.CenterVertically) {
                        StatusPill(if (item.isOverdue) "Überfällig" else "Ausgeliehen", if (item.isOverdue) Tone.Red else Tone.Orange)
                        item.borrowedByName?.let { Text(it, style = MaterialTheme.typography.bodySmall, maxLines = 1) }
                    }
                }
            }
        },
    )
}

@Composable
fun MaterialRow(item: Verbrauchsmaterial, locationColor: Color?, onClick: () -> Unit) {
    ListItem(
        modifier = Modifier.clickable(onClick = onClick),
        leadingContent = { Thumbnail(item.thumbnailUrl ?: item.imageUrl, Icons.Outlined.Inventory2) },
        headlineContent = { Text(item.name, maxLines = 1, overflow = TextOverflow.Ellipsis) },
        supportingContent = {
            Column(verticalArrangement = Arrangement.spacedBy(4.dp)) {
                Row(horizontalArrangement = Arrangement.spacedBy(8.dp), verticalAlignment = Alignment.CenterVertically) {
                    Text(item.category, maxLines = 1)
                    item.storageLocation.nonEmpty?.let { StorageLocationLabel(it, locationColor) }
                }
                if (item.isNachbestellt) StatusPill("Nachbestellt", Tone.Blue)
            }
        },
        trailingContent = {
            Column(horizontalAlignment = Alignment.End) {
                Text(
                    "${item.quantity}",
                    style = MaterialTheme.typography.titleLarge,
                    fontWeight = FontWeight.Bold,
                    color = if (item.isLowStock) toneColor(Tone.Red) else MaterialTheme.colorScheme.onSurface,
                )
                Text(item.unit, style = MaterialTheme.typography.labelSmall)
            }
        },
    )
}
