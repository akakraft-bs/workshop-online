import Foundation

nonisolated struct Werkzeug: Codable, Identifiable, Hashable, Sendable {
    let id: String
    var name: String
    var description: String
    var category: String
    var imageUrl: String?
    var thumbnailUrl: String?
    var dimensions: String?
    var storageLocation: String?
    var isAvailable: Bool
    var borrowedByUserId: String?
    var borrowedByName: String?
    var borrowedAt: Date?
    var expectedReturnAt: Date?
    var returnedAt: Date?
    var createdAt: Date
    var anleitungDokumentId: String?
    var anleitungFileName: String?
    var anleitungFileUrl: String?

    var isOverdue: Bool {
        guard !isAvailable, let expectedReturnAt else { return false }
        return expectedReturnAt < .now
    }
}

nonisolated struct WerkzeugAusleihe: Codable, Identifiable, Hashable, Sendable {
    let id: String
    let userId: String
    let userName: String
    let borrowedAt: Date
    let expectedReturnAt: Date
    let returnedAt: Date?
}

nonisolated struct WerkzeugInput: Codable, Sendable {
    var name: String
    var description: String
    var category: String
    var imageUrl: String?
    var thumbnailUrl: String?
    var dimensions: String?
    var storageLocation: String?
    var anleitungDokumentId: String?
}

nonisolated struct Verbrauchsmaterial: Codable, Identifiable, Hashable, Sendable {
    let id: String
    var name: String
    var description: String
    var category: String
    var unit: String
    var quantity: Int
    var minQuantity: Int?
    var imageUrl: String?
    var thumbnailUrl: String?
    var storageLocation: String?
    var createdAt: Date
    var isNachbestellt: Bool
    var nachbestelltVonName: String?
    var nachbestelltAt: Date?

    var isLowStock: Bool {
        guard let minQuantity else { return false }
        return quantity <= minQuantity
    }
}

nonisolated struct VerbrauchsmaterialInput: Codable, Sendable {
    var name: String
    var description: String
    var category: String
    var unit: String
    var quantity: Int
    var minQuantity: Int?
    var imageUrl: String?
    var thumbnailUrl: String?
    var storageLocation: String?
}

nonisolated struct StorageLocation: Codable, Hashable, Sendable {
    let name: String
    let color: String?
}

nonisolated struct ImageUploadResult: Codable, Sendable {
    let imageUrl: String?
    let thumbnailUrl: String?
    let url: String?

    var primaryURL: String? { imageUrl ?? url }
}
