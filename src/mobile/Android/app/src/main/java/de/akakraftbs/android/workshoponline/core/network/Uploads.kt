package de.akakraftbs.android.workshoponline.core.network

import android.content.Context
import android.graphics.Bitmap
import android.graphics.ImageDecoder
import android.net.Uri
import android.os.Build
import android.provider.OpenableColumns
import android.graphics.BitmapFactory
import androidx.core.graphics.scale
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.withContext
import java.io.ByteArrayOutputStream

/** Eine vom Nutzer gewählte Datei (Dokument, Projektplan). */
class PickedFile(val name: String, val mimeType: String, val bytes: ByteArray)

object Uploads {
    /** Verkleinert auf max. 2048 px Kantenlänge, damit Uploads unter dem 10-MB-Limit bleiben. */
    suspend fun jpeg(context: Context, uri: Uri, maxDimension: Int = 2048): ByteArray? = withContext(Dispatchers.IO) {
        runCatching {
            val bitmap = if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.P) {
                ImageDecoder.decodeBitmap(ImageDecoder.createSource(context.contentResolver, uri)) { decoder, info, _ ->
                    val largest = maxOf(info.size.width, info.size.height)
                    if (largest > maxDimension) {
                        val scale = maxDimension.toFloat() / largest
                        decoder.setTargetSize((info.size.width * scale).toInt(), (info.size.height * scale).toInt())
                    }
                    decoder.allocator = ImageDecoder.ALLOCATOR_SOFTWARE
                }
            } else {
                context.contentResolver.openInputStream(uri)?.use(BitmapFactory::decodeStream)?.let { source ->
                    val largest = maxOf(source.width, source.height)
                    if (largest <= maxDimension) source
                    else source.scale(source.width * maxDimension / largest, source.height * maxDimension / largest)
                }
            } ?: return@runCatching null
            ByteArrayOutputStream().also { bitmap.compress(Bitmap.CompressFormat.JPEG, 82, it) }.toByteArray()
        }.getOrNull()
    }

    suspend fun read(context: Context, uri: Uri): PickedFile? = withContext(Dispatchers.IO) {
        runCatching {
            val resolver = context.contentResolver
            val name = resolver.query(uri, arrayOf(OpenableColumns.DISPLAY_NAME), null, null, null)?.use { cursor ->
                if (cursor.moveToFirst()) cursor.getString(0) else null
            } ?: uri.lastPathSegment ?: "datei"
            val mime = resolver.getType(uri) ?: "application/octet-stream"
            val bytes = resolver.openInputStream(uri)?.use { it.readBytes() } ?: return@runCatching null
            PickedFile(name, mime, bytes)
        }.getOrNull()
    }
}
