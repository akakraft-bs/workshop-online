package de.akakraftbs.android.workshoponline.ui.components

import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.automirrored.outlined.OpenInNew
import androidx.compose.material.icons.outlined.Construction
import androidx.compose.material3.ButtonDefaults
import androidx.compose.material3.FilledTonalButton
import androidx.compose.material3.Icon
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.unit.dp
import de.akakraftbs.android.workshoponline.core.auth.openInCustomTab

/**
 * Platzhalter für Bereiche, die in der Android-App noch nicht umgesetzt sind.
 * Bietet – sofern vorhanden – den Weg in die Web-App an.
 */
@Composable
fun PlaceholderScreen(
    title: String,
    onBack: (() -> Unit)?,
    webUrl: String?,
) {
    val context = LocalContext.current
    AppScaffold(title = title, onBack = onBack) { padding ->
        Column(
            modifier = Modifier.fillMaxSize().padding(padding).padding(32.dp),
            verticalArrangement = Arrangement.spacedBy(16.dp, Alignment.CenterVertically),
            horizontalAlignment = Alignment.CenterHorizontally,
        ) {
            Icon(
                Icons.Outlined.Construction,
                contentDescription = null,
                modifier = Modifier.size(48.dp),
                tint = MaterialTheme.colorScheme.onSurfaceVariant,
            )
            Text("Folgt in Kürze", style = MaterialTheme.typography.titleLarge)
            Text(
                "Dieser Bereich ist in der Android-App noch in Arbeit.",
                style = MaterialTheme.typography.bodyMedium,
                color = MaterialTheme.colorScheme.onSurfaceVariant,
                textAlign = TextAlign.Center,
            )
            if (webUrl != null) {
                FilledTonalButton(onClick = { context.openInCustomTab(webUrl) }) {
                    Icon(Icons.AutoMirrored.Outlined.OpenInNew, contentDescription = null, modifier = Modifier.size(ButtonDefaults.IconSize))
                    Text("In der Web-App öffnen", modifier = Modifier.padding(start = ButtonDefaults.IconSpacing))
                }
            }
        }
    }
}
