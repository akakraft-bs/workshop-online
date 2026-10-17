package de.akakraftbs.android.workshoponline.model

import kotlinx.serialization.Serializable

/** Farbton für Status-Kennzeichnungen. Die konkreten Farben legt die Oberfläche je Hell/Dunkel fest. */
enum class Tone { Blue, Orange, Red, Green, Gray, Purple, Teal }

@Serializable
data class PagedResult<T>(
    val items: List<T> = emptyList(),
    val total: Int = 0,
    val page: Int = 0,
    val pageSize: Int = 0,
)

@Serializable
data class ImageUploadResult(
    val imageUrl: String? = null,
    val thumbnailUrl: String? = null,
    val url: String? = null,
) {
    val primaryUrl: String? get() = imageUrl ?: url
}

@Serializable
data class FileUploadResult(
    val url: String? = null,
    val fileUrl: String? = null,
    val fileName: String? = null,
    val fileSizeBytes: Long? = null,
) {
    val resolvedUrl: String? get() = url ?: fileUrl
}

/** `null` für fehlende oder leere/Whitespace-Strings. */
val String?.nonEmpty: String? get() = this?.trim()?.takeIf { it.isNotEmpty() }
