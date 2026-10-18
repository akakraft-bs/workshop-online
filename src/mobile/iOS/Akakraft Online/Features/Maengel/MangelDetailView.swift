import SwiftUI

struct MangelDetailView: View {
    let onChange: () async -> Void

    @Environment(AppSession.self) private var session
    @State private var mangel: Mangel
    @State private var action = ActionRunner()
    @State private var newComment = ""
    @State private var showEdit = false
    @State private var showStatus = false
    @State private var confirmWithdraw = false
    @State private var editingComment: MangelAnmerkung?
    @FocusState private var commentFocused: Bool

    init(mangel: Mangel, onChange: @escaping () async -> Void) {
        self.onChange = onChange
        _mangel = State(initialValue: mangel)
    }

    private var permissions: Permissions { session.permissions }
    private var isOwner: Bool { permissions.isOwner(mangel.createdByUserId) }

    var body: some View {
        List {
            if let url = mangel.imageUrl.asURL {
                Section {
                    RemoteImage(url: url, contentMode: .fit)
                        .frame(maxWidth: .infinity, minHeight: 200, maxHeight: 360)
                        .listRowInsets(EdgeInsets())
                }
            }

            Section {
                VStack(alignment: .leading, spacing: 8) {
                    HStack {
                        StatusPill(text: mangel.status.label, color: mangel.status.color)
                        StatusPill(text: mangel.kategorie.label, color: .secondary, systemImage: mangel.kategorie.symbol)
                    }
                    Text(mangel.title).font(.title2.bold())
                    if let description = mangel.description.nonEmpty {
                        Text(description)
                    }
                    Text("Gemeldet von \(mangel.createdByName) am \(mangel.createdAt.dayTimeString)")
                        .font(.footnote)
                        .foregroundStyle(.secondary)
                }
                .padding(.vertical, 4)
            }

            if mangel.resolvedByName != nil || mangel.note.nonEmpty != nil {
                Section("Bearbeitung") {
                    if let name = mangel.resolvedByName {
                        DetailRow(title: "Bearbeitet von", value: name, systemImage: "person.badge.shield.checkmark")
                    }
                    if let date = mangel.resolvedAt {
                        DetailRow(title: "Am", value: date.dayTimeString, systemImage: "calendar")
                    }
                    if let note = mangel.note.nonEmpty {
                        Text(note)
                    }
                }
            }

            Section("Anmerkungen") {
                if mangel.anmerkungen.isEmpty {
                    Text("Noch keine Anmerkungen.").foregroundStyle(.secondary)
                }
                ForEach(mangel.anmerkungen.sorted { $0.createdAt < $1.createdAt }) { anmerkung in
                    AnmerkungRow(anmerkung: anmerkung)
                        .swipeActions {
                            if permissions.isOwnerOrPrivileged(anmerkung.createdByUserId) {
                                Button("Löschen", systemImage: "trash", role: .destructive) {
                                    Task { await deleteComment(anmerkung) }
                                }
                                Button("Bearbeiten", systemImage: "pencil") { editingComment = anmerkung }
                                    .tint(.blue)
                            }
                        }
                }
                HStack(alignment: .bottom) {
                    TextField("Anmerkung schreiben …", text: $newComment, axis: .vertical)
                        .lineLimit(1...5)
                        .focused($commentFocused)
                    Button("Senden", systemImage: "arrow.up.circle.fill") {
                        Task { await addComment() }
                    }
                    .labelStyle(.iconOnly)
                    .font(.title2)
                    .disabled(newComment.trimmed.isEmpty || action.isRunning)
                }
                .requiresOnline()
            }
        }
        .navigationTitle("Mangel")
        .navigationBarTitleDisplayMode(.inline)
        .toolbar {
            if permissions.isPrivileged || isOwner {
                ToolbarItem(placement: .topBarTrailing) {
                    Menu("Aktionen", systemImage: "ellipsis") {
                        if permissions.isPrivileged {
                            Button("Status ändern", systemImage: "checklist") { showStatus = true }
                        }
                        if permissions.isOwnerOrPrivileged(mangel.createdByUserId) {
                            Button("Bearbeiten", systemImage: "pencil") { showEdit = true }
                        }
                        if isOwner && mangel.status.isActive {
                            Button("Zurückziehen", systemImage: "arrow.uturn.backward", role: .destructive) {
                                confirmWithdraw = true
                            }
                        }
                    }
                    .requiresOnline()
                }
            }
        }
        .sheet(isPresented: $showEdit) {
            MangelFormView(mangel: mangel) { updated in
                if let updated { mangel = updated }
                await onChange()
            }
        }
        .sheet(isPresented: $showStatus) {
            MangelStatusSheet(mangel: mangel) { updated in
                mangel = updated
                await onChange()
            }
        }
        .sheet(item: $editingComment) { anmerkung in
            AnmerkungEditSheet(mangelId: mangel.id, anmerkung: anmerkung) { updated in
                mangel = updated
                await onChange()
            }
        }
        .confirmationDialog("Mangel zurückziehen?", isPresented: $confirmWithdraw, titleVisibility: .visible) {
            Button("Zurückziehen", role: .destructive) {
                Task {
                    await action.run {
                        mangel = try await session.client.send(.post, "mangel/\(mangel.id)/zurueckziehen", decoding: Mangel.self)
                    }
                    await onChange()
                }
            }
        } message: {
            Text("Der Mangel wird als zurückgezogen markiert.")
        }
        .actionAlert(action)
    }

