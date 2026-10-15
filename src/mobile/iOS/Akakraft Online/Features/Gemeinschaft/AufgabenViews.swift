import SwiftUI

struct AufgabenListView: View {
    @Environment(AppSession.self) private var session
    @State private var remote = Remote<[Aufgabe]>("aufgaben")
    @State private var showDone = false
    @State private var showCreate = false

    var body: some View {
        RemoteContent(remote: remote) { items in
            let open = items.filter { $0.status != .erledigt }.sorted { ($0.priority, $1.createdAt) < ($1.priority, $0.createdAt) }
            let done = items.filter { $0.status == .erledigt }.sorted { $0.createdAt > $1.createdAt }
            List {
                if open.isEmpty {
                    ContentUnavailableView("Alles erledigt", systemImage: "checkmark.circle", description: Text("Es gibt keine offenen Aufgaben."))
                }
                ForEach(open) { aufgabe in
                    NavigationLink(value: aufgabe) { AufgabeRow(aufgabe: aufgabe) }
                }
                if !done.isEmpty {
                    Section(isExpanded: $showDone) {
                        ForEach(done) { aufgabe in
                            NavigationLink(value: aufgabe) { AufgabeRow(aufgabe: aufgabe) }
                        }
                    } header: {
                        Text("Erledigt (\(done.count))")
                    }
                }
                Section {} footer: { LastUpdatedFooter(date: remote.lastUpdated) }
            }
            .listStyle(.sidebar)
        }
        .refreshable { await remote.load(session) }
        .task { await remote.load(session) }
        .navigationTitle("Aufgaben")
        .navigationBarTitleDisplayMode(.inline)
        .navigationDestination(for: Aufgabe.self) { aufgabe in
            AufgabeDetailView(aufgabe: aufgabe) { await remote.load(session) }
        }
        .toolbar {
            if session.permissions.isPrivileged {
                ToolbarItem(placement: .topBarTrailing) {
                    Button("Neue Aufgabe", systemImage: "plus") { showCreate = true }
                        .requiresOnline()
                }
            }
        }
        .sheet(isPresented: $showCreate) {
            AufgabeFormView(aufgabe: nil) { await remote.load(session) }
        }
    }
}

struct AufgabeRow: View {
    let aufgabe: Aufgabe

    var body: some View {
        let priority = AufgabePriority(value: aufgabe.priority)
        HStack(spacing: 12) {
            Image(systemName: aufgabe.status == .erledigt ? "checkmark.circle.fill" : "circle")
                .font(.title3)
                .foregroundStyle(aufgabe.status == .erledigt ? .green : priority.color)
            VStack(alignment: .leading, spacing: 3) {
                Text(aufgabe.titel)
                    .font(.headline)
                    .strikethrough(aufgabe.status == .erledigt)
                HStack(spacing: 6) {
                    if aufgabe.status != .erledigt {
                        StatusPill(text: priority.shortLabel, color: priority.color)
                    }
                    if let assignee = aufgabe.assignee {
                        Label(assignee, systemImage: "person").font(.caption).foregroundStyle(.secondary)
                    }
                }
            }
        }
        .accessibilityElement(children: .combine)
    }
}

struct AufgabeDetailView: View {
    let onChange: () async -> Void

    @Environment(AppSession.self) private var session
    @Environment(\.dismiss) private var dismiss
    @State private var aufgabe: Aufgabe
    @State private var action = ActionRunner()
    @State private var showEdit = false
    @State private var confirmDelete = false

    init(aufgabe: Aufgabe, onChange: @escaping () async -> Void) {
        self.onChange = onChange
        _aufgabe = State(initialValue: aufgabe)
    }

    var body: some View {
        let priority = AufgabePriority(value: aufgabe.priority)
        List {
            if let url = aufgabe.fotoUrl.asURL {
                Section {
                    RemoteImage(url: url, contentMode: .fit)
                        .frame(maxWidth: .infinity, minHeight: 200, maxHeight: 340)
                        .listRowInsets(EdgeInsets())
                }
            }
            Section {
                VStack(alignment: .leading, spacing: 8) {
                    HStack {
                        StatusPill(text: aufgabe.status.label, color: aufgabe.status.color)
                        StatusPill(text: priority.shortLabel, color: priority.color, systemImage: "flag.fill")
                    }
                    Text(aufgabe.titel).font(.title2.bold())
                    if let text = aufgabe.beschreibung.nonEmpty { Text(text) }
                }
                .padding(.vertical, 4)
                DetailRow(title: "Priorität", value: priority.label, systemImage: "flag")
                DetailRow(title: "Zuständig", value: aufgabe.assignee ?? "Niemand", systemImage: "person")
                DetailRow(title: "Erstellt", value: "\(aufgabe.createdByName) · \(aufgabe.createdAt.dayString)", systemImage: "calendar")
            }
            if session.permissions.isPrivileged && aufgabe.status != .erledigt {
                Section {
                    Button("Als erledigt markieren", systemImage: "checkmark.circle") {
                        Task { await markDone() }
                    }
                    .requiresOnline()
                }
            }
        }
        .navigationTitle("Aufgabe")
        .navigationBarTitleDisplayMode(.inline)
        .toolbar {
            if session.permissions.isPrivileged {
                ToolbarItem(placement: .topBarTrailing) {
                    Menu("Verwalten", systemImage: "ellipsis") {
                        Button("Bearbeiten", systemImage: "pencil") { showEdit = true }
                        Button("Löschen", systemImage: "trash", role: .destructive) { confirmDelete = true }
                    }
                    .requiresOnline()
                }
            }
        }
        .sheet(isPresented: $showEdit) {
            AufgabeFormView(aufgabe: aufgabe) { await refresh() }
        }
        .confirmationDialog("Aufgabe löschen?", isPresented: $confirmDelete, titleVisibility: .visible) {
            Button("Löschen", role: .destructive) {
                Task {
                    if await action.run({ try await session.client.send(.delete, "aufgaben/\(aufgabe.id)") }) {
                        await onChange()
                        dismiss()
                    }
                }
            }
        }
        .actionAlert(action)
    }

