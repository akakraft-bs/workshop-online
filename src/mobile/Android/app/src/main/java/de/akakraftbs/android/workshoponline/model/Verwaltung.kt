package de.akakraftbs.android.workshoponline.model

import de.akakraftbs.android.workshoponline.core.network.ApiInstant
import de.akakraftbs.android.workshoponline.core.network.ResilientEnumSerializer
import kotlinx.serialization.Serializable

// region CRM

@Serializable(with = PartnerStatus.Serializer::class)
enum class PartnerStatus(val raw: String, val label: String, val tone: Tone) {
    Potentiell("Potentiell", "Potentiell", Tone.Gray),
    Angeschrieben("Angeschrieben", "Angeschrieben", Tone.Blue),
    InVerhandlung("InVerhandlung", "In Verhandlung", Tone.Orange),
    Aktiv("Aktiv", "Aktiv", Tone.Green),
    Abgelehnt("Abgelehnt", "Abgelehnt", Tone.Red),
    Inaktiv("Inaktiv", "Inaktiv", Tone.Gray),
    Unknown("Unknown", "Unbekannt", Tone.Gray);

    object Serializer : ResilientEnumSerializer<PartnerStatus>("PartnerStatus", entries.toTypedArray(), Unknown, PartnerStatus::raw)

    companion object {
        val selectable = entries.filter { it != Unknown }
    }
}

@Serializable(with = KontaktKanal.Serializer::class)
enum class KontaktKanal(val raw: String, val label: String) {
    Email("Email", "E-Mail"),
    Telefon("Telefon", "Telefon"),
    Meeting("Meeting", "Meeting"),
    Brief("Brief", "Brief"),
    Sonstiges("Sonstiges", "Sonstiges"),
    Unknown("Unknown", "Unbekannt");

    object Serializer : ResilientEnumSerializer<KontaktKanal>("KontaktKanal", entries.toTypedArray(), Unknown, KontaktKanal::raw)

    companion object {
        val selectable = entries.filter { it != Unknown }
    }
}

@Serializable(with = KontaktReaktion.Serializer::class)
enum class KontaktReaktion(val raw: String, val label: String, val tone: Tone) {
    Positiv("Positiv", "Positiv", Tone.Green),
    Neutral("Neutral", "Neutral", Tone.Blue),
    Negativ("Negativ", "Negativ", Tone.Red),
    KeineAntwort("KeineAntwort", "Keine Antwort", Tone.Gray),
    Unknown("Unknown", "Unbekannt", Tone.Gray);

    object Serializer : ResilientEnumSerializer<KontaktReaktion>("KontaktReaktion", entries.toTypedArray(), Unknown, KontaktReaktion::raw)

    companion object {
        val selectable = entries.filter { it != Unknown }
    }
}

@Serializable
data class PartnerOverview(
    val id: String,
    val name: String,
    val kategorie: String? = null,
    val status: PartnerStatus = PartnerStatus.Potentiell,
    val website: String? = null,
    val anzahlKontakte: Int = 0,
    val letzterKontakt: ApiInstant? = null,
)

@Serializable
data class Ansprechpartner(
    val id: String,
    val name: String,
    val position: String? = null,
    val email: String? = null,
    val telefon: String? = null,
    val notizen: String? = null,
)

@Serializable
data class Kontakteintrag(
    val id: String,
    val ansprechpartnerId: String? = null,
    val ansprechpartnerName: String? = null,
    val datum: ApiInstant,
    val kanal: KontaktKanal = KontaktKanal.Sonstiges,
    val reaktion: KontaktReaktion = KontaktReaktion.KeineAntwort,
    val zusammenfassung: String = "",
    val naechsteSchritte: String? = null,
    val erstelltAm: ApiInstant,
)

@Serializable
data class PartnerDetail(
    val id: String,
    val name: String,
    val kategorie: String? = null,
    val status: PartnerStatus = PartnerStatus.Potentiell,
    val website: String? = null,
    val notizen: String? = null,
    val ansprechpartner: List<Ansprechpartner> = emptyList(),
    val kontakteintraege: List<Kontakteintrag> = emptyList(),
)

@Serializable
data class PartnerInput(
    val name: String,
    val kategorie: String?,
    val status: PartnerStatus,
    val website: String?,
    val notizen: String?,
)

@Serializable
data class AnsprechpartnerInput(
    val name: String,
    val position: String?,
    val email: String?,
    val telefon: String?,
    val notizen: String?,
)

@Serializable
data class KontakteintragInput(
    val ansprechpartnerId: String?,
    val datum: ApiInstant,
    val kanal: KontaktKanal,
    val reaktion: KontaktReaktion,
    val zusammenfassung: String,
    val naechsteSchritte: String?,
)

// endregion

// region Feedback

@Serializable(with = FeedbackStatus.Serializer::class)
enum class FeedbackStatus(val raw: String, val label: String, val tone: Tone) {
    New("New", "Neu", Tone.Blue),
    Read("Read", "Gelesen", Tone.Orange),
    Done("Done", "Erledigt", Tone.Green),
    Unknown("Unknown", "Unbekannt", Tone.Gray);

    object Serializer : ResilientEnumSerializer<FeedbackStatus>("FeedbackStatus", entries.toTypedArray(), Unknown, FeedbackStatus::raw)

    companion object {
        val selectable = listOf(New, Read, Done)
    }
}

@Serializable
data class Feedback(
    val id: String,
    val userId: String,
    val userName: String,
    val text: String,
    val pageUrl: String = "",
    val appVersion: String? = null,
    val serverVersion: String? = null,
    val status: FeedbackStatus = FeedbackStatus.New,
    val createdAt: ApiInstant,
)

// endregion

// region Ablageorte

@Serializable
data class AblageortOverview(
    val id: String? = null,
    val name: String,
    val color: String? = null,
    val itemCount: Int = 0,
) {
    val stableId: String get() = id ?: "name:$name"
}

// endregion
