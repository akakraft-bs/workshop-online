import SwiftUI

struct ParkenView: View {
    @Environment(AppSession.self) private var session
    @State private var remote = Remote<ParkOverview>("parkplatz/overview")
    @State private var checkinAccount: ParkAccountStatus?
    @State private var editingClaim: ParkClaim?
    @State private var action = ActionRunner()
    @State private var problemReported: Set<String> = []
    @State private var releasingForOther: ParkClaim?

    var body: some View {
        RemoteContent(remote: remote) { overview in
            List {
                Section {
                    let ownClaims = overview.accounts.compactMap(\.belegung).filter { session.permissions.isOwner($0.userId) }.count
                    ForEach(overview.accounts) { account in
                        accountRow(account, ownClaims: ownClaims)
                    }
                } header: {
                    Text("Parkkonten")
                } footer: {
                    Text(overview.berechtigung.hinweis)
                }

                if !overview.anstehendeReservierungen.isEmpty {
                    Section("Anstehende Reservierungen") {
                        ForEach(overview.anstehendeReservierungen) { reservierung in
                            VStack(alignment: .leading, spacing: 2) {
                                HStack {
                                    Text(reservierung.titel).font(.headline)
                                    if reservierung.istMeine { StatusPill(text: "Du") }
                                }
                                if let start = reservierung.start {
                                    Text("\(start.dayTimeString)\(reservierung.end.map { " – \($0.timeString)" } ?? "")")
                                        .font(.subheadline).foregroundStyle(.secondary)
                                }
                                if let fahrzeug = reservierung.fahrzeugLabel.nonEmpty {
                                    Label(fahrzeug, systemImage: "car").font(.caption).foregroundStyle(.secondary)
                                }
                            }
                        }
                    }
                }

                Section {
                    NavigationLink {
                        ParkHistorieView()
                    } label: {
                        Label("Verlauf", systemImage: "clock.arrow.circlepath")
                    }
                    NavigationLink {
                        FahrzeugeView()
                    } label: {
                        Label("Meine Fahrzeuge", systemImage: "car.2")
                    }
                }
                Section {} footer: { LastUpdatedFooter(date: remote.lastUpdated) }
            }
        }
        .refreshable { await remote.load(session) }
        .task { await remote.load(session) }
        .navigationTitle("Campus-Parken")
        .navigationBarTitleDisplayMode(.inline)
        .sheet(item: $checkinAccount) { account in
            if let berechtigung = remote.value?.berechtigung {
                let ownClaims = remote.value?.accounts.compactMap(\.belegung).filter { session.permissions.isOwner($0.userId) }.count ?? 0
                ParkCheckinSheet(account: account, berechtigung: berechtigung, isSecondAccount: ownClaims > 0) {
                    await remote.load(session)
                }
            }
        }
        .sheet(item: $editingClaim) { claim in
            ParkClaimEditSheet(claim: claim) { await remote.load(session) }
        }
        .confirmationDialog(
            "Parkkonto freigeben?",
            isPresented: Binding(get: { releasingForOther != nil }, set: { if !$0 { releasingForOther = nil } }),
            titleVisibility: .visible,
            presenting: releasingForOther
        ) { claim in
            Button("Für \(claim.displayName) freigeben", role: .destructive) { Task { await release(claim) } }
        } message: { claim in
            Text("\(claim.displayName) (\(claim.kennzeichen)) – bitte nur, wenn das Fahrzeug den Campus verlassen hat.")
        }
        .actionAlert(action)
    }

