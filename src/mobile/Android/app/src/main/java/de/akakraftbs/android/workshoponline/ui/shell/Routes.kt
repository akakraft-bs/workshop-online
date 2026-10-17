package de.akakraftbs.android.workshoponline.ui.shell

import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.automirrored.filled.MenuBook
import androidx.compose.material.icons.automirrored.outlined.MenuBook
import androidx.compose.material.icons.filled.CalendarMonth
import androidx.compose.material.icons.filled.Handyman
import androidx.compose.material.icons.filled.Home
import androidx.compose.material.icons.filled.MoreHoriz
import androidx.compose.material.icons.filled.ReportProblem
import androidx.compose.material.icons.filled.Search
import androidx.compose.material.icons.outlined.CalendarMonth
import androidx.compose.material.icons.outlined.Handyman
import androidx.compose.material.icons.outlined.Home
import androidx.compose.material.icons.outlined.MoreHoriz
import androidx.compose.material.icons.outlined.ReportProblem
import androidx.compose.material.icons.outlined.Search
import androidx.compose.ui.graphics.vector.ImageVector
import kotlinx.serialization.Serializable

// Typsichere Routen für Navigation Compose. Jeder Tab ist ein eigener Graph,
// damit Unterseiten den Tab markiert lassen und der Verlauf je Tab erhalten bleibt.

@Serializable data object DashboardGraph
@Serializable data object KalenderGraph
@Serializable data object HallenbuchGraph
@Serializable data object InventarGraph
@Serializable data object MaengelGraph
@Serializable data object SucheGraph
@Serializable data object MehrGraph

@Serializable data object DashboardRoute
@Serializable data object KalenderRoute
@Serializable data object HallenbuchRoute
@Serializable data object InventarRoute
@Serializable data object MaengelRoute
@Serializable data object SucheRoute
@Serializable data object MehrRoute
@Serializable data object ProfilRoute

/** Bereich, der noch nicht nativ umgesetzt ist – zeigt einen Platzhalter mit Link in die Web-App. */
@Serializable data class FeatureRoute(val title: String, val webRoute: String? = null)

/**
 * Hauptbereiche der App. Auf Smartphones zeigt die Navigationsleiste höchstens fünf Ziele
 * (Material-Empfehlung); Inventar und Mängel liegen dort unter „Mehr“, auf Tablets und
 * Foldables stehen sie als eigene Ziele in der Navigation Rail – wie auf iPhone/iPad.
 */
enum class TopLevel(
    val label: String,
    val icon: ImageVector,
    val selectedIcon: ImageVector,
    val graph: Any,
    val root: Any,
    val showOnCompact: Boolean = true,
) {
    Dashboard("Übersicht", Icons.Outlined.Home, Icons.Filled.Home, DashboardGraph, DashboardRoute),
    Kalender("Kalender", Icons.Outlined.CalendarMonth, Icons.Filled.CalendarMonth, KalenderGraph, KalenderRoute),
    Hallenbuch("Hallenbuch", Icons.AutoMirrored.Outlined.MenuBook, Icons.AutoMirrored.Filled.MenuBook, HallenbuchGraph, HallenbuchRoute),
    Inventar("Inventar", Icons.Outlined.Handyman, Icons.Filled.Handyman, InventarGraph, InventarRoute, showOnCompact = false),
    Maengel("Mängel", Icons.Outlined.ReportProblem, Icons.Filled.ReportProblem, MaengelGraph, MaengelRoute, showOnCompact = false),
    Suche("Suche", Icons.Outlined.Search, Icons.Filled.Search, SucheGraph, SucheRoute),
    Mehr("Mehr", Icons.Outlined.MoreHoriz, Icons.Filled.MoreHoriz, MehrGraph, MehrRoute),
}
