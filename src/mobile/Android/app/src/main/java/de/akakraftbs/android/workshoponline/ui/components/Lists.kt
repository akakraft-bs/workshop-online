package de.akakraftbs.android.workshoponline.ui.components

import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.PaddingValues
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.LazyListScope
import androidx.compose.material3.ExperimentalMaterial3Api
import androidx.compose.material3.pulltorefresh.PullToRefreshBox
import androidx.compose.runtime.Composable
import androidx.compose.ui.Modifier
import androidx.compose.ui.unit.dp
import de.akakraftbs.android.workshoponline.core.cache.Remote

/** Platz unter dem letzten Listeneintrag, damit ein FAB nichts verdeckt. */
val FabSpacing = 88.dp

/**
 * Liste einer [Remote]-Ressource mit Pull-to-Refresh, Lade-/Fehlerzustand und „Stand: …“-Fußzeile.
 * [padding] ist das Innen-Padding des umgebenden [AppScaffold].
 */
@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun <T> RemoteList(
    remote: Remote<T>,
    padding: PaddingValues,
    isRefreshing: Boolean,
    onRefresh: () -> Unit,
    modifier: Modifier = Modifier,
    bottomSpacing: androidx.compose.ui.unit.Dp = FabSpacing,
    content: LazyListScope.(T) -> Unit,
) {
    Box(modifier.fillMaxSize().padding(top = padding.calculateTopPadding())) {
        RemoteContent(remote) { value ->
            PullToRefreshBox(isRefreshing = isRefreshing, onRefresh = onRefresh, modifier = Modifier.fillMaxSize()) {
                LazyColumn(Modifier.fillMaxSize(), contentPadding = PaddingValues(bottom = bottomSpacing)) {
                    content(value)
                    item(key = "last-updated") { LastUpdatedFooter(remote.lastUpdated) }
                }
            }
        }
    }
}

/** Pull-to-Refresh für Listen aus mehreren Quellen (z. B. Übersicht). */
@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun RefreshableList(
    padding: PaddingValues,
    isRefreshing: Boolean,
    onRefresh: () -> Unit,
    modifier: Modifier = Modifier,
    bottomSpacing: androidx.compose.ui.unit.Dp = 24.dp,
    content: LazyListScope.() -> Unit,
) {
    PullToRefreshBox(
        isRefreshing = isRefreshing,
        onRefresh = onRefresh,
        modifier = modifier.fillMaxSize().padding(top = padding.calculateTopPadding()),
    ) {
        LazyColumn(Modifier.fillMaxSize(), contentPadding = PaddingValues(bottom = bottomSpacing), content = content)
    }
}

/** Leerer Zustand innerhalb einer Liste. */
fun LazyListScope.emptyItem(icon: androidx.compose.ui.graphics.vector.ImageVector, title: String, message: String) {
    item(key = "empty") {
        EmptyState(icon, title, message, Modifier.padding(top = 48.dp))
    }
}
