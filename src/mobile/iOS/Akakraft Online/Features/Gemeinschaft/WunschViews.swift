import SwiftUI

struct WunschListView: View {
    @Environment(AppSession.self) private var session
    @State private var remote = Remote<[Wunsch]>("wunsch")
    @State private var showClosed = false
    @State private var showCreate = false
    @State private var action = ActionRunner()

    var body: some View {
        RemoteContent(remote: remote) { items in
            let open = items.filter { $0.status == .offen }.sorted { ($0.score, $0.createdAt) > ($1.score, $1.createdAt) }
            let closed = items.filter { $0.status != .offen }.sorted { ($0.closedAt ?? $0.createdAt) > ($1.closedAt ?? $1.createdAt) }
            List {
                if open.isEmpty {
                    ContentUnavailableView("Keine offenen Wünsche", systemImage: "sparkles",
                                           description: Text("Was fehlt in der Halle? Schlag es vor!"))
                }
                ForEach(open) { wunsch in
                    NavigationLink(value: wunsch) {
                        WunschRow(wunsch: wunsch) { up in Task { await vote(wunsch, up: up) } }
                    }
                }
                if !closed.isEmpty {
                    Section(isExpanded: $showClosed) {
                        ForEach(closed) { wunsch in
                            NavigationLink(value: wunsch) { WunschRow(wunsch: wunsch, onVote: nil) }
                        }
                    } header: {
                        Text("Erledigt (\(closed.count))")
                    }
                }
                Section {} footer: { LastUpdatedFooter(date: remote.lastUpdated) }
            }
            .listStyle(.sidebar)
        }
        .refreshable { await remote.load(session) }
        .task { await remote.load(session) }
        .navigationTitle("Wunschliste")
        .navigationBarTitleDisplayMode(.inline)
        .navigationDestination(for: Wunsch.self) { wunsch in
            WunschDetailView(wunsch: wunsch) { await remote.load(session) }
        }
        .toolbar {
            ToolbarItem(placement: .topBarTrailing) {
                Button("Wunsch hinzufügen", systemImage: "plus") { showCreate = true }
                    .requiresOnline()
            }
        }
        .sheet(isPresented: $showCreate) {
            WunschFormView(wunsch: nil) { await remote.load(session) }
        }
        .actionAlert(action)
    }

    private struct VoteBody: Encodable { let isUpvote: Bool }

    private func vote(_ wunsch: Wunsch, up: Bool) async {
        await action.run {
            let updated = try await session.client.send(.post, "wunsch/\(wunsch.id)/vote", body: VoteBody(isUpvote: up), decoding: Wunsch.self)
            remote.update { list in
                if let index = list.firstIndex(where: { $0.id == updated.id }) { list[index] = updated }
            }
        }
    }
}

struct WunschRow: View {
    let wunsch: Wunsch
    let onVote: ((Bool) -> Void)?

    var body: some View {
        HStack(spacing: 12) {
            VStack(alignment: .leading, spacing: 4) {
                Text(wunsch.title).font(.headline)
                if let price = wunsch.priceText {
                    Text(price).font(.subheadline).foregroundStyle(.secondary)
                }
                if wunsch.status != .offen {
                    StatusPill(text: wunsch.status.label, color: wunsch.status.color)
                }
            }
            Spacer()
            if let onVote {
                VoteControl(wunsch: wunsch, onVote: onVote)
            } else {
                Text("\(wunsch.score)").font(.headline.monospacedDigit()).foregroundStyle(.secondary)
            }
        }
    }
}

struct VoteControl: View {
    let wunsch: Wunsch
    let onVote: (Bool) -> Void

    var body: some View {
        HStack(spacing: 2) {
            Button {
                onVote(true)
            } label: {
                Image(systemName: wunsch.currentUserVote == true ? "hand.thumbsup.fill" : "hand.thumbsup")
            }
            .tint(wunsch.currentUserVote == true ? .green : .secondary)
            .accessibilityLabel("Dafür")

            Text("\(wunsch.score)")
                .font(.headline.monospacedDigit())
                .frame(minWidth: 26)
                .contentTransition(.numericText())

            Button {
                onVote(false)
            } label: {
                Image(systemName: wunsch.currentUserVote == false ? "hand.thumbsdown.fill" : "hand.thumbsdown")
            }
            .tint(wunsch.currentUserVote == false ? .red : .secondary)
            .accessibilityLabel("Dagegen")
        }
        .buttonStyle(.borderless)
        .requiresOnline()
    }
}