    @ViewBuilder
    private func accountRow(_ account: ParkAccountStatus, ownClaims: Int) -> some View {
        let claim = account.belegung
        let isMine = session.permissions.isOwner(claim?.userId)
        VStack(alignment: .leading, spacing: 8) {
            HStack {
                Image(systemName: account.istFrei ? "parkingsign.circle.fill" : "car.side.fill")
                    .font(.title2)
                    .foregroundStyle(account.istFrei ? .green : .orange)
                VStack(alignment: .leading) {
                    Text(account.label).font(.headline)
                    if let notiz = account.notiz.nonEmpty {
                        Text(notiz).font(.caption).foregroundStyle(.secondary)
                    }
                }
                Spacer()
                StatusPill(text: account.istFrei ? "Frei" : "Belegt", color: account.istFrei ? .green : .orange)
            }

            if let claim {
                HStack(spacing: 8) {
                    AvatarView(name: claim.displayName, pictureURL: claim.pictureUrl.asURL, size: 28)
                    VStack(alignment: .leading, spacing: 1) {
                        Text("\(claim.displayName) · \(claim.kennzeichen)").font(.subheadline)
                        Text("seit \(claim.einfahrtAt.timeString)\(claim.voraussichtlichBis.map { ", bis ca. \($0.timeString)" } ?? "")")
                            .font(.caption)
                            .foregroundStyle(.secondary)
                    }
                }
            }

            HStack {
                if account.istFrei, ownClaims < 2 {
                    Button { checkinAccount = account } label: {
                        Label("Einchecken", systemImage: "car.side.arrowtriangle.down.fill")
                            .labelStyle(.titleAndIcon)
                    }
                    .buttonStyle(.glassProminent)
                } else if let claim, isMine {
                    Button { Task { await release(claim) } } label: {
                        Label("Freigeben", systemImage: "car.side.arrowtriangle.up.fill")
                            .labelStyle(.titleAndIcon)
                    }
                    .buttonStyle(.glassProminent)
                    Button("Bis wann?", systemImage: "clock") { editingClaim = claim }
                        .buttonStyle(.glass)
                } else if let claim, session.permissions.isPrivileged {
                    // Vorstand/Admin/Moderation: fremde Belegung freigeben, z. B. wenn vergessen wurde auszuchecken.
                    Button { releasingForOther = claim } label: {
                        Label("Freigeben", systemImage: "car.side.arrowtriangle.up.fill")
                            .labelStyle(.titleAndIcon)
                    }
                    .buttonStyle(.glass)
                }
                Spacer()
                Menu("Mehr", systemImage: "ellipsis.circle") {
                    NavigationLink {
                        ParkKennzeichenView(account: account)
                    } label: {
                        Label("Kennzeichen", systemImage: "list.bullet.rectangle")
                    }
                    if let url = account.portalUrl.asURL {
                        Link(destination: url) { Label("Parkportal öffnen", systemImage: "safari") }
                    }
                    Button(problemReported.contains(account.id) ? "Problem gemeldet" : "Problem melden",
                           systemImage: "exclamationmark.bubble") {
                        Task { await reportProblem(account) }
                    }
                    .disabled(problemReported.contains(account.id))
                }
                .labelStyle(.iconOnly)
            }
            .buttonBorderShape(.capsule)
            .requiresOnline()

            if account.istFrei, ownClaims > 0 {
                Text(ownClaims == 1
                     ? "Ein zweites Konto ist nur für Gastschraubungen erlaubt – auch ohne Hallenbelegung mit Gast-Zusatz."
                     : "Du hältst bereits beide Konten. Zum Wechseln zuerst eines freigeben.")
                    .font(.caption)
                    .foregroundStyle(.secondary)
            }
        }
        .padding(.vertical, 4)
    }

    private func release(_ claim: ParkClaim) async {
        if await action.run({ try await session.client.send(.post, "parkplatz/claims/\(claim.id)/freigeben") }) {
            await remote.load(session)
        }
    }

    private func reportProblem(_ account: ParkAccountStatus) async {
        if await action.run({ try await session.client.send(.post, "parkplatz/accounts/\(account.id)/problem") }) {
            problemReported.insert(account.id)
        }
    }
}

private struct ParkCheckinSheet: View {
    let account: ParkAccountStatus
    let berechtigung: ParkBerechtigung
    /// Die Person hält bereits ein Konto – das zweite ist nur fürs Gastschrauben gedacht.
    var isSecondAccount = false
    let onDone: () async -> Void

    @Environment(AppSession.self) private var session
    @Environment(\.dismiss) private var dismiss
    @State private var fahrzeuge = Remote<[Fahrzeug]>("fahrzeuge")
    @State private var fahrzeugId: String?
    @State private var manualKennzeichen = ""
    @State private var manualBezeichnung = ""
    @State private var hasUntil = false
    @State private var until = Date.now.addingTimeInterval(4 * 3600)
    @State private var reservierungId: String?
    @State private var bestaetigt = false
    @State private var action = ActionRunner()

