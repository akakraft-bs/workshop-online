import Foundation

/// Zielserver der App. Alles Server-spezifische (Tokens, Cache) wird über `id` getrennt abgelegt,
/// damit später – wie bei Nextcloud – mehrere Server bzw. ein Wechsel möglich sind.
nonisolated struct ServerConfiguration: Codable, Hashable, Identifiable, Sendable {
    var baseURL: URL
    var apiPath: String = "/api"
    var webAppPath: String = "/app"

    /// Link in die Web-App, z. B. für Admin-Funktionen, die es nur dort gibt.
    func webURL(route: String) -> URL {
        let path = (webAppPath + "/" + route).replacingOccurrences(of: "//", with: "/")
        return baseURL.appending(path: path.trimmingCharacters(in: CharacterSet(charactersIn: "/")))
    }

    var id: String {
        var components = URLComponents(url: baseURL, resolvingAgainstBaseURL: false)
        components?.path = ""
        return (components?.string ?? baseURL.absoluteString).lowercased()
    }

    var displayName: String {
        guard let host = baseURL.host() else { return baseURL.absoluteString }
        return baseURL.port.map { "\(host):\($0)" } ?? host
    }

    var apiBaseURL: URL {
        apiPath.isEmpty ? baseURL : baseURL.appending(path: apiPath.trimmingCharacters(in: CharacterSet(charactersIn: "/")))
    }

    static let akakraft = ServerConfiguration(baseURL: URL(string: "https://app.akakraft-bs.de")!)

    /// Lokales Backend (`dotnet run`). Ohne `/api`, wie das Web-Frontend lokal –
    /// sonst stimmt die bei Google hinterlegte Rücksprungadresse nicht.
    static let local = ServerConfiguration(baseURL: URL(string: "http://localhost:5153")!, apiPath: "", webAppPath: "")

    /// Debug-Builds aus Xcode nutzen das lokale Backend, Release-Builds den Vereinsserver.
    static var standard: ServerConfiguration {
        #if DEBUG
        .local
        #else
        .akakraft
        #endif
    }

    var isLocal: Bool { ["localhost", "127.0.0.1"].contains(baseURL.host()?.lowercased() ?? "") }

    /// Erzeugt eine Konfiguration aus einer Nutzereingabe wie `app.akakraft-bs.de` oder `https://host:8443`.
    static func fromUserInput(_ input: String) -> ServerConfiguration? {
        var text = input.trimmingCharacters(in: .whitespacesAndNewlines)
        guard !text.isEmpty else { return nil }
        if !text.contains("://") { text = "https://" + text }
        guard var components = URLComponents(string: text),
              let scheme = components.scheme?.lowercased(), ["https", "http"].contains(scheme),
              components.host?.isEmpty == false
        else { return nil }
        components.path = ""
        components.query = nil
        components.fragment = nil
        guard let url = components.url else { return nil }
        let server = ServerConfiguration(baseURL: url)
        return server.isLocal ? ServerConfiguration(baseURL: url, apiPath: "", webAppPath: "") : server
    }
}

enum AppConfig {
    /// Schaltet die Server-Auswahl im Login und in den Einstellungen frei (in Debug-Builds immer an).
    static let allowsCustomServer: Bool = {
        #if DEBUG
        true
        #else
        false
        #endif
    }()
}
