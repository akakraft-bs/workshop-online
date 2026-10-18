import Foundation

enum HTTPMethod: String {
    case get = "GET", post = "POST", put = "PUT", patch = "PATCH", delete = "DELETE"
}

/// HTTP-Client für genau einen Server.
///
/// - Lesende Requests (`getData`) und schreibende Requests (`send`, `upload`) sind getrennt.
///   Schreibende Requests werden offline grundsätzlich abgelehnt – die App ist offline read-only.
/// - Das Refresh-Token kommt vom Server als HttpOnly-Cookie. Es wird hier aus `Set-Cookie`
///   gelesen, im Keychain gespeichert und beim Erneuern explizit mitgeschickt.
final class APIClient {
    let server: ServerConfiguration
    var onSessionExpired: (() -> Void)?

    private let credentialStore: CredentialStore
    private(set) var credentials: Credentials?
    private let session: URLSession
    private let decoder = JSONCoding.makeDecoder()
    private let encoder = JSONCoding.makeEncoder()
    private let isOnline: () -> Bool
    private var refreshTask: Task<Void, Error>?

    init(server: ServerConfiguration, isOnline: @escaping () -> Bool) {
        self.server = server
        self.isOnline = isOnline
        credentialStore = CredentialStore(server: server)
        credentials = credentialStore.load()

        let configuration = URLSessionConfiguration.default
        configuration.httpCookieAcceptPolicy = .never
        configuration.httpShouldSetCookies = false
        configuration.httpCookieStorage = nil
        configuration.urlCache = nil
        configuration.requestCachePolicy = .reloadIgnoringLocalCacheData
        configuration.timeoutIntervalForRequest = 20
        session = URLSession(configuration: configuration)
    }

    var hasCredentials: Bool { credentials != nil }

    // MARK: - Lesen

    func getData(_ path: String, query: [URLQueryItem] = []) async throws -> Data {
        guard isOnline() else { throw APIError.offline }
        return try await perform(.get, path, query: query)
    }

    func get<T: Decodable>(_ type: T.Type, _ path: String, query: [URLQueryItem] = []) async throws -> T {
        try decode(type, from: try await getData(path, query: query))
    }

    // MARK: - Schreiben (nur online)

    @discardableResult
    func send(_ method: HTTPMethod, _ path: String, query: [URLQueryItem] = [], body: (any Encodable)? = nil) async throws -> Data {
        guard isOnline() else { throw APIError.offline }
        let payload: Data? = if let body { try encoder.encode(body) } else if method == .delete { nil } else { Data("{}".utf8) }
        return try await perform(method, path, query: query, body: payload, contentType: "application/json")
    }

    func send<T: Decodable>(_ method: HTTPMethod, _ path: String, body: (any Encodable)? = nil, decoding type: T.Type) async throws -> T {
        try decode(type, from: try await send(method, path, body: body))
    }

    func upload(_ path: String, fileData: Data, fileName: String, mimeType: String) async throws -> Data {
        guard isOnline() else { throw APIError.offline }
        let boundary = "Boundary-\(UUID().uuidString)"
        var body = Data()
        body.append(Data("--\(boundary)\r\n".utf8))
        body.append(Data("Content-Disposition: form-data; name=\"file\"; filename=\"\(fileName)\"\r\n".utf8))
        body.append(Data("Content-Type: \(mimeType)\r\n\r\n".utf8))
        body.append(fileData)
        body.append(Data("\r\n--\(boundary)--\r\n".utf8))
        return try await perform(.post, path, body: body, contentType: "multipart/form-data; boundary=\(boundary)")
    }

    // MARK: - Öffentliche Auth-Endpunkte

    func sendPublic(_ method: HTTPMethod, _ path: String, body: (any Encodable)?) async throws -> Data {
        guard isOnline() else { throw APIError.offline }
        let payload = try body.map { try encoder.encode($0) }
        return try await perform(method, path, body: payload, contentType: "application/json", authenticated: false)
    }

    func login(email: String, password: String) async throws {
        guard isOnline() else { throw APIError.offline }
        let body = try encoder.encode(LoginBody(email: email, password: password))
        let (data, response) = try await rawRequest(.post, "auth/login", body: body, contentType: "application/json")
        switch response.statusCode {
        case 200:
            try storeTokens(from: data, response: response, previousRefreshToken: nil)
        case 401, 400:
            throw APIError.server(status: response.statusCode, message: "E-Mail oder Passwort ist falsch.")
        case 403:
            throw APIError.emailNotConfirmed
        default:
            throw APIError.server(status: response.statusCode, message: APIError.message(from: data))
        }
    }

    /// Startadresse für den Google-Login im System-Browser.
    func googleLoginURL(challenge: String) -> URL {
        url(for: "auth/login/google", query: [
            URLQueryItem(name: "client", value: "ios"),
            URLQueryItem(name: "code_challenge", value: challenge),
        ])
    }

    /// Tauscht den Einmal-Code aus dem Google-Callback gegen Token und Refresh-Token.
    func exchangeGoogleCode(_ code: String, verifier: String) async throws {
        guard isOnline() else { throw APIError.offline }
        let body = try encoder.encode(GoogleExchangeBody(code: code, codeVerifier: verifier))
        let (data, response) = try await rawRequest(.post, "auth/google/exchange", body: body, contentType: "application/json")
        guard response.statusCode == 200 else {
            throw APIError.server(status: response.statusCode, message: "Google-Anmeldung fehlgeschlagen. Bitte versuche es erneut.")
        }
        try storeTokens(from: data, response: response, previousRefreshToken: nil)
    }

