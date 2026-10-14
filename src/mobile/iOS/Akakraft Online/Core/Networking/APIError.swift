import Foundation

enum APIError: LocalizedError, Equatable {
    case offline
    case unauthorized
    case forbidden
    case notFound
    case emailNotConfirmed
    case server(status: Int, message: String?)
    case decoding(String)
    case transport(String)

    var errorDescription: String? {
        switch self {
        case .offline:
            "Keine Verbindung. Änderungen sind nur online möglich."
        case .unauthorized:
            "Deine Sitzung ist abgelaufen. Bitte melde dich erneut an."
        case .forbidden:
            "Dafür fehlt dir die Berechtigung."
        case .notFound:
            "Der Eintrag wurde nicht gefunden."
        case .emailNotConfirmed:
            "Bitte bestätige zuerst deine E-Mail-Adresse."
        case .server(let status, let message):
            message ?? "Serverfehler (\(status))."
        case .decoding:
            "Die Antwort des Servers konnte nicht gelesen werden."
        case .transport(let message):
            message
        }
    }

    static func from(_ error: Error) -> APIError {
        if let apiError = error as? APIError { return apiError }
        if let urlError = error as? URLError {
            switch urlError.code {
            case .notConnectedToInternet, .networkConnectionLost, .dataNotAllowed,
                 .internationalRoamingOff, .cannotFindHost, .cannotConnectToHost, .timedOut:
                return .offline
            default:
                return .transport(urlError.localizedDescription)
            }
        }
        if error is DecodingError { return .decoding(String(describing: error)) }
        return .transport(error.localizedDescription)
    }

    /// Liest Fehlermeldungen im Format `"Text"` oder `{ "error": "Text" }`.
    static func message(from data: Data) -> String? {
        guard !data.isEmpty else { return nil }
        if let text = try? JSONDecoder().decode(String.self, from: data), !text.isEmpty { return text }
        if let object = try? JSONSerialization.jsonObject(with: data) as? [String: Any] {
            for key in ["error", "message", "title", "detail"] {
                if let value = object[key] as? String, !value.isEmpty { return value }
            }
        }
        return nil
    }
}