    private func markDone() async {
        let input = AufgabeUpdateInput(
            titel: aufgabe.titel, beschreibung: aufgabe.beschreibung, fotoUrl: aufgabe.fotoUrl,
            priority: aufgabe.priority, assignedUserId: aufgabe.assignedUserId,
            assignedName: aufgabe.assignedName, erledigt: true
        )
        if await action.run({ try await session.client.send(.put, "aufgaben/\(aufgabe.id)", body: input) }) {
            await refresh()
        }
    }

    private func refresh() async {
        if let all = try? await session.fetch([Aufgabe].self, path: "aufgaben").value,
           let fresh = all.first(where: { $0.id == aufgabe.id }) {
            aufgabe = fresh
        }
        await onChange()
    }
}

struct AufgabeFormView: View {
    let aufgabe: Aufgabe?
    let onSaved: () async -> Void

    @Environment(AppSession.self) private var session
    @Environment(\.dismiss) private var dismiss
    @State private var titel: String
    @State private var beschreibung: String
    @State private var fotoUrl: String?
    @State private var priority: AufgabePriority
    @State private var assignedUserId: String?
    @State private var assignedName: String
    @State private var erledigt: Bool
    @State private var users = Remote<[AssignableUser]>("users/assignable")
    @State private var action = ActionRunner()

    init(aufgabe: Aufgabe?, onSaved: @escaping () async -> Void) {
        self.aufgabe = aufgabe
        self.onSaved = onSaved
        _titel = State(initialValue: aufgabe?.titel ?? "")
        _beschreibung = State(initialValue: aufgabe?.beschreibung ?? "")
        _fotoUrl = State(initialValue: aufgabe?.fotoUrl)
        _priority = State(initialValue: AufgabePriority(value: aufgabe?.priority ?? 3))
        _assignedUserId = State(initialValue: aufgabe?.assignedUserId?.lowercased())
        _assignedName = State(initialValue: aufgabe?.assignedUserId == nil ? (aufgabe?.assignedName ?? "") : "")
        _erledigt = State(initialValue: aufgabe?.status == .erledigt)
    }

    var body: some View {
        NavigationStack {
            Form {
                Section {
                    TextField("Titel", text: $titel)
                    TextField("Beschreibung", text: $beschreibung, axis: .vertical).lineLimit(3...8)
                    Picker("Priorität", selection: $priority) {
                        ForEach(AufgabePriority.allCases) { Text($0.label).tag($0) }
                    }
                }
                if aufgabe != nil {
                    Section("Zuständig") {
                        Picker("Mitglied", selection: $assignedUserId) {
                            Text("Niemand / extern").tag(String?.none)
                            ForEach(users.value ?? []) { Text($0.name).tag(Optional($0.id.lowercased())) }
                        }
                        if assignedUserId == nil {
                            TextField("Name (extern, optional)", text: $assignedName)
                        }
                        Toggle("Erledigt", isOn: $erledigt)
                    }
                }
                PhotoUploadSection(uploadPath: "uploads/aufgabe", imageUrl: $fotoUrl, thumbnailUrl: nil)
            }
            .navigationTitle(aufgabe == nil ? "Neue Aufgabe" : "Aufgabe bearbeiten")
            .navigationBarTitleDisplayMode(.inline)
            .task { if aufgabe != nil { await users.load(session) } }
            .toolbar {
                ToolbarItem(placement: .cancellationAction) { Button("Abbrechen", role: .cancel) { dismiss() } }
                ToolbarItem(placement: .confirmationAction) {
                    Button("Sichern", role: .confirm) { Task { await save() } }
                        .disabled(titel.trimmed.isEmpty || action.isRunning)
                        .requiresOnline()
                }
            }
            .actionAlert(action)
        }
    }

    private func save() async {
        let success = await action.run {
            if let aufgabe {
                let input = AufgabeUpdateInput(
                    titel: titel.trimmed, beschreibung: beschreibung.trimmed, fotoUrl: fotoUrl,
                    priority: priority.rawValue, assignedUserId: assignedUserId,
                    assignedName: assignedUserId == nil ? assignedName.nonEmpty : nil, erledigt: erledigt
                )
                try await session.client.send(.put, "aufgaben/\(aufgabe.id)", body: input)
            } else {
                let input = AufgabeCreateInput(titel: titel.trimmed, beschreibung: beschreibung.trimmed, fotoUrl: fotoUrl, priority: priority.rawValue)
                try await session.client.send(.post, "aufgaben", body: input)
            }
        }
        if success {
            await onSaved()
            dismiss()
        }
    }
}
