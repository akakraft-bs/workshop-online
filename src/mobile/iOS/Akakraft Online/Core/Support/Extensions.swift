import Foundation

extension Optional where Wrapped == String {
    /// `nil` für fehlende oder leere/whitespace-Strings.
    nonisolated var nonEmpty: String? {
        guard let trimmed = self?.trimmingCharacters(in: .whitespacesAndNewlines), !trimmed.isEmpty else { return nil }
        return trimmed
    }

    nonisolated var asURL: URL? { nonEmpty.flatMap(URL.init(string:)) }
}

extension String {
    nonisolated var nonEmpty: String? { Optional(self).nonEmpty }
    nonisolated var trimmed: String { trimmingCharacters(in: .whitespacesAndNewlines) }
    nonisolated var asURL: URL? { Optional(self).asURL }
}

extension Date {
    nonisolated var dayString: String { formatted(.dateTime.day(.twoDigits).month(.twoDigits).year()) }
    nonisolated var dayTimeString: String { formatted(.dateTime.day(.twoDigits).month(.twoDigits).year().hour().minute()) }
    nonisolated var timeString: String { formatted(.dateTime.hour().minute()) }
    nonisolated var relativeString: String { formatted(.relative(presentation: .named)) }

    nonisolated var startOfDay: Date { Calendar.current.startOfDay(for: self) }
}

extension Int {
    nonisolated var byteString: String { ByteCountFormatter.string(fromByteCount: Int64(self), countStyle: .file) }
}

extension Decimal {
    nonisolated var euroString: String { formatted(.currency(code: "EUR")) }
}
