import Foundation
import Security

nonisolated enum Keychain {
    private static let service = "de.akakraft-bs.app.session"

    static func data(for account: String) -> Data? {
        var query = baseQuery(account)
        query[kSecReturnData as String] = true
        query[kSecMatchLimit as String] = kSecMatchLimitOne
        var result: AnyObject?
        guard SecItemCopyMatching(query as CFDictionary, &result) == errSecSuccess else { return nil }
        return result as? Data
    }

    static func set(_ data: Data, for account: String) {
        let query = baseQuery(account)
        let attributes: [String: Any] = [
            kSecValueData as String: data,
            kSecAttrAccessible as String: kSecAttrAccessibleAfterFirstUnlockThisDeviceOnly,
        ]
        if SecItemUpdate(query as CFDictionary, attributes as CFDictionary) == errSecItemNotFound {
            SecItemAdd(query.merging(attributes) { $1 } as CFDictionary, nil)
        }
    }

    static func remove(account: String) {
        SecItemDelete(baseQuery(account) as CFDictionary)
    }

    private static func baseQuery(_ account: String) -> [String: Any] {
        [
            kSecClass as String: kSecClassGenericPassword,
            kSecAttrService as String: service,
            kSecAttrAccount as String: account,
        ]
    }
}

nonisolated struct Credentials: Codable, Sendable {
    var accessToken: String
    var accessTokenExpiresAt: Date
    var refreshToken: String?

    var isAccessTokenFresh: Bool { accessTokenExpiresAt.timeIntervalSinceNow > 60 }
}

/// Speichert Zugangsdaten getrennt je Server.
final class CredentialStore {
    private let account: String

    init(server: ServerConfiguration) {
        account = "credentials|\(server.id)"
    }

    func load() -> Credentials? {
        Keychain.data(for: account).flatMap { try? JSONDecoder().decode(Credentials.self, from: $0) }
    }

    func save(_ credentials: Credentials) {
        if let data = try? JSONEncoder().encode(credentials) {
            Keychain.set(data, for: account)
        }
    }

    func clear() {
        Keychain.remove(account: account)
    }
}
