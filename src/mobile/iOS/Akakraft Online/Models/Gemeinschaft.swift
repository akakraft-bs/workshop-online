import SwiftUI

// MARK: - Wunschliste

nonisolated enum WunschStatus: String, ResilientEnum {
    case offen = "Offen"
    case angeschafft = "Angeschafft"
    case abgelehnt = "Abgelehnt"
    case unknown = "Unknown"

    var label: String { self == .unknown ? "Unbekannt" : rawValue }
    var color: Color {
        switch self {
        case .offen: .blue
        case .angeschafft: .green
        case .abgelehnt, .unknown: .gray
        }
    }
}

nonisolated struct Wunsch: Codable, Identifiable, Hashable, Sendable {
    let id: String
    var title: String
    var description: String
    var link: String?
    var priceMin: Decimal?
    var priceMax: Decimal?
    var status: WunschStatus
    let createdByUserId: String
    let createdByName: String
    let createdAt: Date
    var upVotes: Int
    var downVotes: Int
    var currentUserVote: Bool?
    var closedByUserId: String?
    var closedByName: String?
    var closedAt: Date?
    var closeNote: String?

    var score: Int { upVotes - downVotes }

    var priceText: String? {
        switch (priceMin, priceMax) {
        case let (min?, max?) where min != max: "\(min.euroString) – \(max.euroString)"
        case let (min?, _): min.euroString
        case let (nil, max?): "bis \(max.euroString)"
        default: nil
        }
    }
}

nonisolated struct WunschInput: Codable, Sendable {
    var title: String
    var description: String
    var link: String?
    var priceMin: Decimal?
    var priceMax: Decimal?
}

// MARK: - Umfragen

nonisolated enum UmfrageStatus: String, ResilientEnum {
    case offen = "Offen"
    case geschlossen = "Geschlossen"
    case unknown = "Unknown"
}

nonisolated struct UmfrageOption: Codable, Identifiable, Hashable, Sendable {
    let id: String
    let text: String
    let sortOrder: Int
    let voteCount: Int?
    let voters: [String]?
}

nonisolated struct Umfrage: Codable, Identifiable, Hashable, Sendable {
    let id: String
    var question: String
    var description: String?
    var isMultipleChoice: Bool
    var resultsVisible: Bool
    var revealAfterClose: Bool
    var deadline: Date?
    var lastManualReminderSentAt: Date?
    var status: UmfrageStatus
    let createdByUserId: String
    let createdByName: String
    let createdAt: Date
    var closedByUserId: String?
    var closedByName: String?
    var closedAt: Date?
    var options: [UmfrageOption]
    var currentUserOptionIds: [String]
    var currentUserAbstained: Bool
    var participantCount: Int
    var enthaltungCount: Int
    var linkedEventId: String?
    var linkedCalendarId: String?
    var linkedEventTitle: String?
    var linkedEventStart: Date?

    var isOpen: Bool { status == .offen }
    var hasParticipated: Bool { !currentUserOptionIds.isEmpty || currentUserAbstained }
    var resultsAvailable: Bool { options.contains { $0.voteCount != nil } }
    var sortedOptions: [UmfrageOption] { options.sorted { $0.sortOrder < $1.sortOrder } }
}

nonisolated struct UmfrageInput: Codable, Sendable {
    nonisolated struct OptionInput: Codable, Sendable {
        var id: String?
        var text: String
    }

    var question: String
    var description: String?
    var options: [OptionInput]
    var isMultipleChoice: Bool
    var resultsVisible: Bool
    var revealAfterClose: Bool
    var deadline: Date?
    var linkedEventId: String?
    var linkedCalendarId: String?
    var linkedEventTitle: String?
    var linkedEventStart: Date?
}

/// Beim Anlegen erwartet die API die Optionen als einfache Texte.
nonisolated struct UmfrageCreateInput: Codable, Sendable {
    var question: String
    var description: String?
    var options: [String]
    var isMultipleChoice: Bool
    var resultsVisible: Bool
    var revealAfterClose: Bool
    var deadline: Date?
    var linkedEventId: String?
    var linkedCalendarId: String?
    var linkedEventTitle: String?
    var linkedEventStart: Date?
}

// MARK: - Aufgaben

nonisolated enum AufgabeStatus: String, ResilientEnum {
    case neu = "Neu"
    case zugewiesen = "Zugewiesen"
    case erledigt = "Erledigt"
    case unknown = "Unknown"

    var label: String { self == .unknown ? "Unbekannt" : rawValue }
    var color: Color {
        switch self {
        case .neu: .blue
        case .zugewiesen: .orange
        case .erledigt: .green
        case .unknown: .gray
        }
    }
}

nonisolated struct Aufgabe: Codable, Identifiable, Hashable, Sendable {
    let id: String
    var titel: String
    var beschreibung: String
    var fotoUrl: String?
    var status: AufgabeStatus
    var priority: Int
    var assignedUserId: String?
    var assignedDisplayName: String?
    var assignedName: String?
    let createdByName: String
    let createdAt: Date

    var assignee: String? { assignedDisplayName.nonEmpty ?? assignedName.nonEmpty }
}

nonisolated enum AufgabePriority: Int, CaseIterable, Identifiable {
    case hoch = 1, mittel = 2, niedrig = 3

    var id: Int { rawValue }
    var label: String {
        switch self {
        case .hoch: "Hoch – sofort erledigen"
        case .mittel: "Mittel – bald erledigen"
        case .niedrig: "Niedrig – eilt nicht"
        }
    }

    var shortLabel: String {
        switch self {
        case .hoch: "Hoch"
        case .mittel: "Mittel"
        case .niedrig: "Niedrig"
        }
    }

    var color: Color {
        switch self {
        case .hoch: .red
        case .mittel: .orange
        case .niedrig: .gray
        }
    }

    init(value: Int) {
        self = AufgabePriority(rawValue: value) ?? (value < 1 ? .hoch : .niedrig)
    }
}

nonisolated struct AufgabeCreateInput: Codable, Sendable {
    var titel: String
    var beschreibung: String
    var fotoUrl: String?
    var priority: Int
}

nonisolated struct AufgabeUpdateInput: Codable, Sendable {
    var titel: String
    var beschreibung: String
    var fotoUrl: String?
    var priority: Int
    var assignedUserId: String?
    var assignedName: String?
    var erledigt: Bool
}