    var body: some View {
        NavigationStack {
            Form {
                if isSecondAccount {
                    Section {
                        Label {
                            Text("Ein zweites Parkkonto ist **nur für Gastschraubungen** erlaubt – auch ohne Hallenbelegung mit Gast-Zusatz. Trage hier das Kennzeichen des Gastfahrzeugs ein.")
                        } icon: {
                            Image(systemName: "person.2")
                        }
                        .font(.subheadline)
                    }
                }
                Section {
                    Picker("Fahrzeug", selection: $fahrzeugId) {
                        Text("Anderes Fahrzeug").tag(String?.none)
                        ForEach(fahrzeuge.value ?? []) { Text($0.label).tag(Optional($0.id.lowercased())) }
                    }
                    if fahrzeugId == nil {
                        TextField("Kennzeichen", text: $manualKennzeichen)
                            .textInputAutocapitalization(.characters)
                            .autocorrectionDisabled()
                        TextField("Fahrzeug (z. B. VW Golf)", text: $manualBezeichnung)
                    }
                }
                Section {
                    Toggle("Voraussichtliche Ausfahrt", isOn: $hasUntil)
                    if hasUntil {
                        DatePicker("Bis", selection: $until, in: Date.now...)
                    }
                }
                if !berechtigung.waehlbareReservierungen.isEmpty {
                    Section("Reservierung") {
                        Picker("Buchung", selection: $reservierungId) {
                            Text("Keine").tag(String?.none)
                            ForEach(berechtigung.waehlbareReservierungen) { reservierung in
                                Text("\(reservierung.titel)\(reservierung.start.map { " · \($0.timeString)" } ?? "")")
                                    .tag(Optional(reservierung.eventId))
                            }
                        }
                    }
                }
                if berechtigung.erfordertBestaetigung {
                    Section {
                        Toggle("Ich bestätige die Berechtigung", isOn: $bestaetigt)
                    } footer: {
                        Text(berechtigung.hinweis)
                    }
                }
            }
            .navigationTitle("\(account.label) belegen")
            .navigationBarTitleDisplayMode(.inline)
            .task {
                await fahrzeuge.load(session)
                // Beim zweiten Konto parkt meist das Gastfahrzeug → Kennzeichen eingeben.
                guard !isSecondAccount else { return }
                fahrzeugId = berechtigung.vorgeschlagenesFahrzeugId?.lowercased()
                    ?? fahrzeuge.value?.first(where: \.istStandard)?.id.lowercased()
            }
            .toolbar {
                ToolbarItem(placement: .cancellationAction) { Button("Abbrechen", role: .cancel) { dismiss() } }
                ToolbarItem(placement: .confirmationAction) {
                    Button("Einchecken", role: .confirm) { Task { await submit() } }
                        .disabled(!isValid || action.isRunning)
                        .requiresOnline()
                }
            }
            .actionAlert(action)
        }
    }

    private var isValid: Bool {
        (fahrzeugId != nil || !manualKennzeichen.trimmed.isEmpty) && (!berechtigung.erfordertBestaetigung || bestaetigt)
    }

    private func submit() async {
        let input = ParkCheckinInput(
            parkAccountId: account.id,
            fahrzeugId: fahrzeugId,
            kennzeichen: fahrzeugId == nil ? manualKennzeichen.trimmed.uppercased() : nil,
            fahrzeugBezeichnung: fahrzeugId == nil ? manualBezeichnung.nonEmpty : nil,
            einfahrtAt: .now,
            voraussichtlichBis: hasUntil ? until : nil,
            bestaetigungAkzeptiert: bestaetigt || !berechtigung.erfordertBestaetigung,
            bookingEventId: reservierungId
        )
        if await action.run({ try await session.client.send(.post, "parkplatz/checkin", body: input) }) {
            await onDone()
            dismiss()
        }
    }
}

private struct ParkClaimEditSheet: View {
    let claim: ParkClaim
    let onDone: () async -> Void

    @Environment(AppSession.self) private var session
    @Environment(\.dismiss) private var dismiss
    @State private var until: Date
    @State private var action = ActionRunner()

    init(claim: ParkClaim, onDone: @escaping () async -> Void) {
        self.claim = claim
        self.onDone = onDone
        _until = State(initialValue: claim.voraussichtlichBis ?? .now.addingTimeInterval(2 * 3600))
    }

    private struct UntilBody: Encodable { let voraussichtlichBis: Date? }

    var body: some View {
        NavigationStack {
            Form {
                DatePicker("Voraussichtlich bis", selection: $until, in: claim.einfahrtAt...)
                    .datePickerStyle(.graphical)
            }
            .navigationTitle("Belegung ändern")
            .navigationBarTitleDisplayMode(.inline)
            .toolbar {
                ToolbarItem(placement: .cancellationAction) { Button("Abbrechen", role: .cancel) { dismiss() } }
                ToolbarItem(placement: .confirmationAction) {
                    Button("Sichern", role: .confirm) {
                        Task {
                            if await action.run({
                                try await session.client.send(.put, "parkplatz/claims/\(claim.id)", body: UntilBody(voraussichtlichBis: until))
                            }) {
                                await onDone()
                                dismiss()
                            }
                        }
                    }
                    .requiresOnline()
                }
            }
            .actionAlert(action)
        }
    }
}

struct ParkHistorieView: View {
    @Environment(AppSession.self) private var session
    @State private var remote = Remote<[ParkHistorie]>("parkplatz/historie", query: [URLQueryItem(name: "limit", value: "100")])

