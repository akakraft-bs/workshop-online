package de.akakraftbs.android.workshoponline.ui.shell

import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.PaddingValues
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.LazyListScope
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.automirrored.filled.KeyboardArrowRight
import androidx.compose.material.icons.outlined.AdminPanelSettings
import androidx.compose.material.icons.outlined.AutoAwesome
import androidx.compose.material.icons.outlined.Build
import androidx.compose.material.icons.outlined.Checklist
import androidx.compose.material.icons.outlined.DirectionsCar
import androidx.compose.material.icons.outlined.Folder
import androidx.compose.material.icons.outlined.Groups
import androidx.compose.material.icons.outlined.Handshake
import androidx.compose.material.icons.outlined.Handyman
import androidx.compose.material.icons.outlined.Key
import androidx.compose.material.icons.outlined.LocalParking
import androidx.compose.material.icons.outlined.Poll
import androidx.compose.material.icons.outlined.ReportProblem
import androidx.compose.material3.Badge
import androidx.compose.material3.Icon
import androidx.compose.material3.ListItem
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Surface
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.vector.ImageVector
import androidx.compose.ui.unit.dp
import androidx.lifecycle.compose.collectAsStateWithLifecycle
import de.akakraftbs.android.workshoponline.ui.components.AppScaffold
import de.akakraftbs.android.workshoponline.ui.components.Avatar

/** Alle Bereiche, die nicht in der Navigation stehen – wie das Seitenmenü der Web-App. */
@Composable
fun MoreScreen(
    viewModel: MainViewModel,
    isCompact: Boolean,
    onOpen: (Any) -> Unit,
) {
    val user by viewModel.user.collectAsStateWithLifecycle()
    val permissions by viewModel.permissions.collectAsStateWithLifecycle()
    val badges = viewModel.badges.value
    val colors = MaterialTheme.colorScheme

    AppScaffold(title = "Mehr") { padding ->
        LazyColumn(
            modifier = Modifier.fillMaxSize(),
            contentPadding = PaddingValues(top = padding.calculateTopPadding(), bottom = 16.dp),
        ) {
            user?.let { current ->
                item(key = "profil") {
                    ListItem(
                        modifier = Modifier.clickable { onOpen(ProfilRoute) },
                        leadingContent = { Avatar(current.shownName, current.pictureUrl, size = 48.dp) },
                        headlineContent = { Text(current.shownName, style = MaterialTheme.typography.titleMedium) },
                        supportingContent = { Text("Profil & Einstellungen") },
                        trailingContent = { Icon(Icons.AutoMirrored.Filled.KeyboardArrowRight, contentDescription = null) },
                    )
                }
            }

            if (isCompact) {
                section("Werkstatt")
                link("Inventar", Icons.Outlined.Handyman, colors.tertiaryContainer, colors.onTertiaryContainer, "werkzeug", onOpen = onOpen)
                link("Mängel", Icons.Outlined.ReportProblem, colors.errorContainer, colors.onErrorContainer, "mangel", badges?.openMaengel, onOpen)
            }

            section("Halle")
            link("Campus-Parken", Icons.Outlined.LocalParking, colors.primaryContainer, colors.onPrimaryContainer, "parkplatz", onOpen = onOpen)
            link("Meine Fahrzeuge", Icons.Outlined.DirectionsCar, colors.secondaryContainer, colors.onSecondaryContainer, "fahrzeuge", onOpen = onOpen)

            section("Mitmachen")
            link("Umfragen", Icons.Outlined.Poll, colors.primaryContainer, colors.onPrimaryContainer, "umfrage", badges?.pendingUmfragen, onOpen)
            link("Wunschliste", Icons.Outlined.AutoAwesome, colors.tertiaryContainer, colors.onTertiaryContainer, "wunsch", onOpen = onOpen)
            link("Aufgaben", Icons.Outlined.Checklist, colors.secondaryContainer, colors.onSecondaryContainer, "aufgaben", badges?.openAufgaben, onOpen)

            section("Verein")
            link("Vereinsinfo", Icons.Outlined.Groups, colors.primaryContainer, colors.onPrimaryContainer, "verein/info", onOpen = onOpen)
            link("Dokumente", Icons.Outlined.Folder, colors.secondaryContainer, colors.onSecondaryContainer, "verein/dokumente", onOpen = onOpen)
            link("Zugänge", Icons.Outlined.Key, colors.tertiaryContainer, colors.onTertiaryContainer, "verein/zugaenge", onOpen = onOpen)
            link("Projekte", Icons.Outlined.Build, colors.primaryContainer, colors.onPrimaryContainer, "projekte", onOpen = onOpen)

            if (permissions.canSeeVorstandsbereich || permissions.isAdmin) {
                section("Verwaltung")
                if (permissions.canSeeVorstandsbereich) {
                    link("Partner & Sponsoren", Icons.Outlined.Handshake, colors.secondaryContainer, colors.onSecondaryContainer, "crm", onOpen = onOpen)
                }
                if (permissions.isAdmin) {
                    val adminBadge = (badges?.unseenFeedback ?: 0) + viewModel.usersWithoutAccess
                    link("Adminbereich", Icons.Outlined.AdminPanelSettings, colors.errorContainer, colors.onErrorContainer, "admin", adminBadge, onOpen)
                }
            }
        }
    }
}

private fun LazyListScope.section(title: String) {
    item(key = "section-$title") {
        Text(
            title,
            style = MaterialTheme.typography.labelLarge,
            color = MaterialTheme.colorScheme.primary,
            modifier = Modifier.padding(start = 16.dp, end = 16.dp, top = 24.dp, bottom = 4.dp),
        )
    }
}

private fun LazyListScope.link(
    title: String,
    icon: ImageVector,
    container: Color,
    content: Color,
    webRoute: String,
    badge: Int? = null,
    onOpen: (Any) -> Unit,
) {
    item(key = title) {
        ListItem(
            modifier = Modifier.clickable { onOpen(FeatureRoute(title, webRoute)) },
            leadingContent = {
                Surface(shape = CircleShape, color = container, contentColor = content, modifier = Modifier.size(40.dp)) {
                    Box(contentAlignment = Alignment.Center) {
                        Icon(icon, contentDescription = null, modifier = Modifier.size(22.dp))
                    }
                }
            },
            headlineContent = { Text(title) },
            trailingContent = {
                if (badge != null && badge > 0) Badge { Text("$badge") }
            },
        )
    }
}
