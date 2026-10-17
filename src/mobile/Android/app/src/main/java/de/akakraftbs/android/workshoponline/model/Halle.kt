package de.akakraftbs.android.workshoponline.model

import de.akakraftbs.android.workshoponline.core.network.ApiInstant
import de.akakraftbs.android.workshoponline.core.network.ResilientEnumSerializer
import kotlinx.serialization.Serializable
import java.time.Duration

// region Hallenbuch

@Serializable(with = GastschraubenArt.Serializer::class)
enum class GastschraubenArt(val raw: String, val label: String) {
    KastenPremiumbier("KastenPremiumbier", "Kasten Premiumbier"),
    ZwanzigEuroPayPal("ZwanzigEuroPayPal", "20 € per PayPal"),
    Unknown("Unknown", "Unbekannt");

    object Serializer : ResilientEnumSerializer<GastschraubenArt>("GastschraubenArt", entries.toTypedArray(), Unknown, GastschraubenArt::raw)

    companion object {
        val selectable = listOf(KastenPremiumbier, ZwanzigEuroPayPal)
    }
}

@Serializable
data class HallenbuchEintrag(
    val id: String,
    val userId: String,
    val userName: String,
    val start: ApiInstant,
    val end: ApiInstant,
    val description: String = "",
    val hatGastgeschraubt: Boolean = false,
    val gastschraubenArt: GastschraubenArt? = null,
    val gastschraubenBezahlt: Boolean? = null,
    val hatFamiliegeschraubt: Boolean = false,
    val createdAt: ApiInstant,
    val fahrzeugId: String? = null,
    val fahrzeugLabel: String? = null,
) {
    val duration: Duration get() = Duration.between(start, end)
}

@Serializable
data class HallenbuchInput(
    val start: ApiInstant,
    val end: ApiInstant,
    val description: String,
    val hatGastgeschraubt: Boolean,
    val gastschraubenArt: GastschraubenArt?,
    val gastschraubenBezahlt: Boolean?,
    val hatFamiliegeschraubt: Boolean,
    val fahrzeugId: String?,
)

// endregion

// region Hallen-Check-in & MOTD

@Serializable
data class HallenCheck(
    val id: String,
    val userId: String,
    val displayName: String,
    val pictureUrl: String? = null,
    val message: String? = null,
    val checkedInAt: ApiInstant,
    val expiresAt: ApiInstant,
)

@Serializable(with = MotdSeverity.Serializer::class)
enum class MotdSeverity(val raw: String, val label: String, val tone: Tone) {
    Info("Info", "Info", Tone.Blue),
    Warning("Warning", "Warnung", Tone.Orange),
    Critical("Critical", "Kritisch", Tone.Red),
    Unknown("Unknown", "Info", Tone.Blue);

    object Serializer : ResilientEnumSerializer<MotdSeverity>("MotdSeverity", entries.toTypedArray(), Unknown, MotdSeverity::raw)

    companion object {
        val selectable = listOf(Info, Warning, Critical)
    }
}

@Serializable
data class Motd(
    val id: String,
    val message: String,
    val severity: MotdSeverity = MotdSeverity.Info,
    val updatedAt: ApiInstant,
)

// endregion

// region Kalender

@Serializable
data class CalendarConfig(
    val id: String,
    val googleCalendarId: String = "",
    val name: String,
    val color: String = "#3f51b5",
    val isVisible: Boolean = true,
    val sortOrder: Int = 0,
    val calendarType: String = "",
    val grantsParkplatzBerechtigung: Boolean = false,
    val writeRoles: List<String> = emptyList(),
) {
    /** Entspricht `HasWriteAccess` im Backend. */
    fun canWrite(permissions: Permissions): Boolean =
        permissions.isPrivileged || writeRoles.any { role -> permissions.roles.any { it.raw == role } }
}

@Serializable
data class CalendarEvent(
    val id: String,
    val calendarId: String,
    val calendarName: String = "",
    val calendarColor: String = "#3f51b5",
    val title: String,
    val start: ApiInstant? = null,
    val end: ApiInstant? = null,
    val isAllDay: Boolean = false,
    val creatorName: String? = null,
    val creatorEmail: String? = null,
    val creatorUserId: String? = null,
    val description: String? = null,
    val location: String? = null,
    val url: String? = null,
    val fahrzeugId: String? = null,
    val fahrzeugLabel: String? = null,
)

@Serializable
data class CalendarEventInput(
    val calendarId: String?,
    val title: String,
    val start: ApiInstant,
    val end: ApiInstant,
    val isAllDay: Boolean,
    val description: String?,
    val location: String?,
    val url: String?,
    val fahrzeugId: String?,
)

// endregion

// region Fahrzeuge

@Serializable
data class Fahrzeug(
    val id: String,
    val marke: String,
    val modell: String? = null,
    val kennzeichen: String,
    val istStandard: Boolean = false,
) {
    val label: String get() = listOfNotNull(marke, modell.nonEmpty).joinToString(" ") + " · " + kennzeichen
}

@Serializable
data class FahrzeugInput(
    val marke: String,
    val modell: String?,
    val kennzeichen: String,
    val istStandard: Boolean,
)

// endregion
