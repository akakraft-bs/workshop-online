import SwiftUI

struct InventarView: View {
    enum Segment: String, CaseIterable, Identifiable {
        case werkzeug = "Werkzeug"
        case material = "Material"
        var id: String { rawValue }
    }

    @AppStorage("inventar.segment") private var segment: Segment = .werkzeug

    var body: some View {
        Group {
            switch segment {
            case .werkzeug: WerkzeugListView()
            case .material: VerbrauchsmaterialListView()
            }
        }
        .navigationTitle("Inventar")
        // Umschalter unter Titel und Suche statt in der Kopfzeile, damit der Titel sichtbar bleibt.
        .safeAreaBar(edge: .top) {
            Picker("Bereich", selection: $segment) {
                ForEach(Segment.allCases) { Text($0.rawValue).tag($0) }
            }
            .pickerStyle(.segmented)
            .padding(.horizontal)
            .padding(.bottom, 8)
        }
    }
}

/// Farbpunkt + Name des Ablageorts.
struct StorageLocationLabel: View {
    let name: String
    let color: Color?

    var body: some View {
        HStack(spacing: 6) {
            Circle()
                .fill(color ?? .secondary.opacity(0.5))
                .frame(width: 8, height: 8)
            Text(name)
        }
    }
}

extension Array where Element == StorageLocation {
    var colorMap: [String: Color] {
        reduce(into: [:]) { map, location in
            if let color = Color(hex: location.color) { map[location.name.lowercased()] = color }
        }
    }
}

extension Dictionary where Key == String, Value == Color {
    func color(for location: String?) -> Color? {
        location.map { self[$0.lowercased()] } ?? nil
    }
}
