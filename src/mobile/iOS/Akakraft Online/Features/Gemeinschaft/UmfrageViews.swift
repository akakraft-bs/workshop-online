import SwiftUI

struct UmfrageListView: View {
    @Environment(AppSession.self) private var session
    @State private var remote = Remote<[Umfrage]>("umfrage")
    @State private var showCreate = false

    var body: some View {
        RemoteContent(remote: remote) { items in
            let open = items.filter(\.isOpen).sorted { ($0.deadline ?? .distantFuture) < ($1.deadline ?? .distantFuture) }
            let closed = items.filter { !$0.isOpen }.sorted { ($0.closedAt ?? $0.createdAt) > ($1.closedAt ?? $1.createdAt) }
            List {
                Section("Offen") {
                    if open.isEmpty { Text("Keine offenen Umfragen.").foregroundStyle(.secondary) }
                    ForEach(open) { umfrage in
                        NavigationLink(value: umfrage) { UmfrageRow(umfrage: umfrage) }
                    }
                }
                if !closed.isEmpty {
                    Section("Abgeschlossen") {
                        ForEach(closed) { umfrage in
                            NavigationLink(value: umfrage) { UmfrageRow(umfrage: umfrage) }
                        }
                    }
                }
                Section {} footer: { LastUpdatedFooter(date: remote.lastUpdated) }
            }
        }
        .refreshable { await remote.load(session) }
        .task { await remote.load(session) }
        .navigationTitle("Umfragen")
        .navigationBarTitleDisplayMode(.inline)
        .navigationDestination(for: Umfrage.self) { umfrage in
            UmfrageDetailView(umfrage: umfrage) { await remote.load(session) }
        }
        .toolbar {
            ToolbarItem(placement: .topBarTrailing) {
                Button("Neue Umfrage", systemImage: "plus") { showCreate = true }
                    .requiresOnline()
            }
        }
        .sheet(isPresented: $showCreate) {
            UmfrageFormView(umfrage: nil) { await remote.load(session) }
        }
    }
}

struct UmfrageRow: View {
    let umfrage: Umfrage

    var body: some View {
        VStack(alignment: .leading, spacing: 4) {
            Text(umfrage.question).font(.headline).lineLimit(2)
            HStack(spacing: 6) {
                if umfrage.isOpen {
                    if umfrage.hasParticipated {
                        StatusPill(text: umfrage.currentUserAbstained ? "Enthalten" : "Abgestimmt", color: .green, systemImage: "checkmark")
                    } else {
                        StatusPill(text: "Noch offen", color: .orange, systemImage: "hand.raised")
                    }
                } else {
                    StatusPill(text: "Geschlossen", color: .gray)
                }
                Label("\(umfrage.participantCount)", systemImage: "person.2")
                    .font(.caption)
                    .foregroundStyle(.secondary)
                if let deadline = umfrage.deadline, umfrage.isOpen {
                    Label(deadline.relativeString, systemImage: "clock")
                        .font(.caption)
                        .foregroundStyle(deadline < .now ? .red : .secondary)
                }
            }
        }
        .padding(.vertical, 2)
    }
}

struct UmfrageDetailView: View {
    let onChange: () async -> Void

    @Environment(AppSession.self) private var session
    @Environment(\.dismiss) private var dismiss
    @State private var umfrage: Umfrage
    @State private var selection: Set<String>
    @State private var action = ActionRunner()
    @State private var showEdit = false
    @State private var confirmDelete = false
    @State private var confirmClose = false
    @State private var reminderSent = false

    init(umfrage: Umfrage, onChange: @escaping () async -> Void) {
        self.onChange = onChange
        _umfrage = State(initialValue: umfrage)
        _selection = State(initialValue: Set(umfrage.currentUserOptionIds.map { $0.lowercased() }))
    }

    private var canManage: Bool { session.permissions.isOwnerOrPrivileged(umfrage.createdByUserId) }
    private var totalVotes: Int { umfrage.options.compactMap(\.voteCount).reduce(0, +) }
    private var selectionChanged: Bool { selection != Set(umfrage.currentUserOptionIds.map { $0.lowercased() }) }

