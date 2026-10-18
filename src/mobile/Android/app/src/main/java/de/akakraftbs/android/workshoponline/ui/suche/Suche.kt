package de.akakraftbs.android.workshoponline.ui.suche

import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.PaddingValues
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.lazy.items
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.outlined.Search
import androidx.compose.material.icons.outlined.SearchOff
import androidx.compose.material3.Icon
import androidx.compose.material3.ListItem
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.setValue
import androidx.compose.ui.Modifier
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.unit.dp
import androidx.hilt.lifecycle.viewmodel.compose.hiltViewModel
import androidx.lifecycle.SavedStateHandle
import dagger.hilt.android.lifecycle.HiltViewModel
import de.akakraftbs.android.workshoponline.core.auth.AppSession
import de.akakraftbs.android.workshoponline.core.cache.remote
import de.akakraftbs.android.workshoponline.model.DokumentOrdner
import de.akakraftbs.android.workshoponline.model.Mangel
import de.akakraftbs.android.workshoponline.model.Verbrauchsmaterial
import de.akakraftbs.android.workshoponline.model.Werkzeug
import de.akakraftbs.android.workshoponline.ui.components.AppScaffold
import de.akakraftbs.android.workshoponline.ui.components.EmptyState
import de.akakraftbs.android.workshoponline.ui.components.FeatureViewModel
import de.akakraftbs.android.workshoponline.ui.components.LocalNavigator
import de.akakraftbs.android.workshoponline.ui.components.RefreshableList
import de.akakraftbs.android.workshoponline.ui.components.emptyItem
import de.akakraftbs.android.workshoponline.ui.components.openLink
import de.akakraftbs.android.workshoponline.ui.components.sectionHeader
import de.akakraftbs.android.workshoponline.ui.components.toArg
import de.akakraftbs.android.workshoponline.ui.inventar.MaterialDetailRoute
import de.akakraftbs.android.workshoponline.ui.inventar.MaterialRow
import de.akakraftbs.android.workshoponline.ui.inventar.SearchField
import de.akakraftbs.android.workshoponline.ui.inventar.WerkzeugDetailRoute
import de.akakraftbs.android.workshoponline.ui.inventar.WerkzeugRow
import de.akakraftbs.android.workshoponline.ui.maengel.MangelDetailRoute
import de.akakraftbs.android.workshoponline.ui.maengel.MangelRow
import de.akakraftbs.android.workshoponline.ui.verein.icon
import javax.inject.Inject

/** Übergreifende Suche über Inventar, Mängel und Dokumente – funktioniert auch offline auf dem Cache. */
@HiltViewModel
class SucheViewModel @Inject constructor(session: AppSession, private val saved: SavedStateHandle) : FeatureViewModel(session) {
    val werkzeug = remote<List<Werkzeug>>("werkzeug")
    val material = remote<List<Verbrauchsmaterial>>("verbrauchsmaterial")
    val maengel = remote<List<Mangel>>("mangel")
    val dokumente = remote<List<DokumentOrdner>>("verein/dokumente")

    var query by mutableStateOf(saved.get<String>("query").orEmpty())
        private set

    fun updateQuery(value: String) {
        query = value
        saved["query"] = value
    }

    override suspend fun reloadAll() {
        loadAll(werkzeug, material, maengel, dokumente)
    }

    fun matches(vararg fields: String?): Boolean {
        val needle = query.trim().lowercase()
        return fields.any { it?.lowercase()?.contains(needle) == true }
    }
}

@Composable
fun SucheScreen(viewModel: SucheViewModel = hiltViewModel()) {
    val nav = LocalNavigator.current
    val context = LocalContext.current
    LaunchedEffect(Unit) { viewModel.reload() }

    AppScaffold(title = "Suche") { padding ->
        Column(Modifier.fillMaxSize().padding(top = padding.calculateTopPadding())) {
            SearchField(viewModel.query, viewModel::updateQuery, "Werkzeug, Material, Mängel …")
            val text = viewModel.query.trim()
            if (text.isEmpty()) {
                EmptyState(Icons.Outlined.Search, "Suchen", "Finde Werkzeug, Material, Mängel und Dokumente.")
                return@Column
            }
            val tools = viewModel.werkzeug.value.orEmpty().filter { viewModel.matches(it.name, it.description, it.category, it.storageLocation) }
            val materials = viewModel.material.value.orEmpty().filter { viewModel.matches(it.name, it.description, it.category, it.storageLocation) }
            val defects = viewModel.maengel.value.orEmpty().filter { viewModel.matches(it.title, it.description) }
            val docs = viewModel.dokumente.value.orEmpty().flatMap { it.dokumente }.filter { viewModel.matches(it.fileName) }

            RefreshableList(PaddingValues(0.dp), viewModel.isRefreshing, viewModel::refresh) {
                if (tools.isEmpty() && materials.isEmpty() && defects.isEmpty() && docs.isEmpty()) {
                    emptyItem(Icons.Outlined.SearchOff, "Keine Treffer", "Für „$text“ wurde nichts gefunden.")
                }
                if (tools.isNotEmpty()) {
                    sectionHeader("Werkzeug")
                    items(tools, key = { "w-${it.id}" }) { item -> WerkzeugRow(item, null) { nav.open(WerkzeugDetailRoute(item.toArg())) } }
                }
                if (materials.isNotEmpty()) {
                    sectionHeader("Verbrauchsmaterial")
                    items(materials, key = { "m-${it.id}" }) { item -> MaterialRow(item, null) { nav.open(MaterialDetailRoute(item.toArg())) } }
                }
                if (defects.isNotEmpty()) {
                    sectionHeader("Mängel")
                    items(defects, key = { "d-${it.id}" }) { mangel -> MangelRow(mangel) { nav.open(MangelDetailRoute(mangel.toArg())) } }
                }
                if (docs.isNotEmpty()) {
                    sectionHeader("Dokumente")
                    items(docs, key = { "doc-${it.id}" }) { dokument ->
                        ListItem(
                            modifier = Modifier.clickable { context.openLink(dokument.fileUrl) },
                            leadingContent = { Icon(dokument.icon, contentDescription = null) },
                            headlineContent = { Text(dokument.fileName) },
                        )
                    }
                }
            }
        }
    }
}
