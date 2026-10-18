import SwiftUI

struct WerkzeugDetailView: View {
    let locationColor: Color?
    let onChange: () async -> Void

    @Environment(AppSession.self) private var session
    @Environment(\.dismiss) private var dismiss
    @Environment(\.openURL) private var openURL
    @State private var item: Werkzeug
    @State private var historie: Remote<[WerkzeugAusleihe]>
    @State private var action = ActionRunner()
    @State private var borrowSheet: AusleihenSheet.Mode?
    @State private var showEdit = false
    @State private var confirmDelete = false

    init(werkzeug: Werkzeug, locationColor: Color?, onChange: @escaping () async -> Void) {
        self.locationColor = locationColor
        self.onChange = onChange
        _item = State(initialValue: werkzeug)
        _historie = State(initialValue: Remote("werkzeug/\(werkzeug.id)/historie"))
    }

    private var permissions: Permissions { session.permissions }
    private var isMyBorrow: Bool { permissions.isOwner(item.borrowedByUserId) }
    private var canManageBorrow: Bool { !item.isAvailable && (isMyBorrow || permissions.isPrivileged) }

    var body: some View {
        List {
            Section {
                RemoteImage(url: item.imageUrl.asURL, contentMode: .fit, placeholderSymbol: "wrench.adjustable")
                    .frame(maxWidth: .infinity, minHeight: 200, maxHeight: 320)
                    .listRowInsets(EdgeInsets())
            }

            Section {
                VStack(alignment: .leading, spacing: 6) {
                    HStack(alignment: .firstTextBaseline) {
                        Text(item.name).font(.title2.bold())
                        Spacer()
                        statusPill
                    }
                    Text(item.category).foregroundStyle(.secondary)
                    if let description = item.description.nonEmpty {
                        Text(description).padding(.top, 4)
                    }
                }
                .padding(.vertical, 4)

                if let dimensions = item.dimensions.nonEmpty {
                    DetailRow(title: "Maße", value: dimensions, systemImage: "ruler")
                }
                if let location = item.storageLocation.nonEmpty {
                    LabeledContent {
                        StorageLocationLabel(name: location, color: locationColor)
                    } label: {
                        Label("Ablageort", systemImage: "mappin.and.ellipse")
                    }
                }
                if let url = item.anleitungFileUrl.asURL {
                    Button {
                        openURL(url)
                    } label: {
                        Label(item.anleitungFileName ?? "Anleitung", systemImage: "doc.text")
                    }
                }
            }

            if !item.isAvailable {
                Section("Ausleihe") {
                    if let name = item.borrowedByName {
                        LabeledContent {
                            HStack(spacing: 6) {
                                Text(name)
                                if isMyBorrow { StatusPill(text: "Du") }
                            }
                        } label: {
                            Label("Ausgeliehen von", systemImage: "person")
                        }
                    }
                    if let since = item.borrowedAt {
                        DetailRow(title: "Seit", value: since.dayString, systemImage: "calendar")
                    }
                    if let until = item.expectedReturnAt {
                        DetailRow(title: "Rückgabe bis", value: until.dayString, systemImage: "calendar.badge.clock")
                            .foregroundStyle(item.isOverdue ? .red : .primary)
                    }
                }
            }

            Section { actionButtons }

            Section("Ausleih-Historie") {
                if let entries = historie.value {
                    if entries.isEmpty {
                        Text("Noch keine Ausleihen erfasst.").foregroundStyle(.secondary)
                    }
                    ForEach(entries) { entry in
                        VStack(alignment: .leading, spacing: 2) {
                            HStack {
                                Text(entry.userName)
                                if permissions.isOwner(entry.userId) { StatusPill(text: "Du") }
                            }
                            Group {
                                if let returned = entry.returnedAt {
                                    Text("\(entry.borrowedAt.dayString) – \(returned.dayString)")
                                } else {
                                    Text("\(entry.borrowedAt.dayString) – \(Text("noch ausgeliehen").foregroundStyle(.tint))")
                                }
                            }
                            .font(.subheadline)
                            .foregroundStyle(.secondary)
                        }
                    }
                } else if historie.failure != nil {
                    Text("Historie nicht verfügbar.").foregroundStyle(.secondary)
                } else {
                    ProgressView()
                }
            }
        }
        .navigationTitle(item.name)
        .navigationBarTitleDisplayMode(.inline)
        .task { await historie.load(session) }
        .refreshable { await historie.load(session) }
        .toolbar {
            if permissions.isPrivileged {
                ToolbarItem(placement: .topBarTrailing) {
                    Menu("Verwalten", systemImage: "ellipsis") {
                        Button("Bearbeiten", systemImage: "pencil") { showEdit = true }
                        Button("Löschen", systemImage: "trash", role: .destructive) { confirmDelete = true }
                    }
                    .requiresOnline()
                }
            }
        }
        .sheet(item: $borrowSheet) { mode in
            AusleihenSheet(werkzeug: item, mode: mode) { updated in
                item = updated
                await historie.load(session)
                await onChange()
            }
        }
        .sheet(isPresented: $showEdit) {
            WerkzeugFormView(werkzeug: item, categories: [item.category], locations: []) { updated in
                if let updated { item = updated }
                await onChange()
            }
        }
        .confirmationDialog("„\(item.name)“ löschen?", isPresented: $confirmDelete, titleVisibility: .visible) {
            Button("Löschen", role: .destructive) {
                Task {
                    if await action.run({ try await session.client.send(.delete, "werkzeug/\(item.id)") }) {
                        await onChange()
                        dismiss()
                    }
                }
            }
        }
        .actionAlert(action)
    }