    var body: some View {
        List {
            Section {
                VStack(alignment: .leading, spacing: 8) {
                    Text(umfrage.question).font(.title2.bold())
                    if let description = umfrage.description.nonEmpty { Text(description) }
                    Text("Von \(umfrage.createdByName) · \(umfrage.createdAt.dayString)")
                        .font(.footnote).foregroundStyle(.secondary)
                }
                .padding(.vertical, 4)
                if let title = umfrage.linkedEventTitle {
                    DetailRow(title: "Veranstaltung", value: [title, umfrage.linkedEventStart?.dayTimeString].compactMap { $0 }.joined(separator: " · "),
                              systemImage: "party.popper")
                }
                if let deadline = umfrage.deadline {
                    DetailRow(title: "Frist", value: deadline.dayTimeString, systemImage: "clock")
                }
                DetailRow(title: "Teilnahmen", value: "\(umfrage.participantCount) (davon \(umfrage.enthaltungCount) Enthaltungen)",
                          systemImage: "person.2")
            }

            Section {
                ForEach(umfrage.sortedOptions) { option in
                    optionRow(option)
                }
            } header: {
                Text(umfrage.isMultipleChoice ? "Mehrfachauswahl möglich" : "Eine Antwort wählen")
            } footer: {
                if !umfrage.resultsAvailable {
                    Text(umfrage.revealAfterClose
                         ? "Die Ergebnisse werden nach dem Schließen sichtbar."
                         : "Die Ergebnisse sind nur für die erstellende Person und den Vorstand sichtbar.")
                }
            }

            if umfrage.isOpen {
                Section {
                    Button {
                        Task { await vote() }
                    } label: {
                        Label(umfrage.currentUserOptionIds.isEmpty ? "Abstimmen" : "Stimme ändern", systemImage: "checkmark.circle.fill")
                    }
                    .disabled(selection.isEmpty || !selectionChanged || action.isRunning)

                    Button {
                        Task { await abstain() }
                    } label: {
                        Label("Enthalten", systemImage: "minus.circle")
                    }
                    .disabled(umfrage.currentUserAbstained || action.isRunning)
                }
                .requiresOnline()
            } else if let closedBy = umfrage.closedByName {
                Section {
                    Text("Geschlossen von \(closedBy)\(umfrage.closedAt.map { " am \($0.dayString)" } ?? "")")
                        .foregroundStyle(.secondary)
                }
            }
        }
        .navigationTitle("Umfrage")
        .navigationBarTitleDisplayMode(.inline)
        .toolbar {
            if canManage {
                ToolbarItem(placement: .topBarTrailing) {
                    Menu("Verwalten", systemImage: "ellipsis") {
                        if umfrage.isOpen {
                            Button("Bearbeiten", systemImage: "pencil") { showEdit = true }
                            Button(reminderSent ? "Erinnerung gesendet" : "Erinnerung senden", systemImage: "bell") {
                                Task { await remind() }
                            }
                            .disabled(reminderSent)
                            Button("Schließen", systemImage: "lock") { confirmClose = true }
                        }
                        Button("Löschen", systemImage: "trash", role: .destructive) { confirmDelete = true }
                    }
                    .requiresOnline()
                }
            }
        }
        .sheet(isPresented: $showEdit) {
            UmfrageFormView(umfrage: umfrage) { await refresh() }
        }
        .confirmationDialog("Umfrage schließen?", isPresented: $confirmClose, titleVisibility: .visible) {
            Button("Schließen") {
                Task {
                    await action.run { umfrage = try await session.client.send(.post, "umfrage/\(umfrage.id)/close", decoding: Umfrage.self) }
                    await onChange()
                }
            }
        } message: {
            Text("Danach kann niemand mehr abstimmen.")
        }
        .confirmationDialog("Umfrage löschen?", isPresented: $confirmDelete, titleVisibility: .visible) {
            Button("Löschen", role: .destructive) {
                Task {
                    if await action.run({ try await session.client.send(.delete, "umfrage/\(umfrage.id)") }) {
                        await onChange()
                        dismiss()
                    }
                }
            }
        }
        .actionAlert(action)
    }

