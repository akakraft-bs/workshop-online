import SwiftUI

struct HallenbuchListView: View {
    private static let pageSize = 30

    @Environment(AppSession.self) private var session
    @State private var firstPage = Remote<PagedResult<HallenbuchEintrag>>(
        "hallenbuch", query: [URLQueryItem(name: "page", value: "0"), URLQueryItem(name: "pageSize", value: "\(pageSize)")]
    )
    @State private var morePages: [HallenbuchEintrag] = []
    @State private var nextPage = 1
    @State private var isLoadingMore = false
    @State private var editing: HallenbuchEintrag?
    @State private var showCreate = false
    @State private var action = ActionRunner()

    var body: some View {
        RemoteContent(remote: firstPage) { page in
            let entries = page.items + morePages
            List {
                if entries.isEmpty {
                    ContentUnavailableView("Noch keine Einträge", systemImage: "book.closed",
                                           description: Text("Trag ein, wann du in der Halle geschraubt hast."))
                }
                ForEach(entries) { eintrag in
                    HallenbuchRow(eintrag: eintrag)
                        .contentShape(.rect)
                        .onTapGesture { if canEdit(eintrag) { editing = eintrag } }
                        .swipeActions {
                            if canEdit(eintrag) {
                                Button("Löschen", systemImage: "trash", role: .destructive) {
                                    Task { await delete(eintrag) }
                                }
                                Button("Bearbeiten", systemImage: "pencil") { editing = eintrag }
                                    .tint(.blue)
                            }
                        }
                }
                if entries.count < page.total {
                    Button {
                        Task { await loadMore() }
                    } label: {
                        HStack {
                            Text("Weitere laden (\(page.total - entries.count))")
                            if isLoadingMore { Spacer(); ProgressView() }
                        }
                    }
                    .disabled(isLoadingMore)
                    .requiresOnline()
                }
                Section {} footer: { LastUpdatedFooter(date: firstPage.lastUpdated) }
            }
        }
        .refreshable { await reload() }
        .task { await firstPage.load(session) }
        .navigationTitle("Hallenbuch")
        .toolbar {
            if session.permissions.isPrivileged {
                ToolbarItem(placement: .topBarTrailing) {
                    NavigationLink {
                        HallenbuchStatistikView()
                    } label: {
                        Label("Statistik", systemImage: "chart.bar")
                    }
                }
            }
            ToolbarItem(placement: .topBarTrailing) {
                Button("Eintrag hinzufügen", systemImage: "plus") { showCreate = true }
                    .requiresOnline()
            }
        }
        .sheet(isPresented: $showCreate) {
            HallenbuchFormView(eintrag: nil) { await reload() }
        }
        .sheet(item: $editing) { eintrag in
            HallenbuchFormView(eintrag: eintrag) { await reload() }
        }
        .actionAlert(action)
    }

    /// Mitglieder: eigene Einträge innerhalb von 7 Tagen. Vorstand: alle.
    private func canEdit(_ eintrag: HallenbuchEintrag) -> Bool {
        if session.permissions.isPrivileged { return true }
        return session.permissions.isOwner(eintrag.userId) && Date.now.timeIntervalSince(eintrag.createdAt) < 7 * 86_400
    }

    private func reload() async {
        morePages = []
        nextPage = 1
        await firstPage.load(session)
    }

    private func loadMore() async {
        isLoadingMore = true
        defer { isLoadingMore = false }
        let query = [URLQueryItem(name: "page", value: "\(nextPage)"), URLQueryItem(name: "pageSize", value: "\(Self.pageSize)")]
        if let page = try? await session.client.get(PagedResult<HallenbuchEintrag>.self, "hallenbuch", query: query) {
            morePages += page.items
            nextPage += 1
        }
    }

    private func delete(_ eintrag: HallenbuchEintrag) async {
        if await action.run({ try await session.client.send(.delete, "hallenbuch/\(eintrag.id)") }) {
            await reload()
        }
    }
}

struct HallenbuchRow: View {
    let eintrag: HallenbuchEintrag

    var body: some View {
        VStack(alignment: .leading, spacing: 4) {
            HStack {
                Text(eintrag.userName).font(.headline)
                Spacer()
                Text(hours(eintrag.duration)).font(.subheadline.monospacedDigit()).foregroundStyle(.secondary)
            }
            Text("\(eintrag.start.formatted(.dateTime.weekday(.abbreviated).day().month(.abbreviated))), \(eintrag.start.timeString) – \(eintrag.end.timeString)")
                .font(.subheadline)
                .foregroundStyle(.secondary)
            if let text = eintrag.description.nonEmpty {
                Text(text).lineLimit(3)
            }
            HStack(spacing: 6) {
                if eintrag.hatGastgeschraubt {
                    StatusPill(text: "Gast", color: eintrag.gastschraubenBezahlt == true ? .green : .orange, systemImage: "person.badge.plus")
                }
                if eintrag.hatFamiliegeschraubt {
                    StatusPill(text: "Familie", color: .purple, systemImage: "figure.2.and.child.holdinghands")
                }
                if let fahrzeug = eintrag.fahrzeugLabel.nonEmpty {
                    StatusPill(text: fahrzeug, color: .secondary, systemImage: "car")
                }
            }
        }
        .padding(.vertical, 2)
    }
}

func hours(_ interval: TimeInterval) -> String {
    let value = max(interval, 0) / 3600
    return value.formatted(.number.precision(.fractionLength(0...1))) + " h"
}

struct HallenbuchFormView: View {
    let eintrag: HallenbuchEintrag?
    let onSaved: () async -> Void

    @Environment(AppSession.self) private var session
    @Environment(\.dismiss) private var dismiss
    @State private var input: HallenbuchInput
    @State private var fahrzeuge = Remote<[Fahrzeug]>("fahrzeuge")
    @State private var action = ActionRunner()

