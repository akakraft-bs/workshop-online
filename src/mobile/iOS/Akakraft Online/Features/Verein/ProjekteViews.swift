import SwiftUI
import UniformTypeIdentifiers

struct ProjekteListView: View {
    @Environment(AppSession.self) private var session
    @State private var remote = Remote<[Projekt]>("verein/projekte")
    @State private var showCreate = false

    var body: some View {
        RemoteContent(remote: remote) { items in
            List {
                if items.isEmpty {
                    ContentUnavailableView("Keine Projekte", systemImage: "hammer")
                }
                ForEach(ProjektStatus.selectable) { status in
                    let projekte = items.filter { $0.status == status }.sorted { $0.plannedStartDate < $1.plannedStartDate }
                    if !projekte.isEmpty {
                        Section(status.label) {
                            ForEach(projekte) { projekt in
                                NavigationLink(value: projekt) { ProjektRow(projekt: projekt) }
                            }
                        }
                    }
                }
                Section {} footer: { LastUpdatedFooter(date: remote.lastUpdated) }
            }
        }
        .refreshable { await remote.load(session) }
        .task { await remote.load(session) }
        .navigationTitle("Projekte")
        .navigationDestination(for: Projekt.self) { projekt in
            ProjektDetailView(projekt: projekt) { await remote.load(session) }
        }
        .toolbar {
            if session.permissions.isPrivileged {
                ToolbarItem(placement: .topBarTrailing) {
                    Button("Neues Projekt", systemImage: "plus") { showCreate = true }
                        .requiresOnline()
                }
            }
        }
        .sheet(isPresented: $showCreate) {
            ProjektFormView(projekt: nil) { await remote.load(session) }
        }
    }
}

struct ProjektRow: View {
    let projekt: Projekt

    var body: some View {
        HStack(spacing: 12) {
            Image(systemName: projekt.status.symbol)
                .foregroundStyle(projekt.status.color)
                .font(.title3)
                .frame(width: 28)
            VStack(alignment: .leading, spacing: 3) {
                Text(projekt.name).font(.headline)
                Text("\((projekt.actualStartDate ?? projekt.plannedStartDate).dayString) – \((projekt.actualEndDate ?? projekt.expectedEndDate).dayString)")
                    .font(.subheadline)
                    .foregroundStyle(.secondary)
            }
        }
    }
}

struct ProjektDetailView: View {
    let onChange: () async -> Void

    @Environment(AppSession.self) private var session
    @Environment(\.dismiss) private var dismiss
    @State private var projekt: Projekt
    @State private var showEdit = false
    @State private var confirmDelete = false
    @State private var action = ActionRunner()

    init(projekt: Projekt, onChange: @escaping () async -> Void) {
        self.onChange = onChange
        _projekt = State(initialValue: projekt)
    }

    private var progress: Double? {
        guard projekt.status == .gestartet else { return projekt.status == .abgeschlossen ? 1 : nil }
        let start = projekt.actualStartDate ?? projekt.plannedStartDate
        let total = projekt.expectedEndDate.timeIntervalSince(start)
        guard total > 0 else { return nil }
        return min(max(Date.now.timeIntervalSince(start) / total, 0), 1)
    }

    var body: some View {
        List {
            Section {
                VStack(alignment: .leading, spacing: 8) {
                    StatusPill(text: projekt.status.label, color: projekt.status.color, systemImage: projekt.status.symbol)
                    Text(projekt.name).font(.title2.bold())
                    if let description = projekt.description.nonEmpty { Text(description) }
                    if let progress {
                        ProgressView(value: progress) {
                            Text("Zeitlicher Fortschritt").font(.caption)
                        }
                        .tint(projekt.status.color)
                    }
                }
                .padding(.vertical, 4)
            }
            Section("Zeitplan") {
                DetailRow(title: "Geplanter Start", value: projekt.plannedStartDate.dayString, systemImage: "calendar")
                DetailRow(title: "Dauer", value: "\(projekt.durationWeeks) Wochen", systemImage: "hourglass")
                if let actual = projekt.actualStartDate {
                    DetailRow(title: "Tatsächlicher Start", value: actual.dayString, systemImage: "play.circle")
                }
                DetailRow(title: "Erwartetes Ende", value: projekt.expectedEndDate.dayString, systemImage: "flag.checkered")
                if let end = projekt.actualEndDate {
                    DetailRow(title: "Abgeschlossen", value: end.dayString, systemImage: "checkmark.circle")
                }
            }
            if let url = projekt.projektplanUrl.asURL {
                Section {
                    Link(destination: url) { Label("Projektplan öffnen", systemImage: "doc.richtext") }
                }
            }
            Section {
                DetailRow(title: "Angelegt von", value: "\(projekt.createdByName) · \(projekt.createdAt.dayString)", systemImage: "person")
            }
        }
        .navigationTitle("Projekt")
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
            ProjektFormView(projekt: projekt) {
                if let all = try? await session.fetch([Projekt].self, path: "verein/projekte").value,
                   let fresh = all.first(where: { $0.id == projekt.id }) {
                    projekt = fresh
                }
                await onChange()
            }
        }
        .confirmationDialog("Projekt löschen?", isPresented: $confirmDelete, titleVisibility: .visible) {
            Button("Löschen", role: .destructive) {
                Task {
                    if await action.run({ try await session.client.send(.delete, "verein/projekte/\(projekt.id)") }) {
                        await onChange()
                        dismiss()
                    }
                }
            }
        }
        .actionAlert(action)
    }
}

