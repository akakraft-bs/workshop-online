import SwiftUI

nonisolated struct AmtsTraeger: Codable, Hashable, Sendable, Identifiable {
    let role: String
    let roleLabel: String
    let userId: String
    let userName: String
    let phone: String?
    let address: String?

    var id: String { role + userId }
}

nonisolated struct Schluesselhinterlegung: Codable, Identifiable, Hashable, Sendable {
    let id: String
    let name: String
    let address: String
    let phone: String?
    let sortOrder: Int
}

nonisolated struct SchluesselInput: Codable, Sendable {
    var name: String
    var address: String
    var phone: String?
}

nonisolated struct VereinInfo: Codable, Sendable {
    let amtstraeger: [AmtsTraeger]
    let schluessel: [Schluesselhinterlegung]
}

nonisolated struct Dokument: Codable, Identifiable, Hashable, Sendable {
    let id: String
    let folderId: String
    let fileName: String
    let fileUrl: String
    let uploadedByName: String
    let uploadedAt: Date
    let fileSizeBytes: Int?

    var symbol: String {
        switch (fileName as NSString).pathExtension.lowercased() {
        case "pdf": "doc.richtext"
        case "jpg", "jpeg", "png", "heic", "gif", "webp": "photo"
        case "xls", "xlsx", "csv", "numbers": "tablecells"
        case "doc", "docx", "pages", "txt": "doc.text"
        default: "doc"
        }
    }
}

nonisolated struct DokumentOrdner: Codable, Identifiable, Hashable, Sendable {
    let id: String
    let name: String
    let createdAt: Date
    let dokumente: [Dokument]
}

nonisolated struct DokumentInput: Codable, Sendable {
    var folderId: String
    var fileName: String
    var fileUrl: String
    var fileSizeBytes: Int?
}

nonisolated struct FileUploadResult: Codable, Sendable {
    let url: String?
    let fileUrl: String?
    let fileName: String?
    let fileSizeBytes: Int?

    var resolvedURL: String? { url ?? fileUrl }
}

nonisolated struct VereinZugang: Codable, Identifiable, Hashable, Sendable {
    let id: String
    let anbieter: String
    let zugangsdaten: String
}

nonisolated struct VereinZugangInput: Codable, Sendable {
    var anbieter: String
    var zugangsdaten: String
}

// MARK: - Projekte

nonisolated enum ProjektStatus: String, ResilientEnum, CaseIterable, Identifiable {
    case geplant = "Geplant"
    case gestartet = "Gestartet"
    case abgeschlossen = "Abgeschlossen"
    case unknown = "Unknown"

    static let selectable: [ProjektStatus] = [.geplant, .gestartet, .abgeschlossen]
    var id: String { rawValue }
    var label: String { self == .unknown ? "Unbekannt" : rawValue }
    var color: Color {
        switch self {
        case .geplant: .blue
        case .gestartet: .orange
        case .abgeschlossen: .green
        case .unknown: .gray
        }
    }

    var symbol: String {
        switch self {
        case .geplant: "calendar.badge.clock"
        case .gestartet: "hammer.fill"
        case .abgeschlossen: "checkmark.circle.fill"
        case .unknown: "questionmark.circle"
        }
    }
}

nonisolated struct Projekt: Codable, Identifiable, Hashable, Sendable {
    let id: String
    var name: String
    var description: String?
    var plannedStartDate: Date
    var durationWeeks: Int
    var actualStartDate: Date?
    var actualEndDate: Date?
    var expectedEndDate: Date
    var status: ProjektStatus
    var projektplanUrl: String?
    let createdByName: String
    let createdAt: Date
}

nonisolated struct ProjektInput: Codable, Sendable {
    var name: String
    var description: String?
    var plannedStartDate: Date
    var durationWeeks: Int
    var actualStartDate: Date?
    var status: ProjektStatus
    var projektplanUrl: String?
}
