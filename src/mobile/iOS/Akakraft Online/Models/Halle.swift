import SwiftUI

nonisolated struct PagedResult<Item: Codable & Sendable>: Codable, Sendable {
    let items: [Item]
    let total: Int
    let page: Int
    let pageSize: Int
}

// MARK: - Hallenbuch

nonisolated enum GastschraubenArt: String, ResilientEnum, CaseIterable, Identifiable {
    case kastenPremiumbier = "KastenPremiumbier"
    case zwanzigEuroPayPal = "ZwanzigEuroPayPal"
    case unknown = "Unknown"

    static let selectable: [GastschraubenArt] = [.kastenPremiumbier, .zwanzigEuroPayPal]
    var id: String { rawValue }
    var label: String {
        switch self {
        case .kastenPremiumbier: "Kasten Premiumbier"
        case .zwanzigEuroPayPal: "20 € per PayPal"
        case .unknown: "Unbekannt"
        }
    }
}

nonisolated struct HallenbuchEintrag: Codable, Identifiable, Hashable, Sendable {
    let id: String
    let userId: String
    let userName: String
    var start: Date
    var end: Date
    var description: String
    var hatGastgeschraubt: Bool
    var gastschraubenArt: GastschraubenArt?
    var gastschraubenBezahlt: Bool?
    var hatFamiliegeschraubt: Bool
    let createdAt: Date
    var fahrzeugId: String?
    var fahrzeugLabel: String?

    var duration: TimeInterval { end.timeIntervalSince(start) }
}

nonisolated struct HallenbuchInput: Codable, Sendable {
    var start: Date
    var end: Date
    var description: String
    var hatGastgeschraubt: Bool
    var gastschraubenArt: GastschraubenArt?
    var gastschraubenBezahlt: Bool?
    var hatFamiliegeschraubt: Bool
    var fahrzeugId: String?
}

nonisolated struct HallenbuchStatistik: Codable, Identifiable, Hashable, Sendable {
    let userId: String
    let userName: String
    let eigeneStunden: Double
    let gastStunden: Double
    let familieStunden: Double

    var id: String { userId }
    var gesamt: Double { eigeneStunden + gastStunden + familieStunden }
}

// MARK: - Hallen-Check-in & MOTD

nonisolated struct HallenCheck: Codable, Identifiable, Hashable, Sendable {
    let id: String
    let userId: String
    let displayName: String
    let pictureUrl: String?
    let message: String?
    let checkedInAt: Date
    let expiresAt: Date
}

nonisolated enum MotdSeverity: String, ResilientEnum, CaseIterable, Identifiable {
    case info = "Info"
    case warning = "Warning"
    case critical = "Critical"
    case unknown = "Unknown"

    static let selectable: [MotdSeverity] = [.info, .warning, .critical]
    var id: String { rawValue }
    var label: String {
        switch self {
        case .info, .unknown: "Info"
        case .warning: "Warnung"
        case .critical: "Kritisch"
        }
    }

    var color: Color {
        switch self {
        case .info, .unknown: .blue
        case .warning: .orange
        case .critical: .red
        }
    }

    var symbol: String {
        switch self {
        case .info, .unknown: "info.circle.fill"
        case .warning: "exclamationmark.triangle.fill"
        case .critical: "exclamationmark.octagon.fill"
        }
    }
}

nonisolated struct Motd: Codable, Hashable, Sendable {
    let id: String
    let message: String
    let severity: MotdSeverity
    let updatedAt: Date
}

// MARK: - Kalender

nonisolated enum CalendarKind: String, CaseIterable, Identifiable, Sendable {
    case hallenbelegung = "Hallenbelegung"
    case veranstaltungen = "Veranstaltungen"

    var id: String { rawValue }
    var title: String { rawValue }
    var symbol: String { self == .hallenbelegung ? "calendar" : "party.popper" }
}

nonisolated struct CalendarConfig: Codable, Identifiable, Hashable, Sendable {
    let id: String
    let googleCalendarId: String
    let name: String
    let color: String
    let isVisible: Bool
    let sortOrder: Int
    let calendarType: String
    let grantsParkplatzBerechtigung: Bool
    let writeRoles: [String]

    /// Entspricht `HasWriteAccess` im Backend.
    func canWrite(_ permissions: Permissions) -> Bool {
        permissions.isPrivileged || writeRoles.contains { role in permissions.roles.contains { $0.rawValue == role } }
    }
}

nonisolated struct CalendarEvent: Codable, Identifiable, Hashable, Sendable {
    let id: String
    let calendarId: String
    let calendarName: String
    let calendarColor: String
    let title: String
    let start: Date?
    let end: Date?
    let isAllDay: Bool
    let creatorName: String?
    let creatorEmail: String?
    let creatorUserId: String?
    let description: String?
    let location: String?
    let url: String?
    let fahrzeugId: String?
    let fahrzeugLabel: String?

    var timeText: String {
        guard let start else { return "" }
        if isAllDay { return "Ganztägig" }
        guard let end else { return start.timeString }
        return "\(start.timeString) – \(end.timeString)"
    }
}

nonisolated struct CalendarEventInput: Codable, Sendable {
    var calendarId: String?
    var title: String
    var start: Date
    var end: Date
    var isAllDay: Bool
    var description: String?
    var location: String?
    var url: String?
    var fahrzeugId: String?
}

// MARK: - Fahrzeuge

nonisolated struct Fahrzeug: Codable, Identifiable, Hashable, Sendable {
    let id: String
    var marke: String
    var modell: String?
    var kennzeichen: String
    var istStandard: Bool

    var label: String {
        [marke, modell.nonEmpty].compactMap { $0 }.joined(separator: " ") + " · " + kennzeichen
    }
}

nonisolated struct FahrzeugInput: Codable, Sendable {
    var marke: String
    var modell: String?
    var kennzeichen: String
    var istStandard: Bool
}