struct WunschDetailView: View {
    let onChange: () async -> Void

    @Environment(AppSession.self) private var session
    @State private var wunsch: Wunsch
    @State private var action = ActionRunner()
    @State private var showEdit = false
    @State private var showClose = false

    init(wunsch: Wunsch, onChange: @escaping () async -> Void) {
        self.onChange = onChange
        _wunsch = State(initialValue: wunsch)
    }

    private struct VoteBody: Encodable { let isUpvote: Bool }

    var body: some View {
        List {
            Section {
                VStack(alignment: .leading, spacing: 8) {
                    Text(wunsch.title).font(.title2.bold())
                    if let description = wunsch.description.nonEmpty { Text(description) }
                    Text("Vorgeschlagen von \(wunsch.createdByName) am \(wunsch.createdAt.dayString)")
                        .font(.footnote).foregroundStyle(.secondary)
                }
                .padding(.vertical, 4)
                if let price = wunsch.priceText {
                    DetailRow(title: "Preis", value: price, systemImage: "eurosign")
                }
                if let url = wunsch.link.asURL {
                    Link(destination: url) {
                        Label(url.host() ?? url.absoluteString, systemImage: "link")
                    }
                }
            }

            Section("Abstimmung") {
                LabeledContent("Dafür", value: "\(wunsch.upVotes)")
                LabeledContent("Dagegen", value: "\(wunsch.downVotes)")
                if wunsch.status == .offen {
                    HStack {
                        Spacer()
                        VoteControl(wunsch: wunsch) { up in
                            Task {
                                await action.run {
                                    wunsch = try await session.client.send(.post, "wunsch/\(wunsch.id)/vote", body: VoteBody(isUpvote: up), decoding: Wunsch.self)
                                }
                                await onChange()
                            }
                        }
                        .font(.title2)
                        Spacer()
                    }
                }
            }

            if wunsch.status != .offen {
                Section("Abschluss") {
                    StatusPill(text: wunsch.status.label, color: wunsch.status.color)
                    if let name = wunsch.closedByName {
                        DetailRow(title: "Von", value: name, systemImage: "person")
                    }
                    if let date = wunsch.closedAt {
                        DetailRow(title: "Am", value: date.dayString, systemImage: "calendar")
                    }
                    if let note = wunsch.closeNote.nonEmpty { Text(note) }
                }
            }
        }
        .navigationTitle("Wunsch")
        .navigationBarTitleDisplayMode(.inline)
        .toolbar {
            let canEdit = session.permissions.isOwnerOrPrivileged(wunsch.createdByUserId)
            if wunsch.status == .offen && (canEdit || session.permissions.isPrivileged) {
                ToolbarItem(placement: .topBarTrailing) {
                    Menu("Aktionen", systemImage: "ellipsis") {
                        if canEdit {
                            Button("Bearbeiten", systemImage: "pencil") { showEdit = true }
                        }
                        if session.permissions.isPrivileged {
                            Button("Abschließen", systemImage: "checkmark.seal") { showClose = true }
                        }
                    }
                    .requiresOnline()
                }
            }
        }
        .sheet(isPresented: $showEdit) {
            WunschFormView(wunsch: wunsch) { await refresh() }
        }
        .sheet(isPresented: $showClose) {
            WunschCloseSheet(wunsch: wunsch) { updated in
                wunsch = updated
                await onChange()
            }
        }
        .actionAlert(action)
    }

    private func refresh() async {
        if let all = try? await session.fetch([Wunsch].self, path: "wunsch").value,
           let fresh = all.first(where: { $0.id == wunsch.id }) {
            wunsch = fresh
        }
        await onChange()
    }
}

