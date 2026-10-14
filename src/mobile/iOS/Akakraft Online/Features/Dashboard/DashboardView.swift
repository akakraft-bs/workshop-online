import SwiftUI

struct DashboardView: View {
    @Environment(AppSession.self) private var session
    @State private var motd = Remote<Motd?>("motd")
    @State private var anwesend = Remote<[HallenCheck]>("halle/anwesend")
    @State private var umfragen = Remote<[Umfrage]>("umfrage")
    @State private var events = Remote<[CalendarEvent]>("calendar/upcoming-events")
    @State private var material = Remote<[Verbrauchsmaterial]>("verbrauchsmaterial")
    @State private var maengel = Remote<[Mangel]>("mangel")
    @State private var showMotdEditor = false
    @State private var showCheckin = false
    @State private var lowStockExpanded = true
    @State private var maengelExpanded = true
    @State private var action = ActionRunner()

    private var permissions: Permissions { session.permissions }

    var body: some View {
        List {
            if let message = motd.value ?? nil {
                Section { MotdCard(motd: message) }
            }

            hallenSection

            let offeneUmfragen = (umfragen.value ?? []).filter { $0.isOpen && !$0.hasParticipated }
            if !offeneUmfragen.isEmpty {
                Section {
                    ForEach(offeneUmfragen) { umfrage in
                        NavigationLink(value: umfrage) { UmfrageRow(umfrage: umfrage) }
                    }
                } header: {
                    Label("Deine Stimme fehlt", systemImage: "hand.raised.fill")
                }
            }

            if let upcoming = events.value, !upcoming.isEmpty {
                Section("Nächste Veranstaltungen") {
                    ForEach(upcoming.prefix(5)) { event in
                        NavigationLink(value: event) {
                            HStack(spacing: 12) {
                                DateBadge(date: event.start ?? .now, color: Color(hex: event.calendarColor) ?? .accentColor)
                                VStack(alignment: .leading, spacing: 2) {
                                    Text(event.title).font(.headline)
                                    Text(event.timeText).font(.subheadline).foregroundStyle(.secondary)
                                }
                            }
                        }
                    }
                }
            }

            let lowStock = (material.value ?? []).filter(\.isLowStock)
            if !lowStock.isEmpty {
                Section(isExpanded: $lowStockExpanded) {
                    ForEach(lowStock) { item in
                        NavigationLink(value: item) { MaterialRow(item: item, locationColor: nil) }
                    }
                } header: {
                    Label("Bestand niedrig (\(lowStock.count))", systemImage: "exclamationmark.triangle.fill")
                        .foregroundStyle(.orange)
                }
            }

            let offeneMaengel = (maengel.value ?? []).filter { $0.status.isActive }.sorted { $0.createdAt > $1.createdAt }
            if !offeneMaengel.isEmpty {
                Section(isExpanded: $maengelExpanded) {
                    ForEach(offeneMaengel.prefix(5)) { mangel in
                        NavigationLink(value: mangel) { MangelRow(mangel: mangel) }
                    }
                } header: {
                    Label("Offene Mängel (\(offeneMaengel.count))", systemImage: "wrench.and.screwdriver.fill")
                        .foregroundStyle(.red)
                }
            }

            Section {} footer: { LastUpdatedFooter(date: anwesend.lastUpdated ?? umfragen.lastUpdated) }
        }
        .listStyle(.sidebar)
        .navigationTitle(greeting)
        .refreshable { await reload() }
        .task { await reload() }
        .navigationDestination(for: Umfrage.self) { umfrage in
            UmfrageDetailView(umfrage: umfrage) { await umfragen.load(session) }
        }
        .navigationDestination(for: CalendarEvent.self) { event in
            CalendarEventDetailView(event: event, config: nil) { await events.load(session) }
        }
        .navigationDestination(for: Verbrauchsmaterial.self) { item in
            VerbrauchsmaterialDetailView(item: item, locationColor: nil) { await material.load(session) }
        }
        .navigationDestination(for: Mangel.self) { mangel in
            MangelDetailView(mangel: mangel) { await maengel.load(session) }
        }
        .toolbar {
            if permissions.isPrivileged {
                ToolbarItem(placement: .topBarTrailing) {
                    Button("Nachricht des Tages", systemImage: "megaphone") { showMotdEditor = true }
                        .requiresOnline()
                }
            }
        }
        .sheet(isPresented: $showMotdEditor) {
            MotdEditor(current: motd.value ?? nil) { await motd.load(session) }
        }
        .sheet(isPresented: $showCheckin) {
            HallenCheckinSheet { await anwesend.load(session) }
        }
        .actionAlert(action)
    }

    private var greeting: String {
        let firstName = session.user?.shownName.split(separator: " ").first.map(String.init) ?? ""
        return firstName.isEmpty ? "Übersicht" : "Hallo, \(firstName)"
    }

