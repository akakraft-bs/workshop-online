import SwiftUI

nonisolated enum MangelKategorie: String, ResilientEnum, CaseIterable, Identifiable {
    case halle = "Halle"
    case werkzeug = "Werkzeug"
    case sonstiges = "Sonstiges"
    case unknown = "Unknown"

    static let selectable: [MangelKategorie] = [.halle, .werkzeug, .sonstiges]
    var id: String { rawValue }
    var label: String { self == .unknown ? "Unbekannt" : rawValue }
    var symbol: String {
        switch self {
        case .halle: "building.2"
        case .werkzeug: "wrench.adjustable"
        case .sonstiges, .unknown: "questionmark.square.dashed"
        }
    }
}

nonisolated enum MangelStatus: String, ResilientEnum, CaseIterable, Identifiable {
    case offen = "Offen"
    case kenntnisgenommen = "Kenntnisgenommen"
    case behoben = "Behoben"
    case abgelehnt = "Abgelehnt"
    case zurueckgezogen = "Zurueckgezogen"
    case unknown = "Unknown"

    static let settableByManagers: [MangelStatus] = [.offen, .kenntnisgenommen, .behoben, .abgelehnt]
    var id: String { rawValue }

    var label: String {
        switch self {
        case .offen: "Offen"
        case .kenntnisgenommen: "Zur Kenntnis genommen"
        case .behoben: "Behoben"
        case .abgelehnt: "Abgelehnt"
        case .zurueckgezogen: "Zurückgezogen"
        case .unknown: "Unbekannt"
        }
    }

    var color: Color {
        switch self {
        case .offen: .red
        case .kenntnisgenommen: .orange
        case .behoben: .green
        case .abgelehnt, .zurueckgezogen, .unknown: .gray
        }
    }

    var isActive: Bool { self == .offen || self == .kenntnisgenommen }
}

nonisolated struct MangelAnmerkung: Codable, Identifiable, Hashable, Sendable {
    let id: String
    let text: String
    let createdByUserId: String
    let createdByName: String
    let createdAt: Date
    let updatedAt: Date?
}

nonisolated struct Mangel: Codable, Identifiable, Hashable, Sendable {
    let id: String
    var title: String
    var description: String
    var kategorie: MangelKategorie
    var status: MangelStatus
    let createdByUserId: String
    let createdByName: String
    let createdAt: Date
    var imageUrl: String?
    var resolvedByUserId: String?
    var resolvedByName: String?
    var resolvedAt: Date?
    var note: String?
    var anmerkungen: [MangelAnmerkung]
}

nonisolated struct MangelInput: Codable, Sendable {
    var title: String
    var description: String
    var kategorie: MangelKategorie
    var imageUrl: String?
}