    private struct TextBody: Encodable { let text: String }

    private func addComment() async {
        let text = newComment.trimmed
        let success = await action.run {
            try await session.client.send(.post, "mangel/\(mangel.id)/anmerkungen", body: TextBody(text: text))
        }
        if success {
            newComment = ""
            commentFocused = false
            await reloadMangel()
        }
    }

    private func deleteComment(_ anmerkung: MangelAnmerkung) async {
        if await action.run({ try await session.client.send(.delete, "mangel/\(mangel.id)/anmerkungen/\(anmerkung.id)") }) {
            await reloadMangel()
        }
    }

    private func reloadMangel() async {
        if let all = try? await session.fetch([Mangel].self, path: "mangel").value,
           let fresh = all.first(where: { $0.id == mangel.id }) {
            mangel = fresh
        }
        await onChange()
    }
}

private struct AnmerkungRow: View {
    let anmerkung: MangelAnmerkung

    var body: some View {
        VStack(alignment: .leading, spacing: 4) {
            HStack {
                Text(anmerkung.createdByName).font(.subheadline.weight(.semibold))
                Spacer()
                Text(anmerkung.createdAt.relativeString).font(.caption).foregroundStyle(.secondary)
            }
            Text(anmerkung.text)
            if anmerkung.updatedAt != nil {
                Text("bearbeitet").font(.caption2).foregroundStyle(.tertiary)
            }
        }
        .padding(.vertical, 2)
    }
}

private struct AnmerkungEditSheet: View {
    let mangelId: String
    let anmerkung: MangelAnmerkung
    let onSaved: (Mangel) async -> Void

    @Environment(AppSession.self) private var session
    @Environment(\.dismiss) private var dismiss
    @State private var text: String
    @State private var action = ActionRunner()

    init(mangelId: String, anmerkung: MangelAnmerkung, onSaved: @escaping (Mangel) async -> Void) {
        self.mangelId = mangelId
        self.anmerkung = anmerkung
        self.onSaved = onSaved
        _text = State(initialValue: anmerkung.text)
    }

    private struct TextBody: Encodable { let text: String }

    var body: some View {
        NavigationStack {
            Form {
                TextField("Anmerkung", text: $text, axis: .vertical).lineLimit(3...10)
            }
            .navigationTitle("Anmerkung bearbeiten")
            .navigationBarTitleDisplayMode(.inline)
            .toolbar {
                ToolbarItem(placement: .cancellationAction) { Button("Abbrechen", role: .cancel) { dismiss() } }
                ToolbarItem(placement: .confirmationAction) {
                    Button("Sichern", role: .confirm) {
                        Task {
                            let success = await action.run {
                                try await session.client.send(.put, "mangel/\(mangelId)/anmerkungen/\(anmerkung.id)", body: TextBody(text: text.trimmed))
                            }
                            if success,
                               let all = try? await session.fetch([Mangel].self, path: "mangel").value,
                               let fresh = all.first(where: { $0.id == mangelId }) {
                                await onSaved(fresh)
                            }
                            if success { dismiss() }
                        }
                    }
                    .disabled(text.trimmed.isEmpty || action.isRunning)
                    .requiresOnline()
                }
            }
            .actionAlert(action)
        }
        .presentationDetents([.medium])
    }
}

