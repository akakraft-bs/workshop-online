import SwiftUI

struct MangelListView: View {
    enum Filter: String, CaseIterable, Identifiable {
        case aktiv = "Offen"
        case erledigt = "Erledigt"
        case alle = "Alle"
        var id: String { rawValue }
    }

    @Environment(AppSession.self) private var session
    @State private var remote = Remote<[Mangel]>("mangel")
    @State private var filter: Filter = .aktiv
    @State private var kategorie: MangelKategorie?
    @State private var searchText = ""
    @State private var showCreate = false

    var body: some View {
        RemoteContent(remote: remote) { items in
            let visible = filtered(items)
            List {
                Section {
                    Picker("Status", selection: $filter) {
                        ForEach(Filter.allCases) { Text($0.rawValue).tag($0) }
                    }
                    .pickerStyle(.segmented)
                    .listRowBackground(Color.clear)
                    .listRowInsets(EdgeInsets())
                }
                if visible.isEmpty {
                    ContentUnavailableView(
                        searchText.isEmpty ? "Keine Mängel" : "Keine Treffer",
                        systemImage: searchText.isEmpty ? "checkmark.seal" : "magnifyingglass",
                        description: Text(filter == .aktiv && searchText.isEmpty ? "Aktuell ist nichts gemeldet." : "")
                    )
                }
                ForEach(visible) { mangel in
                    NavigationLink(value: mangel) { MangelRow(mangel: mangel) }
                }
                Section {} footer: { LastUpdatedFooter(date: remote.lastUpdated) }
            }
        }
        .searchable(text: $searchText, prompt: "Mängel durchsuchen")
        .refreshable { await remote.load(session) }
        .task { await remote.load(session) }
        .navigationTitle("Mängel")
        .navigationDestination(for: Mangel.self) { mangel in
            MangelDetailView(mangel: mangel) { await remote.load(session) }
        }
        .toolbar {
            ToolbarItem(placement: .topBarTrailing) {
                Menu {
                    Picker("Kategorie", selection: $kategorie) {
                        Text("Alle Kategorien").tag(MangelKategorie?.none)
                        ForEach(MangelKategorie.selectable) { Label($0.label, systemImage: $0.symbol).tag(Optional($0)) }
                    }
                } label: {
                    Label("Kategorie", systemImage: kategorie == nil
                          ? "line.3.horizontal.decrease.circle"
                          : "line.3.horizontal.decrease.circle.fill")
                }
            }
            ToolbarItem(placement: .topBarTrailing) {
                Button("Mangel melden", systemImage: "plus") { showCreate = true }
                    .requiresOnline()
            }
        }
        .sheet(isPresented: $showCreate) {
            MangelFormView(mangel: nil) { _ in await remote.load(session) }
        }
    }

    private func filtered(_ items: [Mangel]) -> [Mangel] {
        let query = searchText.trimmed.lowercased()
        return items
            .filter {
                switch filter {
                case .aktiv: $0.status.isActive
                case .erledigt: !$0.status.isActive
                case .alle: true
                }
            }
            .filter { kategorie == nil || $0.kategorie == kategorie }
            .filter { query.isEmpty || $0.title.lowercased().contains(query) || $0.description.lowercased().contains(query) }
            .sorted { $0.createdAt > $1.createdAt }
    }
}

struct MangelRow: View {
    let mangel: Mangel

    var body: some View {
        HStack(alignment: .top, spacing: 12) {
            Image(systemName: mangel.kategorie.symbol)
                .font(.title3)
                .foregroundStyle(mangel.status.color)
                .frame(width: 36, height: 36)
                .background(mangel.status.color.opacity(0.12), in: .rect(cornerRadius: 9))
            VStack(alignment: .leading, spacing: 4) {
                Text(mangel.title).font(.headline).lineLimit(2)
                HStack(spacing: 6) {
                    StatusPill(text: mangel.status.label, color: mangel.status.color)
                    if !mangel.anmerkungen.isEmpty {
                        Label("\(mangel.anmerkungen.count)", systemImage: "text.bubble")
                            .font(.caption)
                            .foregroundStyle(.secondary)
                    }
                    if mangel.imageUrl != nil {
                        Image(systemName: "photo").font(.caption).foregroundStyle(.secondary)
                    }
                }
                Text("\(mangel.createdByName) · \(mangel.createdAt.relativeString)")
                    .font(.caption)
                    .foregroundStyle(.secondary)
            }
        }
        .padding(.vertical, 2)
        .accessibilityElement(children: .combine)
    }
}
