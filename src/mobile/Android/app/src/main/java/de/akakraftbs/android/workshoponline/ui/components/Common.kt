package de.akakraftbs.android.workshoponline.ui.components

import android.content.ActivityNotFoundException
import android.content.Context
import android.content.Intent
import androidx.compose.foundation.background
import androidx.compose.foundation.isSystemInDarkTheme
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.lazy.LazyListScope
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.MoreVert
import androidx.compose.material3.AlertDialog
import androidx.compose.material3.DropdownMenu
import androidx.compose.material3.DropdownMenuItem
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButton
import androidx.compose.material3.ListItem
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Surface
import androidx.compose.material3.Text
import androidx.compose.material3.TextButton
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.vector.ImageVector
import androidx.compose.ui.platform.LocalConfiguration
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.core.net.toUri
import de.akakraftbs.android.workshoponline.core.auth.openInCustomTab
import de.akakraftbs.android.workshoponline.model.Tone
import java.time.Instant
import java.time.format.DateTimeFormatter
import java.time.ZoneId

/** Farbe zu einem [Tone], passend für Hell und Dunkel. */
@Composable
fun toneColor(tone: Tone): Color {
    val dark = isSystemInDarkTheme()
    return when (tone) {
        Tone.Blue -> if (dark) Color(0xFF8AB4F8) else Color(0xFF1A63C7)
        Tone.Orange -> if (dark) Color(0xFFFFB870) else Color(0xFFB45A00)
        Tone.Red -> MaterialTheme.colorScheme.error
        Tone.Green -> if (dark) Color(0xFF7DD99A) else Color(0xFF1E7B3C)
        Tone.Gray -> MaterialTheme.colorScheme.onSurfaceVariant
        Tone.Purple -> if (dark) Color(0xFFD0A8FF) else Color(0xFF7B3FC4)
        Tone.Teal -> if (dark) Color(0xFF6FD6CC) else Color(0xFF00796B)
    }
}

/** Farben aus dem Backend im Format `#RRGGBB`. */
fun hexColor(hex: String?): Color? {
    val text = hex?.trim()?.removePrefix("#") ?: return null
    if (text.length != 6) return null
    val value = text.toLongOrNull(16) ?: return null
    return Color(0xFF000000 or value)
}

@Composable
fun StatusPill(text: String, tone: Tone, icon: ImageVector? = null, modifier: Modifier = Modifier) {
    StatusPill(text, toneColor(tone), icon, modifier)
}

@Composable
fun StatusPill(text: String, color: Color, icon: ImageVector? = null, modifier: Modifier = Modifier) {
    Surface(modifier = modifier, shape = CircleShape, color = color.copy(alpha = 0.14f), contentColor = color) {
        Row(
            modifier = Modifier.padding(horizontal = 8.dp, vertical = 3.dp),
            horizontalArrangement = Arrangement.spacedBy(4.dp),
            verticalAlignment = Alignment.CenterVertically,
        ) {
            if (icon != null) Icon(icon, contentDescription = null, modifier = Modifier.size(14.dp))
            Text(text, style = MaterialTheme.typography.labelMedium, fontWeight = FontWeight.SemiBold)
        }
    }
}

/** Monat + Tag als kleines Kalenderblatt. */
@Composable
fun DateBadge(date: Instant, color: Color = MaterialTheme.colorScheme.primary) {
    val zone = ZoneId.systemDefault()
    val locale = LocalConfiguration.current.locales[0]
    Column(
        modifier = Modifier
            .size(48.dp)
            .clip(RoundedCornerShape(12.dp))
            .background(color.copy(alpha = 0.14f)),
        horizontalAlignment = Alignment.CenterHorizontally,
        verticalArrangement = Arrangement.Center,
    ) {
        Text(
            DateTimeFormatter.ofPattern("MMM", locale).withZone(zone).format(date).uppercase().trimEnd('.'),
            style = MaterialTheme.typography.labelSmall,
            fontWeight = FontWeight.Bold,
            color = color,
        )
        Text(DateTimeFormatter.ofPattern("d").withZone(zone).format(date), style = MaterialTheme.typography.titleMedium)
    }
}