    @ViewBuilder
    private func optionRow(_ option: UmfrageOption) -> some View {
        let isSelected = selection.contains(option.id.lowercased())
        Button {
            guard umfrage.isOpen else { return }
            toggle(option)
        } label: {
            VStack(alignment: .leading, spacing: 6) {
                HStack {
                    Image(systemName: symbol(selected: isSelected))
                        .foregroundStyle(isSelected ? Color.accentColor : .secondary)
                        .font(.title3)
                    Text(option.text).foregroundStyle(Color.primary)
                    Spacer()
                    if let count = option.voteCount {
                        Text("\(count)").font(.headline.monospacedDigit())
                    }
                }
                if let count = option.voteCount {
                    ProgressView(value: Double(count), total: Double(max(totalVotes, 1)))
                        .tint(isSelected ? Color.accentColor : .secondary)
                    if let voters = option.voters, !voters.isEmpty {
                        Text(voters.joined(separator: ", "))
                            .font(.caption)
                            .foregroundStyle(.secondary)
                    }
                }
            }
        }
        .disabled(!umfrage.isOpen)
        .accessibilityAddTraits(isSelected ? .isSelected : [])
    }

    private func symbol(selected: Bool) -> String {
        if umfrage.isMultipleChoice { return selected ? "checkmark.square.fill" : "square" }
        return selected ? "largecircle.fill.circle" : "circle"
    }

    private func toggle(_ option: UmfrageOption) {
        let id = option.id.lowercased()
        if umfrage.isMultipleChoice {
            if selection.contains(id) { selection.remove(id) } else { selection.insert(id) }
        } else {
            selection = [id]
        }
    }

    private struct VoteBody: Encodable { let optionIds: [String] }

    private func vote() async {
        let ids = umfrage.options.map(\.id).filter { selection.contains($0.lowercased()) }
        await action.run {
            umfrage = try await session.client.send(.post, "umfrage/\(umfrage.id)/vote", body: VoteBody(optionIds: ids), decoding: Umfrage.self)
        }
        selection = Set(umfrage.currentUserOptionIds.map { $0.lowercased() })
        await onChange()
    }

    private func abstain() async {
        await action.run {
            umfrage = try await session.client.send(.post, "umfrage/\(umfrage.id)/abstain", decoding: Umfrage.self)
        }
        selection = Set(umfrage.currentUserOptionIds.map { $0.lowercased() })
        await onChange()
    }

    private func remind() async {
        if await action.run({ try await session.client.send(.post, "umfrage/\(umfrage.id)/remind") }) {
            reminderSent = true
        }
    }

    private func refresh() async {
        if let all = try? await session.fetch([Umfrage].self, path: "umfrage").value,
           let fresh = all.first(where: { $0.id == umfrage.id }) {
            umfrage = fresh
        }
        await onChange()
    }
}

struct UmfrageFormView: View {
    let umfrage: Umfrage?
    let onSaved: () async -> Void

    private struct EditableOption: Identifiable {
        let id = UUID()
        var serverId: String?
        var text: String
    }

    @Environment(AppSession.self) private var session
    @Environment(\.dismiss) private var dismiss
    @State private var question: String
    @State private var description: String
    @State private var options: [EditableOption]
    @State private var isMultipleChoice: Bool
    @State private var resultsVisible: Bool
    @State private var revealAfterClose: Bool
    @State private var hasDeadline: Bool
    @State private var deadline: Date
    @State private var linkedEvent: CalendarEvent?
    @State private var events = Remote<[CalendarEvent]>("calendar/upcoming-events")
    @State private var action = ActionRunner()

    init(umfrage: Umfrage?, onSaved: @escaping () async -> Void) {
        self.umfrage = umfrage
        self.onSaved = onSaved
        _question = State(initialValue: umfrage?.question ?? "")
        _description = State(initialValue: umfrage?.description ?? "")
        _options = State(initialValue: umfrage?.sortedOptions.map { EditableOption(serverId: $0.id, text: $0.text) }
                         ?? [EditableOption(text: ""), EditableOption(text: "")])
        _isMultipleChoice = State(initialValue: umfrage?.isMultipleChoice ?? false)
        _resultsVisible = State(initialValue: umfrage?.resultsVisible ?? true)
        _revealAfterClose = State(initialValue: umfrage?.revealAfterClose ?? true)
        _hasDeadline = State(initialValue: umfrage?.deadline != nil)
        _deadline = State(initialValue: umfrage?.deadline ?? Calendar.current.date(byAdding: .day, value: 7, to: .now) ?? .now)
    }

