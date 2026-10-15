import Foundation
import Observation

/// Zentraler Einstieg für Anmeldung, Server und Datenzugriff.
///
/// Datenzugriff läuft cache-first: `cached` liefert sofort den letzten Stand,
/// `fetch` lädt online nach und aktualisiert den Cache. Der Cache ist nach
/// Server *und* Nutzer getrennt (`dataScope`).
@Observable
final class AppSession {
    enum State: Equatable {
        case launching
        case signedOut
        case pendingApproval
        case signedIn
    }

    private(set) var state: State = .launching
    private(set) var user: User?
    private(set) var client: APIClient
    private(set) var signOutReason: String?

    let serverStore: ServerStore
    let connectivity: ConnectivityMonitor
    let cache: ResponseCache

    var server: ServerConfiguration { serverStore.current }
    var permissions: Permissions { Permissions(user: user) }

    init(serverStore: ServerStore, connectivity: ConnectivityMonitor, cache: ResponseCache) {
        self.serverStore = serverStore
        self.connectivity = connectivity
        self.cache = cache
        client = APIClient(server: serverStore.current, isOnline: { [connectivity] in connectivity.isOnline })
        client.onSessionExpired = { [weak self] in self?.handleSessionExpired() }
    }

    // MARK: - Lebenszyklus

    func restore() async {
        guard client.hasCredentials else {
            state = .signedOut
            return
        }
        if let entry = cache.entry(for: meKey), let cached = try? client.decode(User.self, from: entry.payload) {
            apply(cached)
        }
        await refreshUser()
        if user == nil { state = .signedOut }
    }

    func login(email: String, password: String) async throws {
        try await client.login(email: email.trimmed, password: password)
        await refreshUser()
        if user == nil { throw APIError.transport("Anmeldung fehlgeschlagen.") }
    }

    func loginWithGoogle(code: String, verifier: String) async throws {
        try await client.exchangeGoogleCode(code, verifier: verifier)
        await refreshUser()
        if user == nil { throw APIError.transport("Anmeldung fehlgeschlagen.") }
    }

    func refreshUser() async {
        do {
            let data = try await client.getData("auth/me")
            let me = try client.decode(User.self, from: data)
            cache.store(data, key: meKey, scope: sessionScope)
            apply(me)
        } catch {
            // Offline oder Serverfehler: zuletzt bekannter Nutzer bleibt aktiv.
        }
    }

    func logout() async {
        await client.logout()
        cache.removeAll(scope: dataScope)
        cache.removeAll(scope: sessionScope)
        ImagePipeline.shared.clear()
        user = nil
        signOutReason = nil
        state = .signedOut
    }

    /// Wechselt den Zielserver. Die Daten des bisherigen Servers bleiben getrennt erhalten.
    func switchServer(to server: ServerConfiguration) async {
        guard server.id != self.server.id else { return }
        await logout()
        serverStore.select(server)
        client = APIClient(server: server, isOnline: { [connectivity] in connectivity.isOnline })
        client.onSessionExpired = { [weak self] in self?.handleSessionExpired() }
        state = .signedOut
    }

    // MARK: - Datenzugriff

    func cached<T: Decodable>(_ type: T.Type, path: String, query: [URLQueryItem] = []) -> (value: T, date: Date)? {
        guard let entry = cache.entry(for: cacheKey(path, query)),
              let value = try? client.decode(type, from: entry.payload)
        else { return nil }
        return (value, entry.updatedAt)
    }

    func fetch<T: Decodable>(_ type: T.Type, path: String, query: [URLQueryItem] = []) async throws -> (value: T, date: Date) {
        let data = try await client.getData(path, query: query)
        let value = try client.decode(type, from: data)
        cache.store(data, key: cacheKey(path, query), scope: dataScope)
        return (value, .now)
    }

    /// Lädt eine Antwort nur in den Cache (Vorladen für die Offline-Nutzung).
    func prefetch(_ path: String, query: [URLQueryItem] = []) async throws {
        let data = try await client.getData(path, query: query)
        cache.store(data, key: cacheKey(path, query), scope: dataScope)
    }

    // MARK: - Intern

    private var sessionScope: String { "\(server.id)|session" }
    private var meKey: String { "\(sessionScope)|auth/me" }
    private var dataScope: String { "\(server.id)|\(user?.id.lowercased() ?? "anonymous")" }

    private func cacheKey(_ path: String, _ query: [URLQueryItem]) -> String {
        let normalized = path.hasPrefix("/") ? String(path.dropFirst()) : path
        let queryString = query.map { "\($0.name)=\($0.value ?? "")" }.joined(separator: "&")
        return "\(dataScope)|\(normalized)?\(queryString)"
    }

    private func apply(_ user: User) {
        self.user = user
        signOutReason = nil
        state = Permissions(user: user).isMember ? .signedIn : .pendingApproval
    }

    private func handleSessionExpired() {
        user = nil
        signOutReason = APIError.unauthorized.localizedDescription
        state = .signedOut
    }
}