/** Zeile mit Symbol, Bezeichnung und Wert für Detailansichten. */
@Composable
fun DetailRow(title: String, value: String, icon: ImageVector, modifier: Modifier = Modifier) {
    ListItem(
        modifier = modifier,
        leadingContent = { Icon(icon, contentDescription = null) },
        overlineContent = { Text(title) },
        headlineContent = { Text(value) },
    )
}

/** Abschnittsüberschrift in Listen. */
fun LazyListScope.sectionHeader(title: String, key: String = "header-$title", tone: Color? = null) {
    item(key = key) { SectionHeader(title, color = tone) }
}

@Composable
fun SectionHeader(title: String, modifier: Modifier = Modifier, color: Color? = null) {
    Text(
        title,
        style = MaterialTheme.typography.labelLarge,
        color = color ?: MaterialTheme.colorScheme.primary,
        modifier = modifier.padding(start = 16.dp, end = 16.dp, top = 20.dp, bottom = 4.dp),
    )
}

/** Runder Symbol-Container wie in den Android-Einstellungen. */
@Composable
fun IconBadge(icon: ImageVector, container: Color, content: Color, size: Int = 40) {
    Surface(shape = CircleShape, color = container, contentColor = content, modifier = Modifier.size(size.dp)) {
        Box(contentAlignment = Alignment.Center) { Icon(icon, contentDescription = null, modifier = Modifier.size((size * 0.55).dp)) }
    }
}

@Composable
fun ConfirmDialog(
    title: String,
    message: String? = null,
    confirmLabel: String = "Löschen",
    onConfirm: () -> Unit,
    onDismiss: () -> Unit,
) {
    AlertDialog(
        onDismissRequest = onDismiss,
        title = { Text(title) },
        text = message?.let { { Text(it) } },
        confirmButton = {
            TextButton(onClick = { onDismiss(); onConfirm() }) {
                Text(confirmLabel, color = MaterialTheme.colorScheme.error)
            }
        },
        dismissButton = { TextButton(onClick = onDismiss) { Text("Abbrechen") } },
    )
}

class MenuAction(val label: String, val icon: ImageVector? = null, val destructive: Boolean = false, val onClick: () -> Unit)

/** Drei-Punkte-Menü für die Titelleiste (Gegenstück zum iOS-„Verwalten“-Menü). */
@Composable
fun OverflowMenu(actions: List<MenuAction>, enabled: Boolean = true) {
    if (actions.isEmpty()) return
    var open by remember { mutableStateOf(false) }
    Box {
        IconButton(onClick = { open = true }, enabled = enabled) {
            Icon(Icons.Filled.MoreVert, contentDescription = "Weitere Aktionen")
        }
        DropdownMenu(expanded = open, onDismissRequest = { open = false }) {
            actions.forEach { action ->
                val color = if (action.destructive) MaterialTheme.colorScheme.error else MaterialTheme.colorScheme.onSurface
                DropdownMenuItem(
                    text = { Text(action.label, color = color) },
                    leadingIcon = action.icon?.let { { Icon(it, contentDescription = null, tint = color) } },
                    onClick = { open = false; action.onClick() },
                )
            }
        }
    }
}

/** Öffnet Links: Web-Adressen im Custom Tab, alles andere (tel:, mailto:, geo:) per Intent. */
fun Context.openLink(url: String) {
    val trimmed = url.trim()
    if (trimmed.startsWith("http://") || trimmed.startsWith("https://")) {
        openInCustomTab(trimmed)
        return
    }
    try {
        startActivity(Intent(Intent.ACTION_VIEW, trimmed.toUri()))
    } catch (_: ActivityNotFoundException) {
        // Keine App für diesen Link installiert.
    }
}

fun Context.dial(phone: String) = openLink("tel:${phone.filter { it.isDigit() || it == '+' }}")
fun Context.mail(address: String) = openLink("mailto:$address")
fun Context.showOnMap(address: String) = openLink("geo:0,0?q=${android.net.Uri.encode(address)}")
