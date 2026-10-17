package de.akakraftbs.android.workshoponline.model

import de.akakraftbs.android.workshoponline.core.network.ApiInstant
import de.akakraftbs.android.workshoponline.core.network.ResilientEnumSerializer
import kotlinx.serialization.Serializable
import java.text.NumberFormat
import java.util.Locale

// region Wunschliste

@Serializable(with = WunschStatus.Serializer::class)
enum class WunschStatus(val raw: String, val label: String, val tone: Tone) {
    Offen("Offen", "Offen", Tone.Blue),
    Angeschafft("Angeschafft", "Angeschafft", Tone.Green),
    Abgelehnt("Abgelehnt", "Abgelehnt", Tone.Gray),
    Unknown("Unknown", "Unbekannt", Tone.Gray);

    object Serializer : ResilientEnumSerializer<WunschStatus>("WunschStatus", entries.toTypedArray(), Unknown, WunschStatus::raw)
}

@Serializable
data class Wunsch(
    val id: String,
    val title: String,
    val description: String = "",
    val link: String? = null,
    val priceMin: Double? = null,
    val priceMax: Double? = null,
    val status: WunschStatus = WunschStatus.Offen,
    val createdByUserId: String,
    val createdByName: String,
    val createdAt: ApiInstant,
    val upVotes: Int = 0,
    val downVotes: Int = 0,
    val currentUserVote: Boolean? = null,
    val closedByUserId: String? = null,
    val closedByName: String? = null,
    val closedAt: ApiInstant? = null,
    val closeNote: String? = null,
) {
    val score: Int get() = upVotes - downVotes

    val priceText: String?
        get() = when {
            priceMin != null && priceMax != null && priceMin != priceMax -> "${priceMin.euro()} – ${priceMax.euro()}"
            priceMin != null -> priceMin.euro()
            priceMax != null -> "bis ${priceMax.euro()}"
            else -> null
        }
}

@Serializable
data class WunschInput(
    val title: String,
    val description: String,
    val link: String?,
    val priceMin: Double?,
    val priceMax: Double?,
)

fun Double.euro(): String = NumberFormat.getCurrencyInstance(Locale.GERMANY).format(this)

// endregion

// region Umfragen

@Serializable(with = UmfrageStatus.Serializer::class)
enum class UmfrageStatus(val raw: String) {
    Offen("Offen"), Geschlossen("Geschlossen"), Unknown("Unknown");

    object Serializer : ResilientEnumSerializer<UmfrageStatus>("UmfrageStatus", entries.toTypedArray(), Unknown, UmfrageStatus::raw)
}

@Serializable
data class UmfrageOption(
    val id: String,
    val text: String,
    val sortOrder: Int = 0,
    val voteCount: Int? = null,
    val voters: List<String>? = null,
)

@Serializable
data class Umfrage(
    val id: String,
    val question: String,
    val description: String? = null,
    val isMultipleChoice: Boolean = false,
    val resultsVisible: Boolean = true,
    val revealAfterClose: Boolean = false,
    val deadline: ApiInstant? = null,
    val lastManualReminderSentAt: ApiInstant? = null,
    val status: UmfrageStatus = UmfrageStatus.Offen,
    val createdByUserId: String,
    val createdByName: String,
    val createdAt: ApiInstant,
    val closedByUserId: String? = null,
    val closedByName: String? = null,
    val closedAt: ApiInstant? = null,
    val options: List<UmfrageOption> = emptyList(),
    val currentUserOptionIds: List<String> = emptyList(),
    val currentUserAbstained: Boolean = false,
    val participantCount: Int = 0,
    val enthaltungCount: Int = 0,
    val linkedEventId: String? = null,
    val linkedCalendarId: String? = null,
    val linkedEventTitle: String? = null,
    val linkedEventStart: ApiInstant? = null,
) {
    val isOpen: Boolean get() = status == UmfrageStatus.Offen
    val hasParticipated: Boolean get() = currentUserOptionIds.isNotEmpty() || currentUserAbstained
    val resultsAvailable: Boolean get() = options.any { it.voteCount != null }
    val sortedOptions: List<UmfrageOption> get() = options.sortedBy { it.sortOrder }
}

@Serializable
data class UmfrageOptionInput(val id: String?, val text: String)

@Serializable
data class UmfrageInput(
    val question: String,
    val description: String?,
    val options: List<UmfrageOptionInput>,
    val isMultipleChoice: Boolean,
    val resultsVisible: Boolean,
    val revealAfterClose: Boolean,
    val deadline: ApiInstant?,
    val linkedEventId: String?,
    val linkedCalendarId: String?,
    val linkedEventTitle: String?,
    val linkedEventStart: ApiInstant?,
)

/** Beim Anlegen erwartet die API die Optionen als einfache Texte. */
@Serializable
data class UmfrageCreateInput(
    val question: String,
    val description: String?,
    val options: List<String>,
    val isMultipleChoice: Boolean,
    val resultsVisible: Boolean,
    val revealAfterClose: Boolean,
    val deadline: ApiInstant?,
    val linkedEventId: String?,
    val linkedCalendarId: String?,
    val linkedEventTitle: String?,
    val linkedEventStart: ApiInstant?,
)

// endregion

// region Aufgaben

@Serializable(with = AufgabeStatus.Serializer::class)
enum class AufgabeStatus(val raw: String, val label: String, val tone: Tone) {
    Neu("Neu", "Neu", Tone.Blue),
    Zugewiesen("Zugewiesen", "Zugewiesen", Tone.Orange),
    Erledigt("Erledigt", "Erledigt", Tone.Green),
    Unknown("Unknown", "Unbekannt", Tone.Gray);

    object Serializer : ResilientEnumSerializer<AufgabeStatus>("AufgabeStatus", entries.toTypedArray(), Unknown, AufgabeStatus::raw)
}

@Serializable
data class Aufgabe(
    val id: String,
    val titel: String,
    val beschreibung: String = "",
    val fotoUrl: String? = null,
    val status: AufgabeStatus = AufgabeStatus.Neu,
    val priority: Int = 2,
    val assignedUserId: String? = null,
    val assignedDisplayName: String? = null,
    val assignedName: String? = null,
    val createdByName: String = "",
    val createdAt: ApiInstant,
) {
    val assignee: String? get() = assignedDisplayName.nonEmpty ?: assignedName.nonEmpty
}

enum class AufgabePriority(val value: Int, val label: String, val shortLabel: String, val tone: Tone) {
    Hoch(1, "Hoch – sofort erledigen", "Hoch", Tone.Red),
    Mittel(2, "Mittel – bald erledigen", "Mittel", Tone.Orange),
    Niedrig(3, "Niedrig – eilt nicht", "Niedrig", Tone.Gray);

    companion object {
        fun of(value: Int): AufgabePriority = entries.firstOrNull { it.value == value } ?: if (value < 1) Hoch else Niedrig
    }
}

@Serializable
data class AufgabeCreateInput(
    val titel: String,
    val beschreibung: String,
    val fotoUrl: String?,
    val priority: Int,
)

@Serializable
data class AufgabeUpdateInput(
    val titel: String,
    val beschreibung: String,
    val fotoUrl: String?,
    val priority: Int,
    val assignedUserId: String?,
    val assignedName: String?,
    val erledigt: Boolean,
)

// endregion