    func logout() async {
        if isOnline(), let refreshToken = credentials?.refreshToken {
            var request = try? makeRequest(.post, "auth/logout", query: [], body: nil, contentType: nil, authenticated: true)
            request?.setValue("refresh_token=\(refreshToken)", forHTTPHeaderField: "Cookie")
            if let request { _ = try? await execute(request) }
        }
        clearCredentials()
    }

    func clearCredentials() {
        credentials = nil
        credentialStore.clear()
    }

    func url(for path: String, query: [URLQueryItem] = []) -> URL {
        let trimmed = path.hasPrefix("/") ? String(path.dropFirst()) : path
        var url = server.apiBaseURL.appending(path: trimmed)
        if !query.isEmpty { url.append(queryItems: query) }
        return url
    }

    func decode<T: Decodable>(_ type: T.Type, from data: Data) throws -> T {
        do { return try decoder.decode(type, from: data) }
        catch { throw APIError.decoding(String(describing: error)) }
    }

    // MARK: - Intern

    private func perform(
        _ method: HTTPMethod, _ path: String, query: [URLQueryItem] = [],
        body: Data? = nil, contentType: String? = nil,
        authenticated: Bool = true, isRetry: Bool = false
    ) async throws -> Data {
        if authenticated, let credentials, !credentials.isAccessTokenFresh, credentials.refreshToken != nil {
            try await refresh()
        }

        let request = try makeRequest(method, path, query: query, body: body, contentType: contentType, authenticated: authenticated)
        let (data, response) = try await execute(request)

        switch response.statusCode {
        case 200..<300:
            // 204 / leerer Body (z. B. keine MOTD) wird als JSON-null dekodierbar.
            return data.isEmpty ? Data("null".utf8) : data
        case 401 where authenticated && !isRetry && credentials?.refreshToken != nil:
            try await refresh()
            return try await perform(method, path, query: query, body: body, contentType: contentType, authenticated: true, isRetry: true)
        case 401:
            if authenticated { expireSession() }
            throw APIError.unauthorized
        case 403:
            throw APIError.forbidden
        case 404:
            throw APIError.notFound
        default:
            throw APIError.server(status: response.statusCode, message: APIError.message(from: data))
        }
    }

    private func refresh() async throws {
        if let refreshTask { return try await refreshTask.value }
        let task = Task { try await performRefresh() }
        refreshTask = task
        defer { refreshTask = nil }
        try await task.value
    }

    private func performRefresh() async throws {
        guard let refreshToken = credentials?.refreshToken else {
            expireSession()
            throw APIError.unauthorized
        }
        var request = try makeRequest(.post, "auth/refresh", query: [], body: nil, contentType: nil, authenticated: false)
        request.setValue("refresh_token=\(refreshToken)", forHTTPHeaderField: "Cookie")
        let (data, response) = try await execute(request)
        guard response.statusCode == 200 else {
            if response.statusCode == 401 { expireSession() }
            throw APIError.unauthorized
        }
        try storeTokens(from: data, response: response, previousRefreshToken: refreshToken)
    }

    private func storeTokens(from data: Data, response: HTTPURLResponse, previousRefreshToken: String?) throws {
        let token = try decode(TokenResponse.self, from: data)
        let updated = Credentials(
            accessToken: token.token,
            accessTokenExpiresAt: token.expiresAt,
            refreshToken: refreshTokenCookie(in: response) ?? previousRefreshToken
        )
        credentials = updated
        credentialStore.save(updated)
    }

    private func expireSession() {
        guard credentials != nil else { return }
        clearCredentials()
        onSessionExpired?()
    }

    private func rawRequest(_ method: HTTPMethod, _ path: String, body: Data?, contentType: String?) async throws -> (Data, HTTPURLResponse) {
        try await execute(try makeRequest(method, path, query: [], body: body, contentType: contentType, authenticated: false))
    }

    private func makeRequest(
        _ method: HTTPMethod, _ path: String, query: [URLQueryItem],
        body: Data?, contentType: String?, authenticated: Bool
    ) throws -> URLRequest {
        var request = URLRequest(url: url(for: path, query: query))
        request.httpMethod = method.rawValue
        request.httpBody = body
        request.setValue("application/json", forHTTPHeaderField: "Accept")
        if let contentType, body != nil { request.setValue(contentType, forHTTPHeaderField: "Content-Type") }
        if authenticated, let token = credentials?.accessToken {
            request.setValue("Bearer \(token)", forHTTPHeaderField: "Authorization")
        }
        return request
    }

    private func execute(_ request: URLRequest) async throws -> (Data, HTTPURLResponse) {
        do {
            let (data, response) = try await session.data(for: request)
            guard let http = response as? HTTPURLResponse else { throw APIError.transport("Ungültige Serverantwort.") }
            return (data, http)
        } catch {
            throw APIError.from(error)
        }
    }

    private func refreshTokenCookie(in response: HTTPURLResponse) -> String? {
        guard let url = response.url else { return nil }
        var headers: [String: String] = [:]
        for (key, value) in response.allHeaderFields {
            if let key = key as? String, let value = value as? String { headers[key] = value }
        }
        return HTTPCookie.cookies(withResponseHeaderFields: headers, for: url)
            .first { $0.name == "refresh_token" }?.value
    }
}

nonisolated private struct LoginBody: Encodable {
    let email: String
    let password: String
}

nonisolated private struct GoogleExchangeBody: Encodable {
    let code: String
    let codeVerifier: String
}

nonisolated private struct TokenResponse: Decodable {
    let token: String
    let expiresAt: Date
}