struct ProjektFormView: View {
    let projekt: Projekt?
    let onSaved: () async -> Void

    @Environment(AppSession.self) private var session
    @Environment(\.dismiss) private var dismiss
    @State private var input: ProjektInput
    @State private var hasActualStart: Bool
    @State private var showImporter = false
    @State private var isUploading = false
    @State private var action = ActionRunner()

    init(projekt: Projekt?, onSaved: @escaping () async -> Void) {
        self.projekt = projekt
        self.onSaved = onSaved
        _input = State(initialValue: ProjektInput(
            name: projekt?.name ?? "", description: projekt?.description,
            plannedStartDate: projekt?.plannedStartDate.localDayFromUTCMidnight ?? .now,
            durationWeeks: projekt?.durationWeeks ?? 4,
            actualStartDate: projekt?.actualStartDate?.localDayFromUTCMidnight,
            status: projekt?.status ?? .geplant, projektplanUrl: projekt?.projektplanUrl
        ))
        _hasActualStart = State(initialValue: projekt?.actualStartDate != nil)
    }

    var body: some View {
        NavigationStack {
            Form {
                Section {
                    TextField("Name", text: $input.name)
                    TextField("Beschreibung", text: optional($input.description), axis: .vertical).lineLimit(3...8)
                    Picker("Status", selection: $input.status) {
                        ForEach(ProjektStatus.selectable) { Text($0.label).tag($0) }
                    }
                }
                Section("Zeitplan") {
                    DatePicker("Geplanter Start", selection: $input.plannedStartDate, displayedComponents: .date)
                    Stepper("Dauer: \(input.durationWeeks) Wochen", value: $input.durationWeeks, in: 1...260)
                    Toggle("Bereits gestartet", isOn: $hasActualStart)
                    if hasActualStart {
                        DatePicker("Tatsächlicher Start", selection: Binding(
                            get: { input.actualStartDate ?? .now }, set: { input.actualStartDate = $0 }
                        ), displayedComponents: .date)
                    }
                }
                Section("Projektplan") {
                    if let url = input.projektplanUrl.asURL {
                        Link(destination: url) { Label(url.lastPathComponent, systemImage: "doc.richtext") }
                        Button("Entfernen", systemImage: "trash", role: .destructive) { input.projektplanUrl = nil }
                    }
                    Button(input.projektplanUrl == nil ? "Datei hochladen" : "Datei ersetzen", systemImage: "square.and.arrow.up") {
                        showImporter = true
                    }
                    .requiresOnline()
                    if isUploading { ProgressView() }
                }
            }
            .navigationTitle(projekt == nil ? "Neues Projekt" : "Projekt bearbeiten")
            .navigationBarTitleDisplayMode(.inline)
            .fileImporter(isPresented: $showImporter, allowedContentTypes: [.pdf, .image, .spreadsheet, .item]) { result in
                if case .success(let url) = result { Task { await upload(url) } }
            }
            .toolbar {
                ToolbarItem(placement: .cancellationAction) { Button("Abbrechen", role: .cancel) { dismiss() } }
                ToolbarItem(placement: .confirmationAction) {
                    Button("Sichern", role: .confirm) { Task { await save() } }
                        .disabled(input.name.trimmed.isEmpty || action.isRunning || isUploading)
                        .requiresOnline()
                }
            }
            .actionAlert(action)
        }
    }

    private func upload(_ fileURL: URL) async {
        isUploading = true
        defer { isUploading = false }
        await action.run {
            let accessed = fileURL.startAccessingSecurityScopedResource()
            defer { if accessed { fileURL.stopAccessingSecurityScopedResource() } }
            let data = try Data(contentsOf: fileURL)
            let mime = UTType(filenameExtension: fileURL.pathExtension)?.preferredMIMEType ?? "application/octet-stream"
            let response = try await session.client.upload("uploads/projektplan", fileData: data, fileName: fileURL.lastPathComponent, mimeType: mime)
            input.projektplanUrl = try session.client.decode(FileUploadResult.self, from: response).resolvedURL
        }
    }

    private func save() async {
        var payload = input
        payload.name = input.name.trimmed
        payload.description = input.description.nonEmpty
        payload.plannedStartDate = input.plannedStartDate.utcMidnightOfLocalDay
        payload.actualStartDate = hasActualStart ? (input.actualStartDate ?? .now).utcMidnightOfLocalDay : nil
        let success = await action.run {
            if let projekt {
                try await session.client.send(.put, "verein/projekte/\(projekt.id)", body: payload)
            } else {
                try await session.client.send(.post, "verein/projekte", body: payload)
            }
        }
        if success {
            await onSaved()
            dismiss()
        }
    }
}
