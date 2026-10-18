import SwiftUI

enum WerkzeugSort: String, CaseIterable, Identifiable {
    case name, createdAt, category, storageLocation
    var id: String { rawValue }
    var label: String {
        switch self {
        case .name: "Name"
        case .createdAt: "Neueste zuerst"
        case .category: "Kategorie"
        case .storageLocation: "Ablageort"
        }
    }
}

struct WerkzeugListView: View {
    @Environment(AppSession.self) private var session
    @State private var remote = Remote<[Werkzeug]>("werkzeug")
    @State private var locations = Remote<[StorageLocation]>("storage-locations")
    @State private var searchText = ""
    @State private var category: String?
    @State private var onlyBorrowed = false
    @State private var showCreate = false
    @AppStorage("werkzeug.sort") private var sort: WerkzeugSort = .name

    var body: some View {
        RemoteContent(remote: remote) { items in
            let visible = filtered(items)
            List {
                if visible.isEmpty {
                    emptyState
                } else {
                    ForEach(visible) { item in
                        NavigationLink(value: item) {
                            WerkzeugRow(item: item, locationColor: colorMap.color(for: item.storageLocation))
                        }
                    }
                }
                Section {} footer: { LastUpdatedFooter(date: remote.lastUpdated) }
            }
            .listStyle(.plain)
        }
        .searchable(text: $searchText, prompt: "Werkzeug suchen")
        .refreshable { await reload() }
        .task { await reload() }
        .navigationDestination(for: Werkzeug.self) { item in
            WerkzeugDetailView(werkzeug: item, locationColor: colorMap.color(for: item.storageLocation)) {
                await remote.load(session)
            }
        }
        .toolbar {
            ToolbarItem(placement: .topBarTrailing) { filterMenu }
            if session.permissions.isPrivileged {
                ToolbarItem(placement: .topBarTrailing) {
                    Button("Neues Werkzeug", systemImage: "plus") { showCreate = true }
                        .requiresOnline()
                }
            }
        }
        .sheet(isPresented: $showCreate) {
            WerkzeugFormView(werkzeug: nil, categories: categories, locations: locations.value ?? []) { _ in
                await remote.load(session)
            }
        }
    }

    private var colorMap: [String: Color] { (locations.value ?? []).colorMap }

    private var categories: [String] {
        Array(Set((remote.value ?? []).map(\.category))).sorted()
    }

    private var filterMenu: some View {
        Menu {
            Toggle("Nur ausgeliehene", systemImage: "person.badge.clock", isOn: $onlyBorrowed)
            Picker("Kategorie", selection: $category) {
                Text("Alle Kategorien").tag(String?.none)
                ForEach(categories, id: \.self) { Text($0).tag(Optional($0)) }
            }
            .pickerStyle(.menu)
            Picker("Sortierung", selection: $sort) {
                ForEach(WerkzeugSort.allCases) { Text($0.label).tag($0) }
            }
            .pickerStyle(.menu)
        } label: {
            Label("Filter", systemImage: isFiltering
                  ? "line.3.horizontal.decrease.circle.fill"
                  : "line.3.horizontal.decrease.circle")
        }
    }

    private var isFiltering: Bool { onlyBorrowed || category != nil }

    @ViewBuilder
    private var emptyState: some View {
        if !searchText.isEmpty {
            ContentUnavailableView.search(text: searchText)
        } else {
            ContentUnavailableView("Keine Werkzeuge", systemImage: "wrench.and.screwdriver",
                                   description: Text(isFiltering ? "Für diesen Filter gibt es keine Einträge." : ""))
        }
    }

    private func filtered(_ items: [Werkzeug]) -> [Werkzeug] {
        let query = searchText.trimmed.lowercased()
        return items
            .filter { !onlyBorrowed || !$0.isAvailable }
            .filter { category == nil || $0.category == category }
            .filter {
                query.isEmpty
                    || $0.name.lowercased().contains(query)
                    || $0.description.lowercased().contains(query)
                    || $0.category.lowercased().contains(query)
            }
            .sorted { a, b in
                switch sort {
                case .name:
                    a.name.localizedStandardCompare(b.name) == .orderedAscending
                case .createdAt:
                    a.createdAt > b.createdAt
                case .category:
                    localizedLess((a.category, a.name), (b.category, b.name))
                case .storageLocation:
                    localizedLess((a.storageLocation ?? "", a.name), (b.storageLocation ?? "", b.name))
                }
            }
    }

    private func reload() async {
        async let tools: Void = remote.load(session)
        async let places: Void = locations.load(session)
        _ = await (tools, places)
    }
}

/// Sortiert nach erstem Schlüssel, bei Gleichstand nach zweitem.
func localizedLess(_ lhs: (String, String), _ rhs: (String, String)) -> Bool {
    let first = lhs.0.localizedStandardCompare(rhs.0)
    if first != .orderedSame { return first == .orderedAscending }
    return lhs.1.localizedStandardCompare(rhs.1) == .orderedAscending
}

struct WerkzeugRow: View {
    let item: Werkzeug
    let locationColor: Color?

    var body: some View {
        HStack(spacing: 12) {
            RemoteImage(url: (item.thumbnailUrl ?? item.imageUrl).asURL, placeholderSymbol: "wrench.adjustable")
                .frame(width: 56, height: 56)
                .clipShape(.rect(cornerRadius: 10))

            VStack(alignment: .leading, spacing: 3) {
                Text(item.name).font(.headline).lineLimit(1)
                HStack(spacing: 8) {
                    Text(item.category)
                    if let location = item.storageLocation.nonEmpty {
                        StorageLocationLabel(name: location, color: locationColor)
                    }
                }
                .font(.subheadline)
                .foregroundStyle(.secondary)
                .lineLimit(1)

                if !item.isAvailable {
                    HStack(spacing: 6) {
                        StatusPill(text: item.isOverdue ? "Überfällig" : "Ausgeliehen",
                                   color: item.isOverdue ? .red : .orange)
                        if let name = item.borrowedByName {
                            Text(name).font(.caption).foregroundStyle(.secondary).lineLimit(1)
                        }
                    }
                }
            }
        }
        .padding(.vertical, 2)
        .accessibilityElement(children: .combine)
    }
}
