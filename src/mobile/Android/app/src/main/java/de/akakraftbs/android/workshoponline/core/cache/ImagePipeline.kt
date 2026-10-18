package de.akakraftbs.android.workshoponline.core.cache

import android.content.Context
import android.graphics.Bitmap
import android.graphics.BitmapFactory
import androidx.core.graphics.scale
import coil3.ImageLoader
import dagger.hilt.android.qualifiers.ApplicationContext
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.withContext
import okhttp3.HttpUrl.Companion.toHttpUrlOrNull
import okhttp3.OkHttpClient
import okhttp3.Request
import java.io.File
import java.security.MessageDigest
import javax.inject.Inject
import javax.inject.Singleton

/**
 * Bilder: Gesehene liegen im Coil-Disk-Cache (400 MB), zusätzlich legt [OfflineSync]
 * verkleinerte Vorschauen im [PreviewImageStore] ab, die nicht verdrängt werden.
 */
@Singleton
class ImagePipeline @Inject constructor(
    val imageLoader: ImageLoader,
    val previews: PreviewImageStore,
    private val http: OkHttpClient,
) {
    /** Lädt ein Bild herunter und legt es verkleinert unter allen [keys] ab (z. B. Vorschau- und Originalbild). */
    suspend fun storePreview(source: String, keys: List<String>) {
        if (previews.containsAll(keys)) return
        val url = source.toHttpUrlOrNull() ?: return
        val bytes = withContext(Dispatchers.IO) {
            runCatching {
                http.newCall(Request.Builder().url(url).build()).execute().use { response ->
                    if (response.code == 200) response.body.bytes() else null
                }
            }.getOrNull()
        } ?: return
        previews.save(bytes, keys)
    }

    suspend fun clear() = withContext(Dispatchers.IO) {
        imageLoader.memoryCache?.clear()
        imageLoader.diskCache?.clear()
        previews.removeAll()
    }

    suspend fun diskUsage(): Long = withContext(Dispatchers.IO) {
        (imageLoader.diskCache?.size ?: 0L) + previews.diskUsage()
    }
}

/**
 * Verkleinerte Bilder, die beim Vorladen gespeichert werden. Anders als der Disk-Cache
 * werden sie nicht verdrängt, damit Listen und Details offline Bilder zeigen.
 * Liegt in `noBackupFilesDir`, also nicht im Cloud-Backup.
 */
@Singleton
class PreviewImageStore @Inject constructor(@ApplicationContext context: Context) {
    private val directory = File(context.noBackupFilesDir, "OfflinePreviews").apply { mkdirs() }

    fun file(url: String): File = File(directory, sha256(url) + ".jpg")

    fun existingFile(url: String?): File? = url?.let(::file)?.takeIf { it.exists() }

    fun containsAll(urls: List<String>): Boolean = urls.all { file(it).exists() }

    suspend fun save(data: ByteArray, urls: List<String>) = withContext(Dispatchers.IO) {
        val bounds = BitmapFactory.Options().apply { inJustDecodeBounds = true }
        BitmapFactory.decodeByteArray(data, 0, data.size, bounds)
        if (bounds.outWidth <= 0 || bounds.outHeight <= 0) return@withContext

        var sampleSize = 1
        while (maxOf(bounds.outWidth, bounds.outHeight) / (sampleSize * 2) >= MAX_PIXEL_SIZE) sampleSize *= 2
        val decoded = BitmapFactory.decodeByteArray(data, 0, data.size, BitmapFactory.Options().apply { inSampleSize = sampleSize })
            ?: return@withContext
        val scale = MAX_PIXEL_SIZE.toFloat() / maxOf(decoded.width, decoded.height)
        val bitmap = if (scale < 1f) decoded.scale((decoded.width * scale).toInt(), (decoded.height * scale).toInt()) else decoded

        for (url in urls) {
            file(url).outputStream().use { bitmap.compress(Bitmap.CompressFormat.JPEG, 70, it) }
        }
    }

    fun removeAll() {
        directory.deleteRecursively()
        directory.mkdirs()
    }

    fun diskUsage(): Long = directory.listFiles()?.sumOf { it.length() } ?: 0L

    private fun sha256(text: String): String =
        MessageDigest.getInstance("SHA-256").digest(text.toByteArray()).joinToString("") { "%02x".format(it) }

    private companion object {
        const val MAX_PIXEL_SIZE = 600
    }
}