struct WunschFormView: View {
    let wunsch: Wunsch?
    let onSaved: () async -> Void

    @Environment(AppSession.self) private var session
    @Environment(\.dismiss) private var dismiss
    @State private var input: WunschInput
    @State private var action = ActionRunner()

    init(wunsch: Wunsch?, onSaved: @escaping () async -> Void) {
        self.wunsch = wunsch
        self.onSaved = onSaved
        _input = State(initialValue: WunschInput(
            title: wunsch?.title ?? "", description: wunsch?.description ?? "",
            link: wunsch?.link, priceMin: wunsch?.priceMin, priceMax: wunsch?.priceMax
        ))
    }

    var body: some View {
        NavigationStack {
            Form {
                Section {
                    TextField("Titel", text: $input.title)
                    TextField("Beschreibung", text: $input.description, axis: .vertical).lineLimit(3...8)
                    TextField("Link (optional)", text: optional($input.link))
                        .keyboardType(.URL)
                        .textInputAutocapitalization(.never)
                }
                Section("Preisspanne") {
                    TextField("Von (€)", value: $input.priceMin, format: .number)
                        .keyboardType(.decimalPad)
                    TextField("Bis (€)", value: $input.priceMax, format: .number)
                        .keyboardType(.decimalPad)
                }
            }
            .navigationTitle(wunsch == nil ? "Neuer Wunsch" : "Wunsch bearbeiten")
            .navigationBarTitleDisplayMode(.inline)
            .toolbar {
                ToolbarItem(placement: .cancellationAction) { Button("Abbrechen", role: .cancel) { dismiss() } }
                ToolbarItem(placement: .confirmationAction) {
                    Button("Sichern", role: .confirm) {
                        Task {
                            var payload = input
                            payload.link = input.link.nonEmpty
                            let success = await action.run {
                                if let wunsch {
                                    try await session.client.send(.put, "wunsch/\(wunsch.id)", body: payload)
                                } else {
                                    try await session.client.send(.post, "wunsch", body: payload)
                                }
                            }
                            if success {
                                await onSaved()
                                dismiss()
                            }
                        }
                    }
                    .disabled(input.title.trimmed.isEmpty || action.isRunning)
                    .requiresOnline()
                }
            }
            .actionAlert(action)
        }
    }
}

private struct WunschCloseSheet: View {
    let wunsch: Wunsch
    let onSaved: (Wunsch) async -> Void

    @Environment(AppSession.self) private var session
    @Environment(\.dismiss) private var dismiss
    @State private var status: WunschStatus = .angeschafft
    @State private var note = ""
    @State private var action = ActionRunner()

    private struct CloseBody: Encodable {
        let status: WunschStatus
        let closeNote: String?
    }

    var body: some View {
        NavigationStack {
            Form {
                Picker("Ergebnis", selection: $status) {
                    Text("Angeschafft").tag(WunschStatus.angeschafft)
                    Text("Abgelehnt").tag(WunschStatus.abgelehnt)
                }
                .pickerStyle(.inline)
                Section("Notiz") {
                    TextField("Optional", text: $note, axis: .vertical).lineLimit(2...5)
                }
            }
            .navigationTitle("Wunsch abschließen")
            .navigationBarTitleDisplayMode(.inline)
            .toolbar {
                ToolbarItem(placement: .cancellationAction) { Button("Abbrechen", role: .cancel) { dismiss() } }
                ToolbarItem(placement: .confirmationAction) {
                    Button("Abschließen", role: .confirm) {
                        Task {
                            var updated: Wunsch?
                            let success = await action.run {
                                updated = try await session.client.send(
                                    .post, "wunsch/\(wunsch.id)/close",
                                    body: CloseBody(status: status, closeNote: note.nonEmpty), decoding: Wunsch.self
                                )
                            }
                            if success, let updated {
                                await onSaved(updated)
                                dismiss()
                            }
                        }
                    }
                    .disabled(action.isRunning)
                    .requiresOnline()
                }
            }
            .actionAlert(action)
        }
        .presentationDetents([.medium, .large])
    }
}
