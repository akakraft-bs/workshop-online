package de.akakraftbs.android.workshoponline.ui.shell

import androidx.compose.animation.AnimatedVisibility
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.material3.Badge
import androidx.compose.material3.BadgedBox
import androidx.compose.material3.Icon
import androidx.compose.material3.Text
import androidx.compose.material3.adaptive.currentWindowAdaptiveInfoV2
import androidx.compose.material3.adaptive.navigationsuite.NavigationSuiteScaffold
import androidx.compose.runtime.Composable
import androidx.compose.runtime.CompositionLocalProvider
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.saveable.rememberSaveable
import androidx.compose.runtime.setValue
import androidx.compose.ui.Modifier
import androidx.hilt.lifecycle.viewmodel.compose.hiltViewModel
import androidx.lifecycle.Lifecycle
import androidx.lifecycle.compose.LifecycleEventEffect
import androidx.navigation.NavController
import androidx.navigation.NavDestination.Companion.hasRoute
import androidx.navigation.NavDestination.Companion.hierarchy
import androidx.navigation.NavGraph.Companion.findStartDestination
import androidx.navigation.NavGraphBuilder
import androidx.navigation.compose.NavHost
import androidx.navigation.compose.composable
import androidx.navigation.compose.currentBackStackEntryAsState
import androidx.navigation.compose.navigation
import androidx.navigation.compose.rememberNavController
import androidx.window.core.layout.WindowSizeClass
import de.akakraftbs.android.workshoponline.ui.components.LocalIsOnline
import de.akakraftbs.android.workshoponline.ui.components.LocalNavigator
import de.akakraftbs.android.workshoponline.ui.components.Navigator
import de.akakraftbs.android.workshoponline.ui.components.OfflineBanner

/**
 * App-Shell: Navigationsleiste auf Smartphones, Navigation Rail auf Tablets/Foldables
 * (`NavigationSuiteScaffold` wählt automatisch nach Fensterbreite – Gegenstück zu
 * `.sidebarAdaptable` auf iOS). Offline erscheint über der Navigation ein Hinweis.
 *
 * Jeder Tab ist ein eigener Graph mit seiner Startseite; alle Unterseiten liegen auf oberster
 * Ebene (siehe [appDestinations]) und sind so aus jedem Tab erreichbar. Der aktive Tab ergibt
 * sich aus dem obersten Tab-Graphen im Back Stack.
 */
@Composable
fun MainScreen(viewModel: MainViewModel = hiltViewModel()) {
    val navController = rememberNavController()
    val navigator = remember(navController) { Navigator(navController) }
    val backStackEntry by navController.currentBackStackEntryAsState()
    val isCompact = !currentWindowAdaptiveInfoV2().windowSizeClass
        .isWidthAtLeastBreakpoint(WindowSizeClass.WIDTH_DP_MEDIUM_LOWER_BOUND)
    val isOnline = LocalIsOnline.current
    val tabs = TopLevel.entries.filter { it.showOnCompact || !isCompact }
    // Unterseiten liegen außerhalb der Tab-Graphen; dort bleibt der zuletzt aktive Tab markiert.
    var lastTab by rememberSaveable { mutableStateOf(TopLevel.Dashboard) }
    val destinationTab = backStackEntry?.destination?.let { destination ->
        TopLevel.entries.firstOrNull { tab -> destination.hierarchy.any { it.hasRoute(tab.graph::class) } }
    }
    LaunchedEffect(destinationTab) { if (destinationTab != null) lastTab = destinationTab }
    val currentTab = destinationTab ?: lastTab

    LifecycleEventEffect(Lifecycle.Event.ON_START) { viewModel.loadBadges() }
    LaunchedEffect(currentTab) { viewModel.loadBadges() }

    // Inventar und Mängel gibt es auf schmalen Fenstern nur unter „Mehr“.
    LaunchedEffect(isCompact, currentTab) {
        if (isCompact && !currentTab.showOnCompact) {
            lastTab = TopLevel.Dashboard
            navController.navigateToTab(TopLevel.Dashboard)
        }
    }

    CompositionLocalProvider(LocalNavigator provides navigator) {
        NavigationSuiteScaffold(
            navigationSuiteItems = {
                tabs.forEach { tab ->
                    val selected = tab == currentTab
                    val count = when (tab) {
                        TopLevel.Maengel -> viewModel.badges.value?.openMaengel ?: 0
                        TopLevel.Mehr -> viewModel.moreBadge(isCompact)
                        else -> 0
                    }
                    item(
                        selected = selected,
                        onClick = {
                            if (selected) {
                                navController.popBackStack(tab.root, inclusive = false)
                            } else {
                                lastTab = tab
                                navController.navigateToTab(tab)
                            }
                        },
                        icon = {
                            BadgedBox(badge = { if (count > 0) Badge { Text("$count") } }) {
                                Icon(if (selected) tab.selectedIcon else tab.icon, contentDescription = null)
                            }
                        },
                        label = { Text(tab.label) },
                    )
                }
            },
        ) {
            Column(Modifier.fillMaxSize()) {
                NavHost(
                    navController = navController,
                    startDestination = DashboardGraph,
                    modifier = Modifier.weight(1f),
                ) {
                    TopLevel.entries.forEach { tab -> tabGraph(tab, viewModel, isCompact) }
                    appDestinations()
                }
                AnimatedVisibility(visible = !isOnline) {
                    OfflineBanner()
                }
            }
        }
    }
}

private fun NavGraphBuilder.tabGraph(tab: TopLevel, viewModel: MainViewModel, isCompact: Boolean) {
    when (tab) {
        TopLevel.Dashboard -> navigation<DashboardGraph>(startDestination = DashboardRoute) { composable<DashboardRoute> { TabRoot(tab) } }
        TopLevel.Kalender -> navigation<KalenderGraph>(startDestination = KalenderRoute) { composable<KalenderRoute> { TabRoot(tab) } }
        TopLevel.Hallenbuch -> navigation<HallenbuchGraph>(startDestination = HallenbuchRoute) { composable<HallenbuchRoute> { TabRoot(tab) } }
        TopLevel.Inventar -> navigation<InventarGraph>(startDestination = InventarRoute) { composable<InventarRoute> { TabRoot(tab) } }
        TopLevel.Maengel -> navigation<MaengelGraph>(startDestination = MaengelRoute) { composable<MaengelRoute> { TabRoot(tab) } }
        TopLevel.Suche -> navigation<SucheGraph>(startDestination = SucheRoute) { composable<SucheRoute> { TabRoot(tab) } }
        TopLevel.Mehr -> navigation<MehrGraph>(startDestination = MehrRoute) {
            composable<MehrRoute> { MoreScreen(viewModel = viewModel, isCompact = isCompact) }
        }
    }
}

/** Tab-Wechsel mit eigenem Verlauf je Tab (Zustand wird gesichert und wiederhergestellt). */
private fun NavController.navigateToTab(tab: TopLevel) {
    navigate(tab.graph) {
        popUpTo(graph.findStartDestination().id) { saveState = true }
        launchSingleTop = true
        restoreState = true
    }
}