private struct MangelStatusSheet: View {
    let mangel: Mangel
    let onSaved: (Mangel) async -> Void

    @Environment(AppSession.self) private var session
    @Environment(\.dismiss) private var dismiss
    @State private var status: MangelStatus
    @State private var note: String
    @State private var action = ActionRunner()

    init(mangel: Mangel, onSaved: @escaping (Mangel) async -> Void) {
        self.mangel = mangel
        self.onSaved = onSaved
        _status = State(initialValue: mangel.status == .zurueckgezogen ? .offen : mangel.status)
        _note = State(initialValue: mangel.note ?? "")
    }

    private struct StatusBody: Encodable {
        let status: MangelStatus
        let note: String?
    }

    var body: some View {
        NavigationStack {
            Form {
                Picker("Status", selection: $status) {
                    ForEach(MangelStatus.settableByManagers) { Text($0.label).tag($0) }
                }
                .pickerStyle(.inline)
                Section("Notiz") {
                    TextField("Optional – z. B. was erledigt wurde", text: $note, axis: .vertical).lineLimit(2...6)
                }
            }
            .navigationTitle("Status ändern")
            .navigationBarTitleDisplayMode(.inline)
            .toolbar {
                ToolbarItem(placement: .cancellationAction) { Button("Abbrechen", role: .cancel) { dismiss() } }
                ToolbarItem(placement: .confirmationAction) {
                    Button("Sichern", role: .confirm) {
                        Task {
                            var updated: Mangel?
                            let success = await action.run {
                                updated = try await session.client.send(
                                    .patch, "mangel/\(mangel.id)/status",
                                    body: StatusBody(status: status, note: note.nonEmpty), decoding: Mangel.self
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
    }
}

struct MangelFormView: View {
    let mangel: Mangel?
    let onSaved: (Mangel?) async -> Void

    @Environment(AppSession.self) private var session
    @Environment(\.dismiss) private var dismiss
    @State private var input: MangelInput
    @State private var action = ActionRunner()

    init(mangel: Mangel?, onSaved: @escaping (Mangel?) async -> Void) {
        self.mangel = mangel
        self.onSaved = onSaved
        _input = State(initialValue: MangelInput(
            title: mangel?.title ?? "",
            description: mangel?.description ?? "",
            kategorie: mangel?.kategorie ?? .halle,
            imageUrl: mangel?.imageUrl
        ))
    }

    var body: some View {
        NavigationStack {
            Form {
                Section {
                    TextField("Was ist kaputt?", text: $input.title)
                    TextField("Beschreibung", text: $input.description, axis: .vertical).lineLimit(3...8)
                    Picker("Kategorie", selection: $input.kategorie) {
                        ForEach(MangelKategorie.selectable) { Label($0.label, systemImage: $0.symbol).tag($0) }
                    }
                }
                PhotoUploadSection(uploadPath: "uploads/mangel", imageUrl: $input.imageUrl, thumbnailUrl: nil)
            }
            .navigationTitle(mangel == nil ? "Mangel melden" : "Mangel bearbeiten")
            .navigationBarTitleDisplayMode(.inline)
            .toolbar {
                ToolbarItem(placement: .cancellationAction) { Button("Abbrechen", role: .cancel) { dismiss() } }
                ToolbarItem(placement: .confirmationAction) {
                    Button(mangel == nil ? "Melden" : "Sichern", role: .confirm) { Task { await save() } }
                        .disabled(input.title.trimmed.isEmpty || action.isRunning)
                        .requiresOnline()
                }
            }
            .actionAlert(action)
        }
    }

    private func save() async {
        var payload = input
        payload.title = input.title.trimmed
        payload.description = input.description.trimmed
        var saved: Mangel?
        let success = await action.run {
            saved = if let mangel {
                try await session.client.send(.patch, "mangel/\(mangel.id)", body: payload, decoding: Mangel.self)
            } else {
                try await session.client.send(.post, "mangel", body: payload, decoding: Mangel.self)
            }
        }
        if success {
            await onSaved(saved)
            dismiss()
        }
    }
}