    private var statusPill: some View {
        Group {
            if item.isAvailable {
                StatusPill(text: "Verfügbar", color: .green, systemImage: "checkmark.circle.fill")
            } else if item.isOverdue {
                StatusPill(text: "Überfällig", color: .red, systemImage: "exclamationmark.circle.fill")
            } else {
                StatusPill(text: "Ausgeliehen", color: .orange, systemImage: "clock.fill")
            }
        }
    }

    @ViewBuilder
    private var actionButtons: some View {
        if item.isAvailable {
            Button {
                borrowSheet = .borrow
            } label: {
                Label("Ausleihen", systemImage: "hand.raised.fill")
            }
            .requiresOnline()
        } else if canManageBorrow {
            Button {
                borrowSheet = .changeDate
            } label: {
                Label("Rückgabedatum ändern", systemImage: "calendar.badge.clock")
            }
            .requiresOnline()

            Button {
                Task { await returnItem() }
            } label: {
                HStack {
                    Label("Zurückgeben", systemImage: "arrow.uturn.backward.circle.fill")
                    if action.isRunning { Spacer(); ProgressView() }
                }
            }
            .disabled(action.isRunning)
            .requiresOnline()
        } else {
            Label("Nur die ausleihende Person oder der Vorstand kann das Werkzeug zurückgeben.", systemImage: "info.circle")
                .font(.footnote)
                .foregroundStyle(.secondary)
        }
    }

    private func returnItem() async {
        await action.run {
            item = try await session.client.send(.post, "werkzeug/\(item.id)/zurueckgeben", decoding: Werkzeug.self)
        }
        await historie.load(session)
        await onChange()
    }
}

struct AusleihenSheet: View {
    enum Mode: String, Identifiable {
        case borrow, changeDate
        var id: String { rawValue }
    }

    let werkzeug: Werkzeug
    let mode: Mode
    let onDone: (Werkzeug) async -> Void

    @Environment(AppSession.self) private var session
    @Environment(\.dismiss) private var dismiss
    @State private var returnDate: Date
    @State private var action = ActionRunner()

    private let tomorrow = Calendar.current.date(byAdding: .day, value: 1, to: .now.startOfDay) ?? .now

    init(werkzeug: Werkzeug, mode: Mode, onDone: @escaping (Werkzeug) async -> Void) {
        self.werkzeug = werkzeug
        self.mode = mode
        self.onDone = onDone
        let initial = mode == .changeDate ? werkzeug.expectedReturnAt?.localDayFromUTCMidnight : nil
        _returnDate = State(initialValue: initial ?? Calendar.current.date(byAdding: .day, value: 1, to: .now.startOfDay) ?? .now)
    }

    var body: some View {
        NavigationStack {
            Form {
                Section {
                    DatePicker("Rückgabe bis", selection: $returnDate, in: tomorrow..., displayedComponents: .date)
                        .datePickerStyle(.graphical)
                } footer: {
                    Text(mode == .borrow
                         ? "Bitte gib das Werkzeug spätestens an diesem Tag zurück."
                         : "Das neue Datum ist für alle sichtbar.")
                }
            }
            .navigationTitle(mode == .borrow ? "\(werkzeug.name) ausleihen" : "Rückgabedatum ändern")
            .navigationBarTitleDisplayMode(.inline)
            .toolbar {
                ToolbarItem(placement: .cancellationAction) {
                    Button("Abbrechen", role: .cancel) { dismiss() }
                }
                ToolbarItem(placement: .confirmationAction) {
                    Button(mode == .borrow ? "Ausleihen" : "Speichern", role: .confirm) {
                        Task { await submit() }
                    }
                    .disabled(action.isRunning)
                    .requiresOnline()
                }
            }
            .actionAlert(action)
        }
        .presentationDetents([.large])
    }

    private struct ReturnDateBody: Encodable { let expectedReturnAt: Date }

    private func submit() async {
        let body = ReturnDateBody(expectedReturnAt: returnDate.utcMidnightOfLocalDay)
        var updated: Werkzeug?
        let success = await action.run {
            updated = mode == .borrow
                ? try await session.client.send(.post, "werkzeug/\(werkzeug.id)/ausleihen", body: body, decoding: Werkzeug.self)
                : try await session.client.send(.patch, "werkzeug/\(werkzeug.id)/rueckgabedatum", body: body, decoding: Werkzeug.self)
        }
        if success, let updated {
            await onDone(updated)
            dismiss()
        }
    }
}