    var body: some View {
        NavigationStack {
            Form {
                Section {
                    TextField("Frage", text: $question, axis: .vertical)
                    TextField("Beschreibung (optional)", text: $description, axis: .vertical).lineLimit(2...6)
                }
                Section("Antworten") {
                    ForEach($options) { $option in
                        TextField("Antwort", text: $option.text)
                    }
                    .onDelete { options.remove(atOffsets: $0) }
                    .onMove { options.move(fromOffsets: $0, toOffset: $1) }
                    Button("Antwort hinzufügen", systemImage: "plus.circle") {
                        options.append(EditableOption(text: ""))
                    }
                }
                Section("Einstellungen") {
                    Toggle("Mehrfachauswahl", isOn: $isMultipleChoice)
                    Toggle("Ergebnisse für alle sichtbar", isOn: $resultsVisible)
                    if !resultsVisible {
                        Toggle("Nach dem Schließen anzeigen", isOn: $revealAfterClose)
                    }
                    Toggle("Frist", isOn: $hasDeadline)
                    if hasDeadline {
                        DatePicker("Endet am", selection: $deadline, in: Date.now...)
                    }
                }
                Section("Veranstaltung") {
                    Picker("Verknüpfen mit", selection: $linkedEvent) {
                        Text("Keine").tag(CalendarEvent?.none)
                        ForEach(events.value ?? []) { event in
                            Text("\(event.title)\(event.start.map { " · \($0.dayString)" } ?? "")").tag(Optional(event))
                        }
                    }
                }
            }
            .environment(\.editMode, .constant(.active))
            .navigationTitle(umfrage == nil ? "Neue Umfrage" : "Umfrage bearbeiten")
            .navigationBarTitleDisplayMode(.inline)
            .task {
                await events.load(session)
                if let umfrage, let eventId = umfrage.linkedEventId {
                    linkedEvent = events.value?.first { $0.id == eventId }
                }
            }
            .toolbar {
                ToolbarItem(placement: .cancellationAction) { Button("Abbrechen", role: .cancel) { dismiss() } }
                ToolbarItem(placement: .confirmationAction) {
                    Button("Sichern", role: .confirm) { Task { await save() } }
                        .disabled(!isValid || action.isRunning)
                        .requiresOnline()
                }
            }
            .actionAlert(action)
        }
    }

    private var filledOptions: [EditableOption] { options.filter { !$0.text.trimmed.isEmpty } }
    private var isValid: Bool { !question.trimmed.isEmpty && filledOptions.count >= 2 }

    private func save() async {
        let event = linkedEvent
        let keepExistingLink = umfrage?.linkedEventId != nil && event == nil && events.value == nil
        let success = await action.run {
            if let umfrage {
                let input = UmfrageInput(
                    question: question.trimmed, description: description.nonEmpty,
                    options: filledOptions.map { .init(id: $0.serverId, text: $0.text.trimmed) },
                    isMultipleChoice: isMultipleChoice, resultsVisible: resultsVisible,
                    revealAfterClose: resultsVisible ? false : revealAfterClose,
                    deadline: hasDeadline ? deadline : nil,
                    linkedEventId: keepExistingLink ? umfrage.linkedEventId : event?.id,
                    linkedCalendarId: keepExistingLink ? umfrage.linkedCalendarId : event?.calendarId,
                    linkedEventTitle: keepExistingLink ? umfrage.linkedEventTitle : event?.title,
                    linkedEventStart: keepExistingLink ? umfrage.linkedEventStart : event?.start
                )
                try await session.client.send(.put, "umfrage/\(umfrage.id)", body: input)
            } else {
                let input = UmfrageCreateInput(
                    question: question.trimmed, description: description.nonEmpty,
                    options: filledOptions.map { $0.text.trimmed },
                    isMultipleChoice: isMultipleChoice, resultsVisible: resultsVisible,
                    revealAfterClose: resultsVisible ? false : revealAfterClose,
                    deadline: hasDeadline ? deadline : nil,
                    linkedEventId: event?.id, linkedCalendarId: event?.calendarId,
                    linkedEventTitle: event?.title, linkedEventStart: event?.start
                )
                try await session.client.send(.post, "umfrage", body: input)
            }
        }
        if success {
            await onSaved()
            dismiss()
        }
    }
}
