import Foundation
import Observation

@Observable
final class ServerStore {
    private static let defaultsKey = "server.current"

    private(set) var current: ServerConfiguration

    init(defaults: UserDefaults = .standard) {
        if AppConfig.allowsCustomServer,
           let data = defaults.data(forKey: Self.defaultsKey),
           let stored = try? JSONDecoder().decode(ServerConfiguration.self, from: data) {
            current = stored
        } else {
            current = .standard
        }
    }

    func select(_ server: ServerConfiguration, defaults: UserDefaults = .standard) {
        current = server
        if let data = try? JSONEncoder().encode(server) {
            defaults.set(data, forKey: Self.defaultsKey)
        }
    }
}
