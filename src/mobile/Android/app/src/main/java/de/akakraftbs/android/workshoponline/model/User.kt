package de.akakraftbs.android.workshoponline.model

import de.akakraftbs.android.workshoponline.core.network.ApiInstant
import de.akakraftbs.android.workshoponline.core.network.ResilientEnumSerializer
import kotlinx.serialization.Serializable

@Serializable(with = RoleSerializer::class)
enum class Role(val raw: String, val label: String) {
    None("None", "Kein Zugriff"),
    Member("Member", "Mitglied"),
    Getraenkewart("Getraenkewart", "Getränkewart"),
    Grillwart("Grillwart", "Grillwart"),
    Hallenwart("Hallenwart", "Hallenwart"),
    Veranstaltungswart("Veranstaltungswart", "Veranstaltungswart"),
    Treasurer("Treasurer", "Kassenwart"),
    ViceChairman("ViceChairman", "2. Vorsitzender"),
    Chairman("Chairman", "1. Vorsitzender"),
    Admin("Admin", "Administrator"),
    Moderator("Moderator", "Moderator"),
    Unknown("Unknown", "Unbekannt");

    companion object {
        val vorstand = setOf(Getraenkewart, Grillwart, Hallenwart, Veranstaltungswart, Treasurer, ViceChairman, Chairman)
        val assignable = entries.filter { it != Unknown && it != None }
    }
}

object RoleSerializer : ResilientEnumSerializer<Role>("Role", Role.entries.toTypedArray(), Role.Unknown, Role::raw)

@Serializable
data class User(
    val id: String,
    val email: String,
    val name: String,
    val displayName: String? = null,
    val pictureUrl: String? = null,
    val createdAt: ApiInstant,
    val roles: List<Role> = emptyList(),
) {
    val shownName: String get() = displayName?.trim()?.takeIf { it.isNotEmpty() } ?: name
}

/** Spiegelt die Policies des Backends (`AnyRole`, `VorstandOrAdmin`, `AdminOnly`). */
data class Permissions(val userId: String?, val roles: Set<Role>) {
    constructor(user: User?) : this(user?.id?.lowercase(), user?.roles?.toSet().orEmpty())

    val isMember: Boolean get() = roles.any { it != Role.None && it != Role.Unknown }
    val isAdmin: Boolean get() = Role.Admin in roles
    val isVorstand: Boolean get() = roles.any { it in Role.vorstand }
    val isModerator: Boolean get() = Role.Moderator in roles

    /** Vorstand, Moderator oder Admin. */
    val isPrivileged: Boolean get() = isAdmin || isVorstand || isModerator
    val canSeeVorstandsbereich: Boolean get() = isVorstand || isAdmin

    fun isOwner(ownerId: String?): Boolean = ownerId != null && userId != null && ownerId.lowercase() == userId

    fun isOwnerOrPrivileged(ownerId: String?): Boolean = isPrivileged || isOwner(ownerId)
}

/** Nutzer ohne Rolle („Kein Zugriff“), die auf Freischaltung warten. */
val List<User>.withoutAccessCount: Int get() = count { !Permissions(it).isMember }

@Serializable
data class UserPreferences(
    val favoriteRoutes: List<String> = emptyList(),
    val displayName: String? = null,
    val phone: String? = null,
    val address: String? = null,
)

@Serializable
data class Badges(
    val pendingUmfragen: Int = 0,
    val openMaengel: Int = 0,
    val lowStock: Int = 0,
    val unseenFeedback: Int = 0,
    val openAufgaben: Int = 0,
)

@Serializable
data class AssignableUser(val id: String, val name: String)
