package de.akakraftbs.android.workshoponline.ui.components

import androidx.activity.compose.rememberLauncherForActivityResult
import androidx.activity.result.PickVisualMediaRequest
import androidx.activity.result.contract.ActivityResultContracts
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.outlined.AddPhotoAlternate
import androidx.compose.material.icons.outlined.DeleteOutline
import androidx.compose.material3.ButtonDefaults
import androidx.compose.material3.CircularProgressIndicator
import androidx.compose.material3.FilledTonalButton
import androidx.compose.material3.Icon
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Text
import androidx.compose.material3.TextButton
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.rememberCoroutineScope
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.unit.dp
import de.akakraftbs.android.workshoponline.core.auth.AppSession
import de.akakraftbs.android.workshoponline.core.network.ApiError
import de.akakraftbs.android.workshoponline.core.network.Uploads
import de.akakraftbs.android.workshoponline.model.ImageUploadResult
import kotlinx.coroutines.launch

/** Zustand eines Foto-Felds; gehört ins ViewModel des Formulars. */
class PhotoUpload(
    private val session: AppSession,
    private val uploadPath: String,
    imageUrl: String? = null,
    thumbnailUrl: String? = null,
) {
    var imageUrl by mutableStateOf(imageUrl)
    var thumbnailUrl by mutableStateOf(thumbnailUrl)
    var isUploading by mutableStateOf(false)
        private set
    var errorMessage by mutableStateOf<String?>(null)
        private set

    suspend fun upload(jpeg: ByteArray?) {
        if (jpeg == null) {
            errorMessage = "Das Bild konnte nicht gelesen werden."
            return
        }
        isUploading = true
        errorMessage = null
        try {
            val response = session.client.upload(uploadPath, jpeg, "foto.jpg", "image/jpeg")
            val result = session.client.decode(ImageUploadResult.serializer(), response)
            imageUrl = result.primaryUrl
            thumbnailUrl = result.thumbnailUrl
        } catch (error: ApiError) {
            errorMessage = error.message
        } finally {
            isUploading = false
        }
    }

    fun remove() {
        imageUrl = null
        thumbnailUrl = null
    }
}

/** Formularabschnitt zum Auswählen (System-Photo-Picker) und Hochladen eines Fotos. */
@Composable
fun PhotoUploadField(state: PhotoUpload, title: String = "Foto") {
    val context = LocalContext.current
    val scope = rememberCoroutineScope()
    val isOnline = LocalIsOnline.current
    val picker = rememberLauncherForActivityResult(ActivityResultContracts.PickVisualMedia()) { uri ->
        if (uri != null) scope.launch { state.upload(Uploads.jpeg(context, uri)) }
    }

    Column(verticalArrangement = Arrangement.spacedBy(8.dp)) {
        Text(title, style = MaterialTheme.typography.titleSmall)
        state.imageUrl?.let { url ->
            RemoteImage(url, modifier = Modifier.fillMaxWidth().height(180.dp).clip(RoundedCornerShape(12.dp)))
        }
        Row(horizontalArrangement = Arrangement.spacedBy(8.dp), verticalAlignment = Alignment.CenterVertically) {
            FilledTonalButton(
                onClick = { picker.launch(PickVisualMediaRequest(ActivityResultContracts.PickVisualMedia.ImageOnly)) },
                enabled = isOnline && !state.isUploading,
            ) {
                Icon(Icons.Outlined.AddPhotoAlternate, contentDescription = null, modifier = Modifier.size(ButtonDefaults.IconSize))
                Text(if (state.imageUrl == null) "  Foto auswählen" else "  Foto ersetzen")
            }
            if (state.imageUrl != null) {
                TextButton(onClick = state::remove) {
                    Icon(Icons.Outlined.DeleteOutline, contentDescription = null, modifier = Modifier.size(ButtonDefaults.IconSize))
                    Text("  Entfernen")
                }
            }
            if (state.isUploading) CircularProgressIndicator(modifier = Modifier.size(20.dp), strokeWidth = 2.dp)
        }
        state.errorMessage?.let { Text(it, color = MaterialTheme.colorScheme.error, style = MaterialTheme.typography.bodySmall) }
    }
}
