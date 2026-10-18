import Foundation

/// .NET liefert bis zu 7 Nachkommastellen und teils keine Zeitzone – beides wird hier normalisiert.
nonisolated enum DateParsing {
    static func parse(_ raw: String) -> Date? {
        var text = raw.trimmingCharacters(in: .whitespaces)
        if text.count == 10 { text += "T00:00:00Z" }

        let hasZone = text.hasSuffix("Z") || text.range(of: #"[+-]\d{2}:?\d{2}$"#, options: .regularExpression) != nil
        if !hasZone { text += "Z" }

        if let dot = text.firstIndex(of: ".") {
            let fractionStart = text.index(after: dot)
            let fractionEnd = text[fractionStart...].firstIndex { !$0.isNumber } ?? text.endIndex
            let digits = text[fractionStart..<fractionEnd]
            let normalized = String(digits.prefix(3)).padding(toLength: 3, withPad: "0", startingAt: 0)
            text.replaceSubrange(fractionStart..<fractionEnd, with: normalized)
            return try? Date.ISO8601FormatStyle(includingFractionalSeconds: true).parse(text)
        }
        return try? Date.ISO8601FormatStyle().parse(text)
    }

    static func string(from date: Date) -> String {
        date.formatted(Date.ISO8601FormatStyle(includingFractionalSeconds: true))
    }
}

nonisolated enum JSONCoding {
    static func makeDecoder() -> JSONDecoder {
        let decoder = JSONDecoder()
        decoder.dateDecodingStrategy = .custom { decoder in
            let container = try decoder.singleValueContainer()
            let raw = try container.decode(String.self)
            guard let date = DateParsing.parse(raw) else {
                throw DecodingError.dataCorruptedError(in: container, debugDescription: "Ungültiges Datum: \(raw)")
            }
            return date
        }
        return decoder
    }

    static func makeEncoder() -> JSONEncoder {
        let encoder = JSONEncoder()
        encoder.dateEncodingStrategy = .custom { date, encoder in
            var container = encoder.singleValueContainer()
            try container.encode(DateParsing.string(from: date))
        }
        return encoder
    }
}

/// Enum, das unbekannte Server-Werte nicht als Fehler behandelt.
protocol ResilientEnum: RawRepresentable, Codable, Hashable, Sendable where RawValue == String {
    static var unknown: Self { get }
}

extension ResilientEnum {
    init(from decoder: Decoder) throws {
        let raw = try decoder.singleValueContainer().decode(String.self)
        self = Self(rawValue: raw) ?? .unknown
    }
}
