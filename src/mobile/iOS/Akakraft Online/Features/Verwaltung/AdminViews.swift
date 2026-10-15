import SwiftUI

struct AdminView: View {
    @Environment(AppSession.self) private var session
    @State private var users = Remote<[User]>("users")

    var body: some View {
        List {
            Section {
                NavigationLink { AdminUsersView() } label: { Label("Nutzerverwaltung", systemImage: "person.2.badge.gearshape") }
                    .badge(users.value?.withoutAccessCount ?? 0)
                NavigationLink { AdminFeedbackView() } label: { Label("Feedback", systemImage: "bubble.left.and.bubble.right") }
                NavigationLink { AblageorteView() } label: { Label("Ablageorte", systemImage: "mappin.and.ellipse") }
                NavigationLink { PushTestView() } label: { Label("Test-Benachrichtigung", systemImage: "bell.badge") }
                NavigationLink { WartungView() } label: { Label("Wartung", systemImage: "wrench.and.screwdriver") }
            }
            Section {
                Link(destination: session.server.webURL(route: "admin/kalender")) {
                    Label("Kalender-Einstellungen", systemImage: "calendar.badge.plus")
                }
                Link(destination: session.server.webURL(route: "admin/parkplatz")) {
                    Label("Parkkonten & Portal-Zugang", systemImage: "parkingsign")
                }
            } header: {
                Text("In der Web-App")
            } footer: {
                Text("Diese Einstellungen werden selten geändert und sind am Computer übersichtlicher.")
            }
        }
        .navigationTitle("Adminbereich")
        .navigationBarTitleDisplayMode(.inline)
        .task { await users.load(session) }
    }
}

extension [User] {
    /// Nutzer ohne Rolle („Kein Zugriff“), die auf Freischaltung warten.
    var withoutAccessCount: Int { filter { !Permissions(user: $0).isMember }.count }
}

// MARK: - Nutzer

struct AdminUsersView: View {
    @Environment(AppSession.self) private var session
    @State private var remote = Remote<[User]>("users")
    @State private var searchText = ""
    @State private var onlyPending = false

    var body: some View {
        RemoteContent(remote: remote) { users in
            let visible = users
                .filter { !onlyPending || !Permissions(user: $0).isMember }
                .filter { searchText.isEmpty || $0.shownName.localizedCaseInsensitiveContains(searchText) || $0.email.localizedCaseInsensitiveContains(searchText) }
                .sorted { lhs, rhs in
                    // Nutzer ohne Zugriff zuerst, damit offene Freischaltungen sofort auffallen.
                    let lhsPending = !Permissions(user: lhs).isMember
                    let rhsPending = !Permissions(user: rhs).isMember
                    if lhsPending != rhsPending { return lhsPending }
                    return lhs.shownName.localizedStandardCompare(rhs.shownName) == .orderedAscending
                }
            List {
                ForEach(visible) { user in
                    NavigationLink(value: user) {
                        HStack(spacing: 12) {
                            AvatarView(name: user.shownName, pictureURL: user.pictureUrl.asURL)
                            VStack(alignment: .leading, spacing: 2) {
                                Text(user.shownName).font(.headline)
                                Text(user.roles.filter { $0 != .none }.map(\.label).joined(separator: ", ").nonEmpty ?? "Kein Zugriff")
                                    .font(.caption)
                                    .foregroundStyle(Permissions(user: user).isMember ? Color.secondary : Color.orange)
                            }
                        }
                    }
                }
                Section {} footer: { LastUpdatedFooter(date: remote.lastUpdated) }
            }
        }
        .searchable(text: $searchText, prompt: "Name oder E-Mail")
        .refreshable { await remote.load(session) }
        .task { await remote.load(session) }
        .navigationTitle("Nutzer")
        .navigationBarTitleDisplayMode(.inline)
        .navigationDestination(for: User.self) { user in
            AdminUserDetailView(user: user) { await remote.load(session) }
        }
        .toolbar {
            ToolbarItem(placement: .topBarTrailing) {
                Toggle(isOn: $onlyPending) {
                    Label("Nur ohne Freischaltung", systemImage: "person.badge.clock")
                }
                .toggleStyle(.button)
            }
        }
    }
}

private struct AdminUserDetailView: View {
    let onChange: () async -> Void

    @Environment(AppSession.self) private var session
    @Environment(\.dismiss) private var dismiss
    @State private var user: User
    @State private var roles: Set<Role>
    @State private var action = ActionRunner()
    @State private var confirmDelete = false

    init(user: User, onChange: @escaping () async -> Void) {
        self.onChange = onChange
        _user = State(initialValue: user)
        _roles = State(initialValue: Set(user.roles))
    }

