import Foundation

nonisolated enum Role: String, ResilientEnum, CaseIterable {
    case none = "None"
    case member = "Member"
    case getraenkewart = "Getraenkewart"
    case grillwart = "Grillwart"
    case hallenwart = "Hallenwart"
    case veranstaltungswart = "Veranstaltungswart"
    case treasurer = "Treasurer"
    case viceChairman = "ViceChairman"
    case chairman = "Chairman"
    case admin = "Admin"
    case moderator = "Moderator"
    case unknown = "Unknown"

    static let vorstand: Set<Role> = [
        .getraenkewart, .grillwart, .hallenwart, .veranstaltungswart, .treasurer, .viceChairman, .chairman,
    ]

    static let assignable: [Role] = allCases.filter { $0 != .unknown && $0 != .none }

    var label: String {
        switch self {
        case .none: "Kein Zugriff"
        case .member: "Mitglied"
        case .getraenkewart: "Getränkewart"
        case .grillwart: "Grillwart"
        case .hallenwart: "Hallenwart"
        case .veranstaltungswart: "Veranstaltungswart"
        case .treasurer: "Kassenwart"
        case .viceChairman: "2. Vorsitzender"
        case .chairman: "1. Vorsitzender"
        case .admin: "Administrator"
        case .moderator: "Moderator"
        case .unknown: "Unbekannt"
        }
    }
}

nonisolated struct User: Codable, Identifiable, Hashable, Sendable {
    let id: String
    let email: String
    let name: String
    let displayName: String?
    let pictureUrl: String?
    let createdAt: Date
    let roles: [Role]

    var shownName: String { displayName.nonEmpty ?? name }
}

/// Spiegelt die Policies des Backends (`AnyRole`, `VorstandOrAdmin`, `AdminOnly`).
nonisolated struct Permissions: Equatable, Sendable {
    let userId: String?
    let roles: Set<Role>

    init(user: User?) {
        userId = user?.id.lowercased()
        roles = Set(user?.roles ?? [])
    }

    var isMember: Bool { roles.contains { $0 != .none && $0 != .unknown } }
    var isAdmin: Bool { roles.contains(.admin) }
    var isVorstand: Bool { !roles.isDisjoint(with: Role.vorstand) }
    var isModerator: Bool { roles.contains(.moderator) }
    /// Vorstand, Moderator oder Admin.
    var isPrivileged: Bool { isAdmin || isVorstand || isModerator }
    var canSeeVorstandsbereich: Bool { isVorstand || isAdmin }

    func isOwner(_ ownerId: String?) -> Bool {
        guard let ownerId, let userId else { return false }
        return ownerId.lowercased() == userId
    }

    func isOwnerOrPrivileged(_ ownerId: String?) -> Bool {
        isPrivileged || isOwner(ownerId)
    }
}

nonisolated struct UserPreferences: Codable, Sendable {
    var favoriteRoutes: [String]
    var displayName: String?
    var phone: String?
    var address: String?
}

nonisolated struct Badges: Codable, Sendable {
    var pendingUmfragen: Int
    var openMaengel: Int
    var lowStock: Int
    var unseenFeedback: Int
    var openAufgaben: Int
}

nonisolated struct AssignableUser: Codable, Identifiable, Hashable, Sendable {
    let id: String
    let name: String
}
