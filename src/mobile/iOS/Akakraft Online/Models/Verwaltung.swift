import SwiftUI

// MARK: - CRM

nonisolated enum PartnerStatus: String, ResilientEnum, CaseIterable, Identifiable {
    case potentiell = "Potentiell"
    case angeschrieben = "Angeschrieben"
    case inVerhandlung = "InVerhandlung"
    case aktiv = "Aktiv"
    case abgelehnt = "Abgelehnt"
    case inaktiv = "Inaktiv"
    case unknown = "Unknown"

    static let selectable: [PartnerStatus] = allCases.filter { $0 != .unknown }
    var id: String { rawValue }

    var label: String {
        switch self {
        case .inVerhandlung: "In Verhandlung"
        case .unknown: "Unbekannt"
        default: rawValue
        }
    }

    var color: Color {
        switch self {
        case .potentiell: .gray
        case .angeschrieben: .blue
        case .inVerhandlung: .orange
        case .aktiv: .green
        case .abgelehnt: .red
        case .inaktiv, .unknown: .secondary
        }
    }
}

nonisolated enum KontaktKanal: String, ResilientEnum, CaseIterable, Identifiable {
    case email = "Email"
    case telefon = "Telefon"
    case meeting = "Meeting"
    case brief = "Brief"
    case sonstiges = "Sonstiges"
    case unknown = "Unknown"

    static let selectable: [KontaktKanal] = allCases.filter { $0 != .unknown }
    var id: String { rawValue }
    var label: String { self == .email ? "E-Mail" : (self == .unknown ? "Unbekannt" : rawValue) }
    var symbol: String {
        switch self {
        case .email: "envelope"
        case .telefon: "phone"
        case .meeting: "person.2"
        case .brief: "envelope.open"
        case .sonstiges, .unknown: "ellipsis.bubble"
        }
    }
}

nonisolated enum KontaktReaktion: String, ResilientEnum, CaseIterable, Identifiable {
    case positiv = "Positiv"
    case neutral = "Neutral"
    case negativ = "Negativ"
    case keineAntwort = "KeineAntwort"
    case unknown = "Unknown"

    static let selectable: [KontaktReaktion] = allCases.filter { $0 != .unknown }
    var id: String { rawValue }
    var label: String {
        switch self {
        case .keineAntwort: "Keine Antwort"
        case .unknown: "Unbekannt"
        default: rawValue
        }
    }

    var color: Color {
        switch self {
        case .positiv: .green
        case .neutral: .blue
        case .negativ: .red
        case .keineAntwort, .unknown: .gray
        }
    }
}

nonisolated struct PartnerOverview: Codable, Identifiable, Hashable, Sendable {
    let id: String
    let name: String
    let kategorie: String?
    let status: PartnerStatus
    let website: String?
    let anzahlKontakte: Int
    let letzterKontakt: Date?
}

nonisolated struct Ansprechpartner: Codable, Identifiable, Hashable, Sendable {
    let id: String
    let name: String
    let position: String?
    let email: String?
    let telefon: String?
    let notizen: String?
}

nonisolated struct Kontakteintrag: Codable, Identifiable, Hashable, Sendable {
    let id: String
    let ansprechpartnerId: String?
    let ansprechpartnerName: String?
    let datum: Date
    let kanal: KontaktKanal
    let reaktion: KontaktReaktion
    let zusammenfassung: String
    let naechsteSchritte: String?
    let erstelltAm: Date
}

nonisolated struct PartnerDetail: Codable, Identifiable, Hashable, Sendable {
    let id: String
    let name: String
    let kategorie: String?
    let status: PartnerStatus
    let website: String?
    let notizen: String?
    let ansprechpartner: [Ansprechpartner]
    let kontakteintraege: [Kontakteintrag]
}

nonisolated struct PartnerInput: Codable, Sendable {
    var name: String
    var kategorie: String?
    var status: PartnerStatus
    var website: String?
    var notizen: String?
}

nonisolated struct AnsprechpartnerInput: Codable, Sendable {
    var name: String
    var position: String?
    var email: String?
    var telefon: String?
    var notizen: String?
}

nonisolated struct KontakteintragInput: Codable, Sendable {
    var ansprechpartnerId: String?
    var datum: Date
    var kanal: KontaktKanal
    var reaktion: KontaktReaktion
    var zusammenfassung: String
    var naechsteSchritte: String?
}

// MARK: - Feedback

nonisolated enum FeedbackStatus: String, ResilientEnum, CaseIterable, Identifiable {
    case new = "New"
    case read = "Read"
    case done = "Done"
    case unknown = "Unknown"

    static let selectable: [FeedbackStatus] = [.new, .read, .done]
    var id: String { rawValue }
    var label: String {
        switch self {
        case .new: "Neu"
        case .read: "Gelesen"
        case .done: "Erledigt"
        case .unknown: "Unbekannt"
        }
    }

    var color: Color {
        switch self {
        case .new: .blue
        case .read: .orange
        case .done: .green
        case .unknown: .gray
        }
    }
}

nonisolated struct Feedback: Codable, Identifiable, Hashable, Sendable {
    let id: String
    let userId: String
    let userName: String
    let text: String
    let pageUrl: String
    let appVersion: String?
    let status: FeedbackStatus
    let createdAt: Date
}

// MARK: - Ablageorte

nonisolated struct AblageortOverview: Codable, Identifiable, Hashable, Sendable {
    let id: String?
    let name: String
    let color: String?
    let itemCount: Int

    var stableId: String { id ?? "name:\(name)" }
}