    init(eintrag: HallenbuchEintrag?, onSaved: @escaping () async -> Void) {
        self.eintrag = eintrag
        self.onSaved = onSaved
        let now = Date.now
        _input = State(initialValue: HallenbuchInput(
            start: eintrag?.start ?? now.addingTimeInterval(-3 * 3600),
            end: eintrag?.end ?? now,
            description: eintrag?.description ?? "",
            hatGastgeschraubt: eintrag?.hatGastgeschraubt ?? false,
            gastschraubenArt: eintrag?.gastschraubenArt,
            gastschraubenBezahlt: eintrag?.gastschraubenBezahlt,
            hatFamiliegeschraubt: eintrag?.hatFamiliegeschraubt ?? false,
            fahrzeugId: eintrag?.fahrzeugId?.lowercased()
        ))
    }

    var body: some View {
        NavigationStack {
            Form {
                Section {
                    DatePicker("Beginn", selection: $input.start)
                    DatePicker("Ende", selection: $input.end, in: input.start...)
                    LabeledContent("Dauer", value: hours(input.end.timeIntervalSince(input.start)))
                }
                Section {
                    TextField("Was hast du gemacht?", text: $input.description, axis: .vertical).lineLimit(3...8)
                    Picker("Fahrzeug", selection: $input.fahrzeugId) {
                        Text("Keins").tag(String?.none)
                        ForEach(fahrzeuge.value ?? []) { Text($0.label).tag(Optional($0.id.lowercased())) }
                    }
                }
                Section {
                    Toggle("Gast hat geschraubt", isOn: $input.hatGastgeschraubt)
                    if input.hatGastgeschraubt {
                        Picker("Gegenleistung", selection: Binding(
                            get: { input.gastschraubenArt ?? .kastenPremiumbier },
                            set: { input.gastschraubenArt = $0 }
                        )) {
                            ForEach(GastschraubenArt.selectable) { Text($0.label).tag($0) }
                        }
                        Toggle("Bezahlt", isOn: Binding(
                            get: { input.gastschraubenBezahlt ?? false },
                            set: { input.gastschraubenBezahlt = $0 }
                        ))
                    }
                    Toggle("Familie hat geschraubt", isOn: $input.hatFamiliegeschraubt)
                }
            }
            .navigationTitle(eintrag == nil ? "Neuer Eintrag" : "Eintrag bearbeiten")
            .navigationBarTitleDisplayMode(.inline)
            .task {
                await fahrzeuge.load(session)
                if eintrag == nil, input.fahrzeugId == nil {
                    input.fahrzeugId = fahrzeuge.value?.first(where: \.istStandard)?.id.lowercased()
                }
            }
            .toolbar {
                ToolbarItem(placement: .cancellationAction) { Button("Abbrechen", role: .cancel) { dismiss() } }
                ToolbarItem(placement: .confirmationAction) {
                    Button("Sichern", role: .confirm) { Task { await save() } }
                        .disabled(input.end <= input.start || action.isRunning)
                        .requiresOnline()
                }
            }
            .actionAlert(action)
        }
    }

    private func save() async {
        var payload = input
        payload.description = input.description.trimmed
        if !payload.hatGastgeschraubt {
            payload.gastschraubenArt = nil
            payload.gastschraubenBezahlt = nil
        } else if payload.gastschraubenArt == nil {
            payload.gastschraubenArt = .kastenPremiumbier
        }
        let success = await action.run {
            if let eintrag {
                try await session.client.send(.put, "hallenbuch/\(eintrag.id)", body: payload)
            } else {
                try await session.client.send(.post, "hallenbuch", body: payload)
            }
        }
        if success {
            await onSaved()
            dismiss()
        }
    }
}

struct HallenbuchStatistikView: View {
    @Environment(AppSession.self) private var session
    @State private var from = Calendar.current.dateInterval(of: .year, for: .now)?.start ?? .now
    @State private var to = Date.now
    @State private var remote = Remote<[HallenbuchStatistik]>("hallenbuch/statistik")

    var body: some View {
        List {
            Section("Zeitraum") {
                DatePicker("Von", selection: $from, displayedComponents: .date)
                DatePicker("Bis", selection: $to, in: from..., displayedComponents: .date)
            }
            if let rows = remote.value {
                Section {
                    ForEach(rows.sorted { $0.gesamt > $1.gesamt }) { row in
                        VStack(alignment: .leading, spacing: 4) {
                            HStack {
                                Text(row.userName).font(.headline)
                                Spacer()
                                Text(hours(row.gesamt * 3600)).font(.headline.monospacedDigit())
                            }
                            Text("Eigen \(hours(row.eigeneStunden * 3600)) · Gast \(hours(row.gastStunden * 3600)) · Familie \(hours(row.familieStunden * 3600))")
                                .font(.caption)
                                .foregroundStyle(.secondary)
                        }
                    }
                } footer: {
                    LastUpdatedFooter(date: remote.lastUpdated)
                }
            } else if let failure = remote.failure {
                Text(failure.localizedDescription).foregroundStyle(.secondary)
            } else {
                ProgressView()
            }
        }
        .navigationTitle("Statistik")
        .task(id: [from, to]) { await load() }
        .refreshable { await load() }
    }

    private func load() async {
        let end = Calendar.current.date(byAdding: .day, value: 1, to: to.startOfDay) ?? to
        await remote.load(session, path: "hallenbuch/statistik", query: [
            URLQueryItem(name: "from", value: DateParsing.string(from: from.startOfDay)),
            URLQueryItem(name: "to", value: DateParsing.string(from: end)),
        ])
    }
}
