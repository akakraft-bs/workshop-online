import SwiftUI

struct StatusPill: View {
    let text: String
    var color: Color = .accentColor
    var systemImage: String?

    var body: some View {
        HStack(spacing: 4) {
            if let systemImage { Image(systemName: systemImage) }
            Text(text)
        }
        .font(.caption.weight(.semibold))
        .padding(.horizontal, 8)
        .padding(.vertical, 3)
        .foregroundStyle(color)
        .background(color.opacity(0.15), in: .capsule)
    }
}

extension Color {
    /// Farben aus dem Backend im Format `#RRGGBB`.
    init?(hex: String?) {
        guard var text = hex?.trimmed, !text.isEmpty else { return nil }
        if text.hasPrefix("#") { text.removeFirst() }
        guard text.count == 6, let value = UInt64(text, radix: 16) else { return nil }
        self.init(
            red: Double((value >> 16) & 0xFF) / 255,
            green: Double((value >> 8) & 0xFF) / 255,
            blue: Double(value & 0xFF) / 255
        )
    }
}

/// Zeile mit Symbol, Titel und Wert für Detailansichten.
struct DetailRow: View {
    let title: String
    let value: String
    var systemImage: String

    var body: some View {
        LabeledContent {
            Text(value)
                .multilineTextAlignment(.trailing)
                .textSelection(.enabled)
        } label: {
            Label(title, systemImage: systemImage)
        }
    }
}
