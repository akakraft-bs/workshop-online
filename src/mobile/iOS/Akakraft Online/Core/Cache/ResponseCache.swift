import Foundation
import SwiftData

/// Eine zwischengespeicherte API-Antwort. Gespeichert wird das rohe JSON, damit
/// neue Felder im Backend keine Datenbank-Migration in der App erfordern.
@Model
final class CachedResponse {
    @Attribute(.unique) var key: String
    var scope: String
    var payload: Data
    var updatedAt: Date

    init(key: String, scope: String, payload: Data, updatedAt: Date = .now) {
        self.key = key
        self.scope = scope
        self.payload = payload
        self.updatedAt = updatedAt
    }
}

final class ResponseCache {
    private let context: ModelContext

    init(container: ModelContainer) {
        context = container.mainContext
    }

    func entry(for key: String) -> CachedResponse? {
        var descriptor = FetchDescriptor<CachedResponse>(predicate: #Predicate { $0.key == key })
        descriptor.fetchLimit = 1
        return try? context.fetch(descriptor).first
    }

    func store(_ payload: Data, key: String, scope: String) {
        if let existing = entry(for: key) {
            existing.payload = payload
            existing.updatedAt = .now
        } else {
            context.insert(CachedResponse(key: key, scope: scope, payload: payload))
        }
        try? context.save()
    }

    func removeAll(scope: String) {
        try? context.delete(model: CachedResponse.self, where: #Predicate { $0.scope == scope })
        try? context.save()
    }

    func removeEverything() {
        try? context.delete(model: CachedResponse.self)
        try? context.save()
    }

    var totalBytes: Int {
        ((try? context.fetch(FetchDescriptor<CachedResponse>())) ?? []).reduce(0) { $0 + $1.payload.count }
    }
}

enum OfflineStore {
    /// Der Cache ist verzichtbar: Kann der Store nicht geöffnet werden, wird er neu angelegt.
    static func makeContainer() -> ModelContainer {
        let schema = Schema([CachedResponse.self])
        let configuration = ModelConfiguration("OfflineCache", schema: schema)
        if let container = try? ModelContainer(for: schema, configurations: [configuration]) {
            return container
        }
        let storeURL = configuration.url
        for suffix in ["", "-shm", "-wal"] {
            try? FileManager.default.removeItem(at: URL(fileURLWithPath: storeURL.path() + suffix))
        }
        if let container = try? ModelContainer(for: schema, configurations: [configuration]) {
            return container
        }
        // swiftlint:disable:next force_try
        return try! ModelContainer(for: schema, configurations: [ModelConfiguration(isStoredInMemoryOnly: true)])
    }
}
