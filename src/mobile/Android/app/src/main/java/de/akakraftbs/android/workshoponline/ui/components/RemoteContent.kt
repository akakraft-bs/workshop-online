package de.akakraftbs.android.workshoponline.ui.components

import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.layout.widthIn
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.outlined.CloudOff
import androidx.compose.material.icons.outlined.ErrorOutline
import androidx.compose.material.icons.outlined.WifiOff
import androidx.compose.material3.AlertDialog
import androidx.compose.material3.Icon
import androidx.compose.material3.CircularProgressIndicator
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Surface
import androidx.compose.material3.Text
import androidx.compose.material3.TextButton
import androidx.compose.runtime.Composable
import androidx.compose.runtime.compositionLocalOf
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.vector.ImageVector
import androidx.compose.ui.semantics.contentDescription
import androidx.compose.ui.semantics.semantics
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.unit.dp
import de.akakraftbs.android.workshoponline.core.cache.ActionRunner
import de.akakraftbs.android.workshoponline.core.cache.Remote
import de.akakraftbs.android.workshoponline.core.network.ApiError
import java.time.Instant
import java.time.ZoneId
import java.time.format.DateTimeFormatter
import java.time.format.FormatStyle

/**
 * Verbindungsstatus für die ganze Oberfläche. Schreibende Bedienelemente nutzen
 * `enabled = LocalIsOnline.current` – Gegenstück zu `.requiresOnline()` auf iOS.
 */
val LocalIsOnline = compositionLocalOf { true }

/** Zeigt Laden, Fehler oder Inhalt einer [Remote]-Ressource an. */
@Composable
fun <T> RemoteContent(
    remote: Remote<T>,
    modifier: Modifier = Modifier,
    content: @Composable (T) -> Unit,
) {
    val value = remote.value
    val failure = remote.failure
    when {
        value != null -> Column(modifier) {
            if (failure != null) ErrorBanner(failure.message, Modifier.align(Alignment.CenterHorizontally))
            Box(Modifier.weight(1f)) { content(value) }
        }
        failure != null -> EmptyState(
            icon = if (failure == ApiError.Offline) Icons.Outlined.WifiOff else Icons.Outlined.ErrorOutline,
            title = if (failure == ApiError.Offline) "Offline" else "Laden fehlgeschlagen",
            message = if (failure == ApiError.Offline) {
                "Für diese Seite ist noch nichts gespeichert. Öffne sie einmal mit Verbindung, dann ist sie auch offline verfügbar."
            } else {
                failure.message
            },
            modifier = modifier,
        )
        else -> LoadingState(modifier)
    }
}

@Composable
fun LoadingState(modifier: Modifier = Modifier) {
    Box(modifier.fillMaxSize(), contentAlignment = Alignment.Center) {
        CircularProgressIndicator()
    }
}

@Composable
fun EmptyState(icon: ImageVector, title: String, message: String, modifier: Modifier = Modifier) {
    Column(
        modifier = modifier.fillMaxSize().padding(32.dp),
        verticalArrangement = Arrangement.spacedBy(12.dp, Alignment.CenterVertically),
        horizontalAlignment = Alignment.CenterHorizontally,
    ) {
        Icon(icon, contentDescription = null, modifier = Modifier.size(48.dp), tint = MaterialTheme.colorScheme.onSurfaceVariant)
        Text(title, style = MaterialTheme.typography.titleLarge, textAlign = TextAlign.Center)
        Text(
            message,
            style = MaterialTheme.typography.bodyMedium,
            color = MaterialTheme.colorScheme.onSurfaceVariant,
            textAlign = TextAlign.Center,
            modifier = Modifier.widthIn(max = 420.dp),
        )
    }
}

@Composable
fun ErrorBanner(message: String, modifier: Modifier = Modifier) {
    Surface(
        modifier = modifier.padding(vertical = 6.dp),
        shape = MaterialTheme.shapes.extraLarge,
        color = MaterialTheme.colorScheme.errorContainer,
        contentColor = MaterialTheme.colorScheme.onErrorContainer,
    ) {
        Row(
            modifier = Modifier.padding(horizontal = 14.dp, vertical = 8.dp),
            horizontalArrangement = Arrangement.spacedBy(8.dp),
            verticalAlignment = Alignment.CenterVertically,
        ) {
            Icon(Icons.Outlined.ErrorOutline, contentDescription = null, modifier = Modifier.size(18.dp))
            Text(message, style = MaterialTheme.typography.labelLarge)
        }
    }
}

/** Hinweis über der Navigationsleiste, solange keine Verbindung besteht. */
@Composable
fun OfflineBanner(modifier: Modifier = Modifier) {
    Surface(
        modifier = modifier
            .fillMaxWidth()
            .semantics { contentDescription = "Offline. Daten werden nur angezeigt, Änderungen sind nicht möglich." },
        color = MaterialTheme.colorScheme.inverseSurface,
        contentColor = MaterialTheme.colorScheme.inverseOnSurface,
    ) {
        Row(
            modifier = Modifier.padding(horizontal = 16.dp, vertical = 8.dp),
            horizontalArrangement = Arrangement.spacedBy(8.dp, Alignment.CenterHorizontally),
            verticalAlignment = Alignment.CenterVertically,
        ) {
            Icon(Icons.Outlined.CloudOff, contentDescription = null, modifier = Modifier.size(18.dp))
            Text("Offline – nur Ansicht, keine Änderungen", style = MaterialTheme.typography.labelLarge)
        }
    }
}

/** Fußzeile „Stand: …“ – offline besonders wichtig, um das Alter der Daten zu sehen. */
@Composable
fun LastUpdatedFooter(date: Instant?, modifier: Modifier = Modifier) {
    if (date == null) return
    val isOnline = LocalIsOnline.current
    Row(
        modifier = modifier.fillMaxWidth().padding(8.dp),
        horizontalArrangement = Arrangement.spacedBy(4.dp, Alignment.CenterHorizontally),
        verticalAlignment = Alignment.CenterVertically,
    ) {
        if (!isOnline) Icon(Icons.Outlined.WifiOff, contentDescription = null, modifier = Modifier.size(14.dp))
        Text(
            "Stand: ${date.dayTimeString()}",
            style = MaterialTheme.typography.labelSmall,
            color = MaterialTheme.colorScheme.onSurfaceVariant,
        )
    }
}

/** Fehlerdialog für einen [ActionRunner]. */
@Composable
fun ActionErrorDialog(runner: ActionRunner) {
    val message = runner.errorMessage ?: return
    AlertDialog(
        onDismissRequest = { runner.errorMessage = null },
        confirmButton = { TextButton(onClick = { runner.errorMessage = null }) { Text("OK") } },
        title = { Text("Aktion fehlgeschlagen") },
        text = { Text(message) },
    )
}

fun Instant.dayTimeString(): String =
    DateTimeFormatter.ofLocalizedDateTime(FormatStyle.MEDIUM, FormatStyle.SHORT)
        .withZone(ZoneId.systemDefault())
        .format(this)
