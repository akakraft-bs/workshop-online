import Foundation

nonisolated struct ParkOverview: Codable, Sendable {
    let accounts: [ParkAccountStatus]
    let berechtigung: ParkBerechtigung
    let anstehendeReservierungen: [ParkReservierung]
    let kalenderKonfiguriert: Bool
}

nonisolated struct ParkAccountStatus: Codable, Identifiable, Hashable, Sendable {
    let id: String
    let label: String
    let portalUrl: String?
    let notiz: String?
    let istFrei: Bool
    let belegung: ParkClaim?
    let zugangKonfiguriert: Bool?
    let kennzeichen: [String]?
    let kennzeichenFehler: String?
}

nonisolated struct ParkClaim: Codable, Identifiable, Hashable, Sendable {
    let id: String
    let parkAccountId: String
    let userId: String
    let displayName: String
    let pictureUrl: String?
    let kennzeichen: String
    let fahrzeugBezeichnung: String
    let einfahrtAt: Date
    let voraussichtlichBis: Date?
    let autoExpiresAt: Date
    let berechtigungArt: String
}

nonisolated struct ParkReservierung: Codable, Identifiable, Hashable, Sendable {
    let eventId: String
    let titel: String
    let start: Date?
    let end: Date?
    let istMeine: Bool
    let fahrzeugId: String?
    let fahrzeugLabel: String?

    var id: String { eventId }
}

nonisolated struct ParkBerechtigung: Codable, Hashable, Sendable {
    /// "Automatisch" | "Selbstbestaetigt" | "Spontan"
    let art: String
    let erfordertBestaetigung: Bool
    let hinweis: String
    let waehlbareReservierungen: [ParkReservierung]
    let vorgeschlagenesFahrzeugId: String?
    let vorgeschlagenesFahrzeugLabel: String?
}

nonisolated struct ParkCheckinInput: Codable, Sendable {
    var parkAccountId: String
    var fahrzeugId: String?
    var kennzeichen: String?
    var fahrzeugBezeichnung: String?
    var einfahrtAt: Date?
    var voraussichtlichBis: Date?
    var bestaetigungAkzeptiert: Bool
    var bookingEventId: String?
}

nonisolated struct ParkHistorie: Codable, Identifiable, Hashable, Sendable {
    let id: String
    /// "Belegung" | "KennzeichenHinzugefuegt" | "KennzeichenEntfernt"
    let typ: String
    let zeitpunkt: Date
    let accountLabel: String
    let displayName: String
    let kennzeichen: String?
    let fahrzeugBezeichnung: String?
    let einfahrtAt: Date?
    let freigegebenAt: Date?
    let autoExpiresAt: Date?
    let berechtigungArt: String?
    let bestaetigungHinweis: String?
    let status: String?

    var symbol: String {
        switch typ {
        case "KennzeichenHinzugefuegt": "plus.rectangle"
        case "KennzeichenEntfernt": "minus.rectangle"
        default: "car.fill"
        }
    }

    var title: String {
        switch typ {
        case "KennzeichenHinzugefuegt": "Kennzeichen hinzugefügt"
        case "KennzeichenEntfernt": "Kennzeichen entfernt"
        default: "Belegung"
        }
    }
}

nonisolated struct ParkKennzeichenListe: Codable, Sendable {
    let accountId: String
    let accountLabel: String
    let zugangKonfiguriert: Bool
    let max: Int
    let kennzeichen: [String]
    let fehler: String?
}

nonisolated struct ParkAccountAdmin: Codable, Identifiable, Hashable, Sendable {
    let id: String
    let label: String
    let portalUrl: String?
    let notiz: String?
    let portalUsername: String?
    let zugangKonfiguriert: Bool
    let sortOrder: Int
}

nonisolated struct ParkKennzeichenAudit: Codable, Identifiable, Hashable, Sendable {
    let id: String
    let ausgefuehrtVon: String
    let aktion: String
    let kennzeichen: String
    let kennzeichenNachher: [String]
    let createdAt: Date
}