    var body: some View {
        Form {
            Section {
                HStack(spacing: 14) {
                    AvatarView(name: user.shownName, pictureURL: user.pictureUrl.asURL, size: 52)
                    VStack(alignment: .leading) {
                        Text(user.shownName).font(.title3.bold())
                        Text(user.email).foregroundStyle(.secondary)
                        Text("Registriert am \(user.createdAt.dayString)").font(.caption).foregroundStyle(.secondary)
                    }
                }
            }
            Section("Rollen") {
                ForEach(Role.assignable, id: \.self) { role in
                    Toggle(role.label, isOn: Binding(
                        get: { roles.contains(role) },
                        set: { enabled in Task { await setRole(role, enabled: enabled) } }
                    ))
                    .disabled(action.isRunning)
                }
            }
            .requiresOnline()
            if !session.permissions.isOwner(user.id) {
                Section {
                    Button("Nutzer löschen", systemImage: "person.crop.circle.badge.xmark", role: .destructive) { confirmDelete = true }
                        .requiresOnline()
                }
            }
        }
        .navigationTitle(user.shownName)
        .navigationBarTitleDisplayMode(.inline)
        .confirmationDialog("\(user.shownName) löschen?", isPresented: $confirmDelete, titleVisibility: .visible) {
            Button("Endgültig löschen", role: .destructive) {
                Task {
                    if await action.run({ try await session.client.send(.delete, "users/\(user.id)") }) {
                        await onChange()
                        dismiss()
                    }
                }
            }
        } message: {
            Text("Das Konto und alle persönlichen Daten werden entfernt.")
        }
        .actionAlert(action)
    }

    private func setRole(_ role: Role, enabled: Bool) async {
        let success = await action.run {
            try await session.client.send(enabled ? .post : .delete, "users/\(user.id)/roles/\(role.rawValue)")
        }
        if success {
            if enabled { roles.insert(role) } else { roles.remove(role) }
            await onChange()
        }
    }
}

// MARK: - Feedback

struct AdminFeedbackView: View {
    @Environment(AppSession.self) private var session
    @State private var remote = Remote<[Feedback]>("admin/feedback")
    @State private var action = ActionRunner()

    private struct StatusBody: Encodable { let status: FeedbackStatus }

    var body: some View {
        RemoteContent(remote: remote) { items in
            List {
                if items.isEmpty { ContentUnavailableView("Kein Feedback", systemImage: "tray") }
                ForEach(items.sorted { $0.createdAt > $1.createdAt }) { feedback in
                    VStack(alignment: .leading, spacing: 6) {
                        HStack {
                            Text(feedback.userName).font(.headline)
                            Spacer()
                            Menu {
                                ForEach(FeedbackStatus.selectable) { status in
                                    Button(status.label) { Task { await setStatus(feedback, status) } }
                                }
                            } label: {
                                StatusPill(text: feedback.status.label, color: feedback.status.color)
                            }
                            .requiresOnline()
                        }
                        Text(feedback.text)
                        Text([feedback.createdAt.dayTimeString, feedback.pageUrl, feedback.appVersion].compactMap { $0.nonEmpty }.joined(separator: " · "))
                            .font(.caption)
                            .foregroundStyle(.secondary)
                    }
                    .padding(.vertical, 2)
                }
            }
        }
        .refreshable { await remote.load(session) }
        .task { await remote.load(session) }
        .navigationTitle("Feedback")
        .navigationBarTitleDisplayMode(.inline)
        .actionAlert(action)
    }

    private func setStatus(_ feedback: Feedback, _ status: FeedbackStatus) async {
        if await action.run({ try await session.client.send(.patch, "admin/feedback/\(feedback.id)/status", body: StatusBody(status: status)) }) {
            await remote.load(session)
        }
    }
}

// MARK: - Ablageorte

struct AblageorteView: View {
    @Environment(AppSession.self) private var session
    @State private var remote = Remote<[AblageortOverview]>("admin/ablageorte")
    @State private var editing: AblageortOverview?
    @State private var action = ActionRunner()