    var body: some View {
        RemoteContent(remote: remote) { entries in
            List {
                if entries.isEmpty {
                    ContentUnavailableView("Kein Verlauf", systemImage: "clock")
                }
                ForEach(entries) { entry in
                    HStack(alignment: .top, spacing: 12) {
                        Image(systemName: entry.symbol).foregroundStyle(.tint).frame(width: 24)
                        VStack(alignment: .leading, spacing: 2) {
                            Text("\(entry.title) · \(entry.accountLabel)").font(.subheadline.weight(.semibold))
                            Text([entry.displayName, entry.kennzeichen, entry.fahrzeugBezeichnung].compactMap { $0.nonEmpty }.joined(separator: " · "))
                                .font(.subheadline)
                            if let start = entry.einfahrtAt {
                                Text("\(start.dayTimeString)\(entry.freigegebenAt.map { " – \($0.timeString)" } ?? "")")
                                    .font(.caption).foregroundStyle(.secondary)
                            } else {
                                Text(entry.zeitpunkt.dayTimeString).font(.caption).foregroundStyle(.secondary)
                            }
                            if let hinweis = entry.bestaetigungHinweis.nonEmpty {
                                Text(hinweis).font(.caption).foregroundStyle(.orange)
                            }
                        }
                    }
                }
                Section {} footer: { LastUpdatedFooter(date: remote.lastUpdated) }
            }
        }
        .refreshable { await remote.load(session) }
        .task { await remote.load(session) }
        .navigationTitle("Verlauf")
        .navigationBarTitleDisplayMode(.inline)
    }
}

struct ParkKennzeichenView: View {
    let account: ParkAccountStatus

    @Environment(AppSession.self) private var session
    @State private var remote: Remote<ParkKennzeichenListe>
    @State private var audit: Remote<[ParkKennzeichenAudit]>
    @State private var newKennzeichen = ""
    @State private var action = ActionRunner()

    init(account: ParkAccountStatus) {
        self.account = account
        _remote = State(initialValue: Remote("parkplatz/accounts/\(account.id)/kennzeichen"))
        _audit = State(initialValue: Remote("parkplatz/accounts/\(account.id)/kennzeichen/historie"))
    }

    private struct AddBody: Encodable { let kennzeichen: String }

    var body: some View {
        RemoteContent(remote: remote) { liste in
            List {
                if let fehler = liste.fehler.nonEmpty {
                    Section { Label(fehler, systemImage: "exclamationmark.triangle").foregroundStyle(.orange) }
                }
                Section {
                    ForEach(liste.kennzeichen, id: \.self) { kennzeichen in
                        Text(kennzeichen).font(.body.monospaced())
                            .swipeActions {
                                Button("Entfernen", systemImage: "trash", role: .destructive) {
                                    Task { await remove(kennzeichen) }
                                }
                            }
                    }
                    if liste.kennzeichen.count < liste.max && liste.zugangKonfiguriert {
                        HStack {
                            TextField("Kennzeichen hinzufügen", text: $newKennzeichen)
                                .textInputAutocapitalization(.characters)
                                .autocorrectionDisabled()
                            Button("Hinzufügen", systemImage: "plus.circle.fill") { Task { await add() } }
                                .labelStyle(.iconOnly)
                                .disabled(newKennzeichen.trimmed.isEmpty || action.isRunning)
                        }
                        .requiresOnline()
                    }
                } header: {
                    Text("Hinterlegt (\(liste.kennzeichen.count)/\(liste.max))")
                } footer: {
                    if !liste.zugangKonfiguriert { Text("Für dieses Konto sind keine Portal-Zugangsdaten hinterlegt.") }
                }
                if let entries = audit.value, !entries.isEmpty {
                    Section("Änderungen") {
                        ForEach(entries) { entry in
                            VStack(alignment: .leading, spacing: 2) {
                                Text("\(entry.aktion): \(entry.kennzeichen)").font(.subheadline)
                                Text("\(entry.ausgefuehrtVon) · \(entry.createdAt.dayTimeString)").font(.caption).foregroundStyle(.secondary)
                            }
                        }
                    }
                }
            }
        }
        .refreshable { await reload() }
        .task { await reload() }
        .navigationTitle(account.label)
        .navigationBarTitleDisplayMode(.inline)
        .actionAlert(action)
    }

    private func reload() async {
        await remote.load(session)
        await audit.load(session)
    }

    private func add() async {
        let value = newKennzeichen.trimmed.uppercased()
        if await action.run({ try await session.client.send(.post, "parkplatz/accounts/\(account.id)/kennzeichen", body: AddBody(kennzeichen: value)) }) {
            newKennzeichen = ""
            await reload()
        }
    }

    private func remove(_ kennzeichen: String) async {
        if await action.run({
            try await session.client.send(.delete, "parkplatz/accounts/\(account.id)/kennzeichen", query: [URLQueryItem(name: "code", value: kennzeichen)])
        }) {
            await reload()
        }
    }
}
