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
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.getValue
import androidx.compose.ui.Modifier
import androidx.hilt.lifecycle.viewmodel.compose.hiltViewModel
import androidx.lifecycle.Lifecycle
import androidx.lifecycle.compose.LifecycleEventEffect
import androidx.navigation.NavController
import androidx.navigation.NavDestination
import androidx.navigation.NavDestination.Companion.hasRoute
import androidx.navigation.NavDestination.Companion.hierarchy
import androidx.navigation.NavGraph.Companion.findStartDestination
import androidx.navigation.NavGraphBuilder
import androidx.navigation.compose.NavHost
import androidx.navigation.compose.composable
import androidx.navigation.compose.currentBackStackEntryAsState
import androidx.navigation.compose.navigation
import androidx.navigation.compose.rememberNavController
import androidx.navigation.toRoute
import androidx.window.core.layout.WindowSizeClass
import de.akakraftbs.android.workshoponline.ui.components.LocalIsOnline
import de.akakraftbs.android.workshoponline.ui.components.OfflineBanner
import de.akakraftbs.android.workshoponline.ui.components.PlaceholderScreen
import de.akakraftbs.android.workshoponline.ui.profil.ProfilScreen

/**
 * App-Shell: Navigationsleiste auf Smartphones, Navigation Rail auf Tablets/Foldables
 * (`NavigationSuiteScaffold` wählt automatisch nach Fensterbreite – Gegenstück zu
 * `.sidebarAdaptable` auf iOS). Offline erscheint über der Navigation ein Hinweis.
 */
@Composable
fun MainScreen(viewModel: MainViewModel = hiltViewModel()) {
    val navController = rememberNavController()
    val backStackEntry by navController.currentBackStackEntryAsState()
    val destination = backStackEntry?.destination
    val isCompact = !currentWindowAdaptiveInfoV2().windowSizeClass
        .isWidthAtLeastBreakpoint(WindowSizeClass.WIDTH_DP_MEDIUM_LOWER_BOUND)
    val isOnline = LocalIsOnline.current
    val tabs = TopLevel.entries.filter { it.showOnCompact || !isCompact }
    val currentTab = TopLevel.entries.firstOrNull { destination.isIn(it) }

    LifecycleEventEffect(Lifecycle.Event.ON_START) { viewModel.loadBadges() }
    LaunchedEffect(currentTab) { if (currentTab != null) viewModel.loadBadges() }

    // Inventar und Mängel gibt es auf schmalen Fenstern nur unter „Mehr“.
    LaunchedEffect(isCompact, currentTab) {
        if (isCompact && currentTab?.showOnCompact == false) navController.navigateToTab(TopLevel.Dashboard)
    }

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
                tabRoot<DashboardGraph, DashboardRoute>(DashboardRoute) {
                    PlaceholderScreen("Übersicht", onBack = null, webUrl = viewModel.webUrl("dashboard"))
                }
                tabRoot<KalenderGraph, KalenderRoute>(KalenderRoute) {
                    PlaceholderScreen("Kalender", onBack = null, webUrl = viewModel.webUrl("kalender"))
                }
                tabRoot<HallenbuchGraph, HallenbuchRoute>(HallenbuchRoute) {
                    PlaceholderScreen("Hallenbuch", onBack = null, webUrl = viewModel.webUrl("hallenbuch"))
                }
                tabRoot<InventarGraph, InventarRoute>(InventarRoute) {
                    PlaceholderScreen("Inventar", onBack = null, webUrl = viewModel.webUrl("werkzeug"))
                }
                tabRoot<MaengelGraph, MaengelRoute>(MaengelRoute) {
                    PlaceholderScreen("Mängel", onBack = null, webUrl = viewModel.webUrl("mangel"))
                }
                tabRoot<SucheGraph, SucheRoute>(SucheRoute) {
                    PlaceholderScreen("Suche", onBack = null, webUrl = null)
                }
                navigation<MehrGraph>(startDestination = MehrRoute) {
                    composable<MehrRoute> {
                        MoreScreen(viewModel = viewModel, isCompact = isCompact, onOpen = { navController.navigate(it) })
                    }
                    composable<ProfilRoute> {
                        ProfilScreen(onBack = { navController.popBackStack() })
                    }
                    composable<FeatureRoute> { entry ->
                        val route = entry.toRoute<FeatureRoute>()
                        PlaceholderScreen(
                            title = route.title,
                            onBack = { navController.popBackStack() },
                            webUrl = route.webRoute?.let(viewModel::webUrl),
                        )
                    }
                }
            }
            AnimatedVisibility(visible = !isOnline) {
                OfflineBanner()
            }
        }
    }
}

private inline fun <reified G : Any, reified R : Any> NavGraphBuilder.tabRoot(
    root: R,
    crossinline content: @Composable () -> Unit,
) {
    navigation<G>(startDestination = root) {
        composable<R> { content() }
    }
}

private fun NavDestination?.isIn(tab: TopLevel): Boolean =
    this?.hierarchy?.any { it.hasRoute(tab.graph::class) } == true

/** Tab-Wechsel mit eigenem Verlauf je Tab (Zustand wird gesichert und wiederhergestellt). */
private fun NavController.navigateToTab(tab: TopLevel) {
    navigate(tab.graph) {
        popUpTo(graph.findStartDestination().id) { saveState = true }
        launchSingleTop = true
        restoreState = true
    }
}
