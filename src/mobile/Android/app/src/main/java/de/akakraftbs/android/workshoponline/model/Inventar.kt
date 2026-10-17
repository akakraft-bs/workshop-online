package de.akakraftbs.android.workshoponline.model

import de.akakraftbs.android.workshoponline.core.network.ApiInstant
import de.akakraftbs.android.workshoponline.core.network.ResilientEnumSerializer
import kotlinx.serialization.Serializable
import java.time.Instant

@Serializable
data class Werkzeug(
    val id: String,
    val name: String,
    val description: String = "",
    val category: String = "",
    val imageUrl: String? = null,
    val thumbnailUrl: String? = null,
    val dimensions: String? = null,
    val storageLocation: String? = null,
    val isAvailable: Boolean = true,
    val borrowedByUserId: String? = null,
    val borrowedByName: String? = null,
    val borrowedAt: ApiInstant? = null,
    val expectedReturnAt: ApiInstant? = null,
    val returnedAt: ApiInstant? = null,
    val createdAt: ApiInstant,
    val anleitungDokumentId: String? = null,
    val anleitungFileName: String? = null,
    val anleitungFileUrl: String? = null,
) {
    val isOverdue: Boolean get() = !isAvailable && expectedReturnAt?.isBefore(Instant.now()) == true
}

@Serializable
data class WerkzeugAusleihe(
    val id: String,
    val userId: String,
    val userName: String,
    val borrowedAt: ApiInstant,
    val expectedReturnAt: ApiInstant,
    val returnedAt: ApiInstant? = null,
)

@Serializable
data class WerkzeugInput(
    val name: String,
    val description: String,
    val category: String,
    val imageUrl: String?,
    val thumbnailUrl: String?,
    val dimensions: String?,
    val storageLocation: String?,
    val anleitungDokumentId: String?,
)

@Serializable
data class Verbrauchsmaterial(
    val id: String,
    val name: String,
    val description: String = "",
    val category: String = "",
    val unit: String = "",
    val quantity: Int = 0,
    val minQuantity: Int? = null,
    val imageUrl: String? = null,
    val thumbnailUrl: String? = null,
    val storageLocation: String? = null,
    val createdAt: ApiInstant,
    val isNachbestellt: Boolean = false,
    val nachbestelltVonName: String? = null,
    val nachbestelltAt: ApiInstant? = null,
) {
    val isLowStock: Boolean get() = minQuantity != null && quantity <= minQuantity
}

@Serializable
data class VerbrauchsmaterialInput(
    val name: String,
    val description: String,
    val category: String,
    val unit: String,
    val quantity: Int,
    val minQuantity: Int?,
    val imageUrl: String?,
    val thumbnailUrl: String?,
    val storageLocation: String?,
)

@Serializable
data class StorageLocation(val name: String, val color: String? = null)

// region Mängel

@Serializable(with = MangelKategorie.Serializer::class)
enum class MangelKategorie(val raw: String, val label: String) {
    Halle("Halle", "Halle"),
    Werkzeug("Werkzeug", "Werkzeug"),
    Sonstiges("Sonstiges", "Sonstiges"),
    Unknown("Unknown", "Unbekannt");

    object Serializer : ResilientEnumSerializer<MangelKategorie>("MangelKategorie", entries.toTypedArray(), Unknown, MangelKategorie::raw)

    companion object {
        val selectable = listOf(Halle, Werkzeug, Sonstiges)
    }
}

@Serializable(with = MangelStatus.Serializer::class)
enum class MangelStatus(val raw: String, val label: String, val tone: Tone) {
    Offen("Offen", "Offen", Tone.Red),
    Kenntnisgenommen("Kenntnisgenommen", "Zur Kenntnis genommen", Tone.Orange),
    Behoben("Behoben", "Behoben", Tone.Green),
    Abgelehnt("Abgelehnt", "Abgelehnt", Tone.Gray),
    Zurueckgezogen("Zurueckgezogen", "Zurückgezogen", Tone.Gray),
    Unknown("Unknown", "Unbekannt", Tone.Gray);

    val isActive: Boolean get() = this == Offen || this == Kenntnisgenommen

    object Serializer : ResilientEnumSerializer<MangelStatus>("MangelStatus", entries.toTypedArray(), Unknown, MangelStatus::raw)

    companion object {
        val settableByManagers = listOf(Offen, Kenntnisgenommen, Behoben, Abgelehnt)
    }
}

@Serializable
data class MangelAnmerkung(
    val id: String,
    val text: String,
    val createdByUserId: String,
    val createdByName: String,
    val createdAt: ApiInstant,
    val updatedAt: ApiInstant? = null,
)

@Serializable
data class Mangel(
    val id: String,
    val title: String,
    val description: String = "",
    val kategorie: MangelKategorie = MangelKategorie.Sonstiges,
    val status: MangelStatus = MangelStatus.Offen,
    val createdByUserId: String,
    val createdByName: String,
    val createdAt: ApiInstant,
    val imageUrl: String? = null,
    val resolvedByUserId: String? = null,
    val resolvedByName: String? = null,
    val resolvedAt: ApiInstant? = null,
    val note: String? = null,
    val anmerkungen: List<MangelAnmerkung> = emptyList(),
)

@Serializable
data class MangelInput(
    val title: String,
    val description: String,
    val kategorie: MangelKategorie,
    val imageUrl: String?,
)

// endregion
