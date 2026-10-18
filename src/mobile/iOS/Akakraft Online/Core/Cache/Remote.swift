import Foundation
import Observation

/// Eine vom Server geladene Ressource mit Offline-Fallback.
///
/// `load` zeigt zuerst den Cache, lädt dann online nach. Offline bleibt der
/// zwischengespeicherte Stand sichtbar; `lastUpdated` nennt dessen Alter.
@Observable
final class Remote<Value: Decodable> {
    private(set) var value: Value?
    private(set) var lastUpdated: Date?
    private(set) var isLoading = false
    private(set) var failure: APIError?
    private(set) var path: String
    private(set) var query: [URLQueryItem]

    init(_ path: String, query: [URLQueryItem] = []) {
        self.path = path
        self.query = query
    }

    func load(_ session: AppSession) async {
        if value == nil, let cached = session.cached(Value.self, path: path, query: query) {
            value = cached.value
            lastUpdated = cached.date
        }
        isLoading = true
        defer { isLoading = false }
        do {
            let fresh = try await session.fetch(Value.self, path: path, query: query)
            value = fresh.value
            lastUpdated = fresh.date
            failure = nil
        } catch {
            let apiError = APIError.from(error)
            failure = (apiError == .offline && value != nil) ? nil : apiError
        }
    }

    /// Wechselt auf eine andere Abfrage (z. B. anderer Monat) und lädt neu.
    func load(_ session: AppSession, path: String, query: [URLQueryItem]) async {
        if path != self.path || query != self.query {
            self.path = path
            self.query = query
            value = nil
            lastUpdated = nil
            failure = nil
        }
        await load(session)
    }

    /// Lokale Anpassung nach erfolgreicher Änderung, bis der nächste Abruf den Serverstand liefert.
    func update(_ transform: (inout Value) -> Void) {
        guard var current = value else { return }
        transform(&current)
        value = current
    }
}

/// Führt schreibende Aktionen aus und stellt Fehler für einen Alert bereit.
@Observable
final class ActionRunner {
    private(set) var isRunning = false
    var errorMessage: String?

    @discardableResult
    func run(_ work: () async throws -> Void) async -> Bool {
        isRunning = true
        defer { isRunning = false }
        do {
            try await work()
            return true
        } catch {
            errorMessage = APIError.from(error).localizedDescription
            return false
        }
    }
}
