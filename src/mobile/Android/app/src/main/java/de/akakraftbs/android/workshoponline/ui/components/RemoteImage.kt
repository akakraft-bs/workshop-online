package de.akakraftbs.android.workshoponline.ui.components

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.outlined.Image
import androidx.compose.material.icons.outlined.Person
import androidx.compose.material3.CircularProgressIndicator
import androidx.compose.material3.Icon
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.graphics.vector.ImageVector
import androidx.compose.ui.layout.ContentScale
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.semantics.clearAndSetSemantics
import androidx.compose.ui.unit.Dp
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import coil3.compose.AsyncImage
import coil3.compose.AsyncImagePainter
import de.akakraftbs.android.workshoponline.akakraftApp

/**
 * Bild vom Server mit Offline-Fallback: Coil liefert gesehene Bilder aus dem Disk-Cache;
 * fehlt das Bild dort und ist keine Verbindung da, wird die beim Vorladen gespeicherte
 * Vorschau aus dem [de.akakraftbs.android.workshoponline.core.cache.PreviewImageStore] gezeigt.
 */
@Composable
fun RemoteImage(
    url: String?,
    modifier: Modifier = Modifier,
    contentDescription: String? = null,
    contentScale: ContentScale = ContentScale.Crop,
    placeholderIcon: ImageVector = Icons.Outlined.Image,
) {
    val context = LocalContext.current
    val preview = remember(url) { context.akakraftApp.session.images.previews.existingFile(url) }
    var useFallback by remember(url) { mutableStateOf(false) }
    var state by remember(url) { mutableStateOf<AsyncImagePainter.State>(AsyncImagePainter.State.Empty) }

    Box(modifier.background(MaterialTheme.colorScheme.surfaceContainerHighest), contentAlignment = Alignment.Center) {
        if (url != null) {
            AsyncImage(
                model = if (useFallback) preview else url,
                contentDescription = contentDescription,
                contentScale = contentScale,
                modifier = Modifier.fillMaxSize(),
                onState = { newState ->
                    state = newState
                    if (newState is AsyncImagePainter.State.Error && !useFallback && preview != null) useFallback = true
                },
            )
        }
        when {
            url == null || state is AsyncImagePainter.State.Error ->
                Icon(placeholderIcon, contentDescription = null, tint = MaterialTheme.colorScheme.outline)
            state is AsyncImagePainter.State.Loading ->
                CircularProgressIndicator(modifier = Modifier.size(20.dp), strokeWidth = 2.dp)
        }
    }
}

@Composable
fun Avatar(name: String, pictureUrl: String?, modifier: Modifier = Modifier, size: Dp = 40.dp) {
    val shape = CircleShape
    if (!pictureUrl.isNullOrBlank()) {
        RemoteImage(
            url = pictureUrl,
            placeholderIcon = Icons.Outlined.Person,
            modifier = modifier.size(size).clip(shape).clearAndSetSemantics { },
        )
    } else {
        Box(
            modifier = modifier
                .size(size)
                .clip(shape)
                .background(MaterialTheme.colorScheme.primaryContainer)
                .clearAndSetSemantics { },
            contentAlignment = Alignment.Center,
        ) {
            Text(
                initials(name),
                color = MaterialTheme.colorScheme.onPrimaryContainer,
                style = MaterialTheme.typography.titleMedium.copy(fontSize = (size.value * 0.38f).sp),
            )
        }
    }
}

private fun initials(name: String): String =
    name.split(" ").filter { it.isNotBlank() }.take(2).map { it.first().uppercaseChar() }.joinToString("")