    @ViewBuilder
    private var hallenSection: some View {
        let present = anwesend.value ?? []
        let mine = present.first { permissions.isOwner($0.userId) }
        Section {
            if present.isEmpty {
                Label("Gerade ist niemand eingecheckt.", systemImage: "building.2")
                    .foregroundStyle(.secondary)
            }
            ForEach(present) { check in
                HStack(spacing: 12) {
                    AvatarView(name: check.displayName, pictureURL: check.pictureUrl.asURL, size: 34)
                    VStack(alignment: .leading, spacing: 2) {
                        Text(check.displayName).font(.headline)
                        if let message = check.message.nonEmpty {
                            Text(message).font(.subheadline)
                        }
                        Text("seit \(check.checkedInAt.timeString) · bis ca. \(check.expiresAt.timeString)")
                            .font(.caption)
                            .foregroundStyle(.secondary)
                    }
                }
            }
            if mine != nil {
                Button("Ich bin wieder weg", systemImage: "figure.walk.departure", role: .destructive) {
                    Task {
                        if await action.run({ try await session.client.send(.delete, "halle/checkin") }) {
                            await anwesend.load(session)
                        }
                    }
                }
                .requiresOnline()
            } else {
                Button("Ich bin an der Halle", systemImage: "figure.walk.arrival") { showCheckin = true }
                    .requiresOnline()
            }
        } header: {
            Label("An der Halle", systemImage: "building.2.fill")
        }
    }

    private func reload() async {
        await withTaskGroup(of: Void.self) { group in
            group.addTask { await motd.load(session) }
            group.addTask { await anwesend.load(session) }
            group.addTask { await umfragen.load(session) }
            group.addTask { await events.load(session) }
            group.addTask { await material.load(session) }
            group.addTask { await maengel.load(session) }
        }
    }
}

struct MotdCard: View {
    let motd: Motd

    var body: some View {
        Label {
            VStack(alignment: .leading, spacing: 4) {
                Text(motd.message)
                Text(motd.updatedAt.relativeString).font(.caption).foregroundStyle(.secondary)
            }
        } icon: {
            Image(systemName: motd.severity.symbol).foregroundStyle(motd.severity.color)
        }
        .listRowBackground(motd.severity.color.opacity(0.12))
    }
}

struct DateBadge: View {
    let date: Date
    var color: Color = .accentColor

    var body: some View {
        VStack(spacing: 0) {
            Text(date.formatted(.dateTime.month(.abbreviated)).uppercased())
                .font(.caption2.weight(.bold))
                .foregroundStyle(color)
            Text(date.formatted(.dateTime.day()))
                .font(.title3.weight(.semibold))
        }
        .frame(width: 44, height: 44)
        .background(color.opacity(0.12), in: .rect(cornerRadius: 10))
        .accessibilityHidden(true)
    }
}

private struct HallenCheckinSheet: View {
    let onDone: () async -> Void

    @Environment(AppSession.self) private var session
    @Environment(\.dismiss) private var dismiss
    @State private var message = ""
    @State private var action = ActionRunner()

    private struct CheckinBody: Encodable { let message: String? }

    var body: some View {
        NavigationStack {
            Form {
                Section {
                    TextField("Was machst du? (optional)", text: $message, axis: .vertical).lineLimit(1...3)
                } footer: {
                    Text("Andere Mitglieder sehen, dass du an der Halle bist. Der Check-in läuft automatisch ab.")
                }
            }
            .navigationTitle("Einchecken")
            .navigationBarTitleDisplayMode(.inline)
            .toolbar {
                ToolbarItem(placement: .cancellationAction) { Button("Abbrechen", role: .cancel) { dismiss() } }
                ToolbarItem(placement: .confirmationAction) {
                    Button("Einchecken", role: .confirm) {
                        Task {
                            if await action.run({
                                try await session.client.send(.post, "halle/checkin", body: CheckinBody(message: message.nonEmpty))
                            }) {
                                await onDone()
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
        .presentationDetents([.medium])
    }
}

private struct MotdEditor: View {
    let current: Motd?
    let onDone: () async -> Void

    @Environment(AppSession.self) private var session
    @Environment(\.dismiss) private var dismiss
    @State private var message: String
    @State private var severity: MotdSeverity
    @State private var action = ActionRunner()

    init(current: Motd?, onDone: @escaping () async -> Void) {
        self.current = current
        self.onDone = onDone
        _message = State(initialValue: current?.message ?? "")
        _severity = State(initialValue: current?.severity ?? .info)
    }

    private struct MotdBody: Encodable {
        let message: String
        let severity: MotdSeverity
    }

    var body: some View {
        NavigationStack {
            Form {
                Section {
                    TextField("Nachricht", text: $message, axis: .vertical).lineLimit(2...6)
                    Picker("Wichtigkeit", selection: $severity) {
                        ForEach(MotdSeverity.selectable) { Label($0.label, systemImage: $0.symbol).tag($0) }
                    }
                }
                if current != nil {
                    Section {
                        Button("Nachricht entfernen", systemImage: "trash", role: .destructive) {
                            Task { await run { try await session.client.send(.delete, "motd") } }
                        }
                    }
                }
            }
            .navigationTitle("Nachricht des Tages")
            .navigationBarTitleDisplayMode(.inline)
            .toolbar {
                ToolbarItem(placement: .cancellationAction) { Button("Abbrechen", role: .cancel) { dismiss() } }
                ToolbarItem(placement: .confirmationAction) {
                    Button("Veröffentlichen", role: .confirm) {
                        Task {
                            await run {
                                try await session.client.send(.put, "motd", body: MotdBody(message: message.trimmed, severity: severity))
                            }
                        }
                    }
                    .disabled(message.trimmed.isEmpty || action.isRunning)
                }
            }
            .requiresOnline()
            .actionAlert(action)
        }
    }

    private func run(_ work: () async throws -> Void) async {
        if await action.run(work) {
            await onDone()
            dismiss()
        }
    }
}