    var body: some View {
        RemoteContent(remote: remote) { items in
            List {
                ForEach(items.sorted { $0.name.localizedStandardCompare($1.name) == .orderedAscending }, id: \.stableId) { ort in
                    Button {
                        editing = ort
                    } label: {
                        HStack {
                            StorageLocationLabel(name: ort.name, color: Color(hex: ort.color)).foregroundStyle(Color.primary)
                            Spacer()
                            Text("\(ort.itemCount)").foregroundStyle(.secondary)
                        }
                    }
                    .swipeActions {
                        if let id = ort.id {
                            Button("Löschen", systemImage: "trash", role: .destructive) {
                                Task {
                                    if await action.run({ try await session.client.send(.delete, "admin/ablageorte/\(id)") }) {
                                        await remote.load(session)
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
        .refreshable { await remote.load(session) }
        .task { await remote.load(session) }
        .navigationTitle("Ablageorte")
        .navigationBarTitleDisplayMode(.inline)
        .sheet(item: Binding(
            get: { editing.map(IdentifiedOrt.init) },
            set: { editing = $0?.ort }
        )) { item in
            AblageortEditSheet(ort: item.ort) { await remote.load(session) }
        }
        .actionAlert(action)
    }

    private struct IdentifiedOrt: Identifiable {
        let ort: AblageortOverview
        var id: String { ort.stableId }
    }
}

private struct AblageortEditSheet: View {
    let ort: AblageortOverview
    let onSaved: () async -> Void

    @Environment(AppSession.self) private var session
    @State private var name: String
    @State private var color: Color
    @State private var hasColor: Bool
    @State private var action = ActionRunner()

    init(ort: AblageortOverview, onSaved: @escaping () async -> Void) {
        self.ort = ort
        self.onSaved = onSaved
        _name = State(initialValue: ort.name)
        _color = State(initialValue: Color(hex: ort.color) ?? .blue)
        _hasColor = State(initialValue: ort.color != nil)
    }

    private struct UpdateBody: Encodable { let name: String; let color: String? }
    private struct RenameBody: Encodable { let currentName: String; let newName: String; let color: String? }

    var body: some View {
        FormSheet(title: "Ablageort", canSave: !name.trimmed.isEmpty, action: action) {
            let hex = hasColor ? color.hexString : nil
            if let id = ort.id {
                try await session.client.send(.put, "admin/ablageorte/\(id)", body: UpdateBody(name: name.trimmed, color: hex))
            } else {
                try await session.client.send(.post, "admin/ablageorte/rename-from-name",
                                              body: RenameBody(currentName: ort.name, newName: name.trimmed, color: hex))
            }
            await onSaved()
        } content: {
            TextField("Name", text: $name)
            Toggle("Farbe", isOn: $hasColor)
            if hasColor {
                ColorPicker("Farbe wählen", selection: $color, supportsOpacity: false)
            }
            Text("Wird von \(ort.itemCount) Einträgen verwendet. Umbenennen ändert alle Einträge.")
                .font(.footnote)
                .foregroundStyle(.secondary)
        }
        .presentationDetents([.medium])
    }
}

extension Color {
    var hexString: String {
        var red: CGFloat = 0, green: CGFloat = 0, blue: CGFloat = 0, alpha: CGFloat = 0
        UIColor(self).getRed(&red, green: &green, blue: &blue, alpha: &alpha)
        let clamp = { (value: CGFloat) in Int((min(max(value, 0), 1) * 255).rounded()) }
        return String(format: "#%02X%02X%02X", clamp(red), clamp(green), clamp(blue))
    }
}

// MARK: - Push & Wartung

private struct PushTestView: View {
    @Environment(AppSession.self) private var session
    @State private var users = Remote<[User]>("users")
    @State private var userId: String?
    @State private var title = "Test"
    @State private var message = "Hallo von AkaKraft Online!"
    @State private var sent = false
    @State private var action = ActionRunner()

    private struct PushBody: Encodable { let userId: String?; let title: String; let body: String }

    var body: some View {
        Form {
            Picker("Empfänger", selection: $userId) {
                Text("Alle Nutzer").tag(String?.none)
                ForEach(users.value ?? []) { Text($0.shownName).tag(Optional($0.id)) }
            }
            TextField("Titel", text: $title)
            TextField("Text", text: $message, axis: .vertical)
            Button(sent ? "Gesendet" : "Senden", systemImage: sent ? "checkmark" : "paperplane") {
                Task {
                    if await action.run({
                        try await session.client.send(.post, "admin/push/test", body: PushBody(userId: userId, title: title.trimmed, body: message.trimmed))
                    }) { sent = true }
                }
            }
            .disabled(title.trimmed.isEmpty || message.trimmed.isEmpty || action.isRunning)
            .requiresOnline()
        }
        .navigationTitle("Test-Benachrichtigung")
        .navigationBarTitleDisplayMode(.inline)
        .task { await users.load(session) }
        .actionAlert(action)
    }
}

private struct WartungView: View {
    @Environment(AppSession.self) private var session
    @State private var result: String?
    @State private var action = ActionRunner()

    var body: some View {
        Form {
            Section {
                Button("Leerzeichen bereinigen", systemImage: "text.badge.minus") { Task { await run("admin/trim-text-fields") } }
                Button("Thumbnails generieren", systemImage: "photo.stack") { Task { await run("admin/backfill-thumbnails") } }
            } footer: {
                Text("Betrifft Werkzeug und Verbrauchsmaterial.")
            }
            .disabled(action.isRunning)
            .requiresOnline()
            if action.isRunning { ProgressView() }
            if let result {
                Section("Ergebnis") { Text(result).font(.footnote.monospaced()) }
            }
        }
        .navigationTitle("Wartung")
        .navigationBarTitleDisplayMode(.inline)
        .actionAlert(action)
    }

    private func run(_ path: String) async {
        await action.run {
            let data = try await session.client.send(.post, path)
            result = String(data: data, encoding: .utf8) ?? "Erledigt."
        }
    }
}
