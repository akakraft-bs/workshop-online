package de.akakraftbs.android.workshoponline.model

import de.akakraftbs.android.workshoponline.core.network.ApiInstant
import kotlinx.serialization.Serializable

@Serializable
data class ParkOverview(
    val accounts: List<ParkAccountStatus> = emptyList(),
    val berechtigung: ParkBerechtigung,
    val anstehendeReservierungen: List<ParkReservierung> = emptyList(),
    val kalenderKonfiguriert: Boolean = false,
)

@Serializable
data class ParkAccountStatus(
    val id: String,
    val label: String,
    val portalUrl: String? = null,
    val notiz: String? = null,
    val istFrei: Boolean = true,
    val belegung: ParkClaim? = null,
    val zugangKonfiguriert: Boolean? = null,
    val kennzeichen: List<String>? = null,
    val kennzeichenFehler: String? = null,
)

@Serializable
data class ParkClaim(
    val id: String,
    val parkAccountId: String,
    val userId: String,
    val displayName: String,
    val pictureUrl: String? = null,
    val kennzeichen: String,
    val fahrzeugBezeichnung: String = "",
    val einfahrtAt: ApiInstant,
    val voraussichtlichBis: ApiInstant? = null,
    val autoExpiresAt: ApiInstant,
    val berechtigungArt: String = "",
)

@Serializable
data class ParkReservierung(
    val eventId: String,
    val titel: String,
    val start: ApiInstant? = null,
    val end: ApiInstant? = null,
    val istMeine: Boolean = false,
    val fahrzeugId: String? = null,
    val fahrzeugLabel: String? = null,
)

@Serializable
data class ParkBerechtigung(
    /** "Automatisch" | "Selbstbestaetigt" | "Spontan" */
    val art: String = "",
    val erfordertBestaetigung: Boolean = false,
    val hinweis: String = "",
    val waehlbareReservierungen: List<ParkReservierung> = emptyList(),
    val vorgeschlagenesFahrzeugId: String? = null,
    val vorgeschlagenesFahrzeugLabel: String? = null,
)

@Serializable
data class ParkCheckinInput(
    val parkAccountId: String,
    val fahrzeugId: String?,
    val kennzeichen: String?,
    val fahrzeugBezeichnung: String?,
    val einfahrtAt: ApiInstant?,
    val voraussichtlichBis: ApiInstant?,
    val bestaetigungAkzeptiert: Boolean,
    val bookingEventId: String?,
)

@Serializable
data class ParkHistorie(
    val id: String,
    /** "Belegung" | "KennzeichenHinzugefuegt" | "KennzeichenEntfernt" */
    val typ: String = "Belegung",
    val zeitpunkt: ApiInstant,
    val accountLabel: String = "",
    val displayName: String = "",
    val kennzeichen: String? = null,
    val fahrzeugBezeichnung: String? = null,
    val einfahrtAt: ApiInstant? = null,
    val freigegebenAt: ApiInstant? = null,
    val autoExpiresAt: ApiInstant? = null,
    val berechtigungArt: String? = null,
    val bestaetigungHinweis: String? = null,
    val status: String? = null,
) {
    val title: String
        get() = when (typ) {
            "KennzeichenHinzugefuegt" -> "Kennzeichen hinzugefügt"
            "KennzeichenEntfernt" -> "Kennzeichen entfernt"
            else -> "Belegung"
        }
}

@Serializable
data class ParkKennzeichenListe(
    val accountId: String,
    val accountLabel: String = "",
    val zugangKonfiguriert: Boolean = false,
    val max: Int = 0,
    val kennzeichen: List<String> = emptyList(),
    val fehler: String? = null,
)

@Serializable
data class ParkKennzeichenAudit(
    val id: String,
    val ausgefuehrtVon: String = "",
    val aktion: String = "",
    val kennzeichen: String = "",
    val kennzeichenNachher: List<String> = emptyList(),
    val createdAt: ApiInstant,
)
