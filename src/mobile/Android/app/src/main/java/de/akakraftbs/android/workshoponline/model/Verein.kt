package de.akakraftbs.android.workshoponline.model

import de.akakraftbs.android.workshoponline.core.network.ApiInstant
import de.akakraftbs.android.workshoponline.core.network.ResilientEnumSerializer
import kotlinx.serialization.Serializable

@Serializable
data class AmtsTraeger(
    val role: String,
    val roleLabel: String,
    val userId: String,
    val userName: String,
    val phone: String? = null,
    val address: String? = null,
)

@Serializable
data class Schluesselhinterlegung(
    val id: String,
    val name: String,
    val address: String = "",
    val phone: String? = null,
    val sortOrder: Int = 0,
)

@Serializable
data class SchluesselInput(val name: String, val address: String, val phone: String?)

@Serializable
data class VereinInfo(
    val amtstraeger: List<AmtsTraeger> = emptyList(),
    val schluessel: List<Schluesselhinterlegung> = emptyList(),
)

@Serializable
data class Dokument(
    val id: String,
    val folderId: String,
    val fileName: String,
    val fileUrl: String,
    val uploadedByName: String = "",
    val uploadedAt: ApiInstant,
    val fileSizeBytes: Long? = null,
) {
    val extension: String get() = fileName.substringAfterLast('.', "").lowercase()
}

@Serializable
data class DokumentOrdner(
    val id: String,
    val name: String,
    val createdAt: ApiInstant,
    val dokumente: List<Dokument> = emptyList(),
)

@Serializable
data class DokumentInput(val folderId: String, val fileName: String, val fileUrl: String, val fileSizeBytes: Long?)

@Serializable
data class VereinZugang(val id: String, val anbieter: String, val zugangsdaten: String = "")

@Serializable
data class VereinZugangInput(val anbieter: String, val zugangsdaten: String)

// region Projekte

@Serializable(with = ProjektStatus.Serializer::class)
enum class ProjektStatus(val raw: String, val label: String, val tone: Tone) {
    Geplant("Geplant", "Geplant", Tone.Blue),
    Gestartet("Gestartet", "Gestartet", Tone.Orange),
    Abgeschlossen("Abgeschlossen", "Abgeschlossen", Tone.Green),
    Unknown("Unknown", "Unbekannt", Tone.Gray);

    object Serializer : ResilientEnumSerializer<ProjektStatus>("ProjektStatus", entries.toTypedArray(), Unknown, ProjektStatus::raw)

    companion object {
        val selectable = listOf(Geplant, Gestartet, Abgeschlossen)
    }
}

@Serializable
data class Projekt(
    val id: String,
    val name: String,
    val description: String? = null,
    val plannedStartDate: ApiInstant,
    val durationWeeks: Int = 1,
    val actualStartDate: ApiInstant? = null,
    val actualEndDate: ApiInstant? = null,
    val expectedEndDate: ApiInstant,
    val status: ProjektStatus = ProjektStatus.Geplant,
    val projektplanUrl: String? = null,
    val createdByName: String = "",
    val createdAt: ApiInstant,
)

@Serializable
data class ProjektInput(
    val name: String,
    val description: String?,
    val plannedStartDate: ApiInstant,
    val durationWeeks: Int,
    val actualStartDate: ApiInstant?,
    val status: ProjektStatus,
    val projektplanUrl: String?,
)

// endregion
