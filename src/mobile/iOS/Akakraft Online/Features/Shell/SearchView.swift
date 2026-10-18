import SwiftUI

/// Übergreifende Suche über Inventar, Mängel und Dokumente – funktioniert auch offline auf dem Cache.
struct SearchView: View {
    @Environment(AppSession.self) private var session
    @State private var query = ""
    @State private var werkzeug = Remote<[Werkzeug]>("werkzeug")
    @State private var material = Remote<[Verbrauchsmaterial]>("verbrauchsmaterial")
    @State private var maengel = Remote<[Mangel]>("mangel")
    @State private var dokumente = Remote<[DokumentOrdner]>("verein/dokumente")
    @Environment(\.openURL) private var openURL

    var body: some View {
        List {
            let text = query.trimmed
            if text.isEmpty {
                ContentUnavailableView("Suchen", systemImage: "magnifyingglass",
                                       description: Text("Finde Werkzeug, Material, Mängel und Dokumente."))
            } else {
                let tools = (werkzeug.value ?? []).filter { matches($0.name, $0.description, $0.category, $0.storageLocation) }
                let materials = (material.value ?? []).filter { matches($0.name, $0.description, $0.category, $0.storageLocation) }
                let defects = (maengel.value ?? []).filter { matches($0.title, $0.description) }
                let docs = (dokumente.value ?? []).flatMap(\.dokumente).filter { matches($0.fileName) }

                if tools.isEmpty && materials.isEmpty && defects.isEmpty && docs.isEmpty {
                    ContentUnavailableView.search(text: text)
                }
                if !tools.isEmpty {
                    Section("Werkzeug") {
                        ForEach(tools) { item in
                            NavigationLink(value: item) { WerkzeugRow(item: item, locationColor: nil) }
                        }
                    }
                }
                if !materials.isEmpty {
                    Section("Verbrauchsmaterial") {
                        ForEach(materials) { item in
                            NavigationLink(value: item) { MaterialRow(item: item, locationColor: nil) }
                        }
                    }
                }
                if !defects.isEmpty {
                    Section("Mängel") {
                        ForEach(defects) { mangel in
                            NavigationLink(value: mangel) { MangelRow(mangel: mangel) }
                        }
                    }
                }
                if !docs.isEmpty {
                    Section("Dokumente") {
                        ForEach(docs) { dokument in
                            Button {
                                if let url = dokument.fileUrl.asURL { openURL(url) }
                            } label: {
                                Label(dokument.fileName, systemImage: dokument.symbol)
                            }
                        }
                    }
                }
            }
        }
        .navigationTitle("Suche")
        // Suchfeld immer sichtbar direkt unter dem Titel statt erst beim Wischen.
        .searchable(text: $query, placement: .navigationBarDrawer(displayMode: .always), prompt: "Werkzeug, Material, Mängel …")
        .task {
            await withTaskGroup(of: Void.self) { group in
                group.addTask { await werkzeug.load(session) }
                group.addTask { await material.load(session) }
                group.addTask { await maengel.load(session) }
                group.addTask { await dokumente.load(session) }
            }
        }
        .navigationDestination(for: Werkzeug.self) { item in
            WerkzeugDetailView(werkzeug: item, locationColor: nil) { await werkzeug.load(session) }
        }
        .navigationDestination(for: Verbrauchsmaterial.self) { item in
            VerbrauchsmaterialDetailView(item: item, locationColor: nil) { await material.load(session) }
        }
        .navigationDestination(for: Mangel.self) { mangel in
            MangelDetailView(mangel: mangel) { await maengel.load(session) }
        }
    }

    private func matches(_ fields: String?...) -> Bool {
        let needle = query.trimmed
        return fields.contains { $0?.localizedCaseInsensitiveContains(needle) == true }
    }
}
