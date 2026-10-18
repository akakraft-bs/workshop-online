package de.akakraftbs.android.workshoponline.ui.shell

import androidx.compose.runtime.Composable
import androidx.navigation.NavGraphBuilder
import androidx.navigation.compose.composable
import de.akakraftbs.android.workshoponline.ui.components.LocalNavigator
import de.akakraftbs.android.workshoponline.ui.dashboard.DashboardScreen
import de.akakraftbs.android.workshoponline.ui.dashboard.dashboardDestinations
import de.akakraftbs.android.workshoponline.ui.gemeinschaft.aufgabenDestinations
import de.akakraftbs.android.workshoponline.ui.gemeinschaft.umfrageDestinations
import de.akakraftbs.android.workshoponline.ui.gemeinschaft.wunschDestinations
import de.akakraftbs.android.workshoponline.ui.halle.HallenbuchScreen
import de.akakraftbs.android.workshoponline.ui.halle.KalenderScreen
import de.akakraftbs.android.workshoponline.ui.halle.fahrzeugeDestinations
import de.akakraftbs.android.workshoponline.ui.halle.hallenbuchDestinations
import de.akakraftbs.android.workshoponline.ui.halle.kalenderDestinations
import de.akakraftbs.android.workshoponline.ui.halle.parkenDestinations
import de.akakraftbs.android.workshoponline.ui.inventar.InventarScreen
import de.akakraftbs.android.workshoponline.ui.inventar.inventarDestinations
import de.akakraftbs.android.workshoponline.ui.maengel.MaengelScreen
import de.akakraftbs.android.workshoponline.ui.maengel.maengelDestinations
import de.akakraftbs.android.workshoponline.ui.profil.FeedbackRoute
import de.akakraftbs.android.workshoponline.ui.profil.ProfilScreen
import de.akakraftbs.android.workshoponline.ui.profil.feedbackDestinations
import de.akakraftbs.android.workshoponline.ui.suche.SucheScreen
import de.akakraftbs.android.workshoponline.ui.verein.vereinDestinations
import de.akakraftbs.android.workshoponline.ui.verwaltung.adminDestinations
import de.akakraftbs.android.workshoponline.ui.verwaltung.crmDestinations

/** Startseite eines Tabs („Mehr“ registriert die Shell selbst). */
@Composable
fun TabRoot(tab: TopLevel) {
    when (tab) {
        TopLevel.Dashboard -> DashboardScreen()
        TopLevel.Kalender -> KalenderScreen()
        TopLevel.Hallenbuch -> HallenbuchScreen()
        TopLevel.Inventar -> InventarScreen()
        TopLevel.Maengel -> MaengelScreen()
        TopLevel.Suche -> SucheScreen()
        TopLevel.Mehr -> Unit
    }
}

/** Alle Unterseiten – aus jedem Tab erreichbar. */
fun NavGraphBuilder.appDestinations() {
    composable<ProfilRoute> {
        val nav = LocalNavigator.current
        ProfilScreen(onBack = nav::back, onFeedback = { nav.open(FeedbackRoute) })
    }
    feedbackDestinations()
    dashboardDestinations()
    kalenderDestinations()
    hallenbuchDestinations()
    parkenDestinations()
    fahrzeugeDestinations()
    inventarDestinations()
    maengelDestinations()
    umfrageDestinations()
    wunschDestinations()
    aufgabenDestinations()
    vereinDestinations()
    crmDestinations()
    adminDestinations()
}
