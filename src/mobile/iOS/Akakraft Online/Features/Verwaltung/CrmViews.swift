import SwiftUI

struct CrmListView: View {
    @Environment(AppSession.self) private var session
    @State private var remote = Remote<[PartnerOverview]>("crm/partner")
    @State private var searchText = ""
    @State private var status: PartnerStatus?
    @State private var showCreate = false

    var body: some View {
        RemoteContent(remote: remote) { items in
            let visible = items
                .filter { status == nil || $0.status == status }
                .filter { searchText.isEmpty || $0.name.localizedCaseInsensitiveContains(searchText) || ($0.kategorie ?? "").localizedCaseInsensitiveContains(searchText) }
                .sorted { $0.name.localizedStandardCompare($1.name) == .orderedAscending }
            List {
                if visible.isEmpty {
                    ContentUnavailableView("Keine Partner", systemImage: "person.crop.rectangle.stack")
                }
                ForEach(visible) { partner in
                    NavigationLink(value: partner) {
                        VStack(alignment: .leading, spacing: 4) {
                            HStack {
                                Text(partner.name).font(.headline)
                                Spacer()
                                StatusPill(text: partner.status.label, color: partner.status.color)
                            }
                            HStack(spacing: 8) {
                                if let kategorie = partner.kategorie.nonEmpty { Text(kategorie) }
                                Label("\(partner.anzahlKontakte)", systemImage: "bubble.left.and.bubble.right")
                                if let last = partner.letzterKontakt { Text("zuletzt \(last.relativeString)") }
                            }
                            .font(.caption)
                            .foregroundStyle(.secondary)
                        }
                    }
                }
                Section {} footer: { LastUpdatedFooter(date: remote.lastUpdated) }
            }
        }
        .searchable(text: $searchText, prompt: "Partner suchen")
        .refreshable { await remote.load(session) }
        .task { await remote.load(session) }
        .navigationTitle("Partner & Sponsoren")
        .navigationBarTitleDisplayMode(.inline)
        .navigationDestination(for: PartnerOverview.self) { partner in
            CrmDetailView(partnerId: partner.id) { await remote.load(session) }
        }
        .toolbar {
            ToolbarItem(placement: .topBarTrailing) {
                Menu {
                    Picker("Status", selection: $status) {
                        Text("Alle").tag(PartnerStatus?.none)
                        ForEach(PartnerStatus.selectable) { Text($0.label).tag(Optional($0)) }
                    }
                } label: {
                    Label("Filter", systemImage: status == nil ? "line.3.horizontal.decrease.circle" : "line.3.horizontal.decrease.circle.fill")
                }
            }
            ToolbarItem(placement: .topBarTrailing) {
                Button("Partner hinzufügen", systemImage: "plus") { showCreate = true }
                    .requiresOnline()
            }
        }
        .sheet(isPresented: $showCreate) {
            PartnerFormView(partner: nil) { await remote.load(session) }
        }
    }
}

struct CrmDetailView: View {
    let partnerId: String
    let onChange: () async -> Void

    @Environment(AppSession.self) private var session
    @Environment(\.dismiss) private var dismiss
    @State private var remote: Remote<PartnerDetail>
    @State private var showEditPartner = false
    @State private var editingContact: Ansprechpartner?
    @State private var showNewContact = false
    @State private var editingEntry: Kontakteintrag?
    @State private var showNewEntry = false
    @State private var confirmDelete = false
    @State private var action = ActionRunner()

    init(partnerId: String, onChange: @escaping () async -> Void) {
        self.partnerId = partnerId
        self.onChange = onChange
        _remote = State(initialValue: Remote("crm/partner/\(partnerId)"))
    }

    var body: some View {
        RemoteContent(remote: remote) { partner in
            List {
                Section {
                    VStack(alignment: .leading, spacing: 6) {
                        StatusPill(text: partner.status.label, color: partner.status.color)
                        Text(partner.name).font(.title2.bold())
                        if let kategorie = partner.kategorie.nonEmpty { Text(kategorie).foregroundStyle(.secondary) }
                    }
                    .padding(.vertical, 4)
                    if let url = partner.website.asURL {
                        Link(destination: url) { Label(url.host() ?? url.absoluteString, systemImage: "globe") }
                    }
                    if let notizen = partner.notizen.nonEmpty { Text(notizen) }
                }

                Section("Ansprechpartner") {
                    if partner.ansprechpartner.isEmpty { Text("Noch keine Ansprechpartner.").foregroundStyle(.secondary) }
                    ForEach(partner.ansprechpartner) { contact in
                        VStack(alignment: .leading, spacing: 4) {
                            Text(contact.name).font(.headline)
                            if let position = contact.position.nonEmpty { Text(position).font(.subheadline).foregroundStyle(.secondary) }
                            if let email = contact.email.nonEmpty, let url = URL(string: "mailto:\(email)") {
                                Link(destination: url) { Label(email, systemImage: "envelope") }.font(.subheadline)
                            }
                            ContactLinks(phone: contact.telefon, address: nil)
                            if let notizen = contact.notizen.nonEmpty { Text(notizen).font(.caption).foregroundStyle(.secondary) }
                        }
                        .swipeActions {
                            Button("Löschen", systemImage: "trash", role: .destructive) {
                                Task { await run { try await session.client.send(.delete, "crm/partner/\(partnerId)/ansprechpartner/\(contact.id)") } }
                            }
                            Button("Bearbeiten", systemImage: "pencil") { editingContact = contact }.tint(.blue)
                        }
                    }
                }

                Section("Kontakthistorie") {
                    if partner.kontakteintraege.isEmpty { Text("Noch keine Kontakte erfasst.").foregroundStyle(.secondary) }
                    ForEach(partner.kontakteintraege.sorted { $0.datum > $1.datum }) { entry in
                        VStack(alignment: .leading, spacing: 4) {
                            HStack {
                                Label(entry.kanal.label, systemImage: entry.kanal.symbol).font(.subheadline.weight(.semibold))
                                Spacer()
                                StatusPill(text: entry.reaktion.label, color: entry.reaktion.color)
                            }
                            Text("\(entry.datum.dayString)\(entry.ansprechpartnerName.map { " · \($0)" } ?? "")")
                                .font(.caption).foregroundStyle(.secondary)
                            Text(entry.zusammenfassung)
                            if let next = entry.naechsteSchritte.nonEmpty {
                                Label(next, systemImage: "arrow.forward.circle").font(.subheadline).foregroundStyle(.tint)
                            }
                        }
                        .padding(.vertical, 2)
                        .swipeActions {
                            Button("Löschen", systemImage: "trash", role: .destructive) {
                                Task { await run { try await session.client.send(.delete, "crm/partner/\(partnerId)/kontakt/\(entry.id)") } }
                            }
                            Button("Bearbeiten", systemImage: "pencil") { editingEntry = entry }.tint(.blue)
                        }
                    }
                }
            }
        }
        .refreshable { await remote.load(session) }
        .task { await remote.load(session) }
        .navigationTitle(remote.value?.name ?? "Partner")
        .navigationBarTitleDisplayMode(.inline)
        .toolbar {
            ToolbarItem(placement: .topBarTrailing) {
                Menu("Aktionen", systemImage: "ellipsis") {
                    Button("Kontakt erfassen", systemImage: "plus.bubble") { showNewEntry = true }
                    Button("Ansprechpartner hinzufügen", systemImage: "person.badge.plus") { showNewContact = true }
                    Button("Partner bearbeiten", systemImage: "pencil") { showEditPartner = true }
                    Button("Partner löschen", systemImage: "trash", role: .destructive) { confirmDelete = true }
                }
                .requiresOnline()
            }
        }
        .sheet(isPresented: $showEditPartner) {
            if let partner = remote.value { PartnerFormView(partner: partner) { await reload() } }
        }
        .sheet(isPresented: $showNewContact) {
            AnsprechpartnerFormView(partnerId: partnerId, contact: nil) { await reload() }
        }
        .sheet(item: $editingContact) { contact in
            AnsprechpartnerFormView(partnerId: partnerId, contact: contact) { await reload() }
        }
        .sheet(isPresented: $showNewEntry) {
            KontaktFormView(partnerId: partnerId, entry: nil, contacts: remote.value?.ansprechpartner ?? []) { await reload() }
        }
        .sheet(item: $editingEntry) { entry in
            KontaktFormView(partnerId: partnerId, entry: entry, contacts: remote.value?.ansprechpartner ?? []) { await reload() }
        }
        .confirmationDialog("Partner löschen?", isPresented: $confirmDelete, titleVisibility: .visible) {
            Button("Löschen", role: .destructive) {
                Task {
                    if await action.run({ try await session.client.send(.delete, "crm/partner/\(partnerId)") }) {
                        await onChange()
                        dismiss()
                    }
                }
            }
        } message: {
            Text("Ansprechpartner und Kontakthistorie werden ebenfalls gelöscht.")
        }
        .actionAlert(action)
    }

    private func reload() async {
        await remote.load(session)
        await onChange()
    }

    private func run(_ work: () async throws -> Void) async {
        if await action.run(work) { await reload() }
    }
}

struct PartnerFormView: View {
    let partner: PartnerDetail?
    let onSaved: () async -> Void

    @Environment(AppSession.self) private var session
    @Environment(\.dismiss) private var dismiss
    @State private var input: PartnerInput
    @State private var action = ActionRunner()

    init(partner: PartnerDetail?, onSaved: @escaping () async -> Void) {
        self.partner = partner
        self.onSaved = onSaved
        _input = State(initialValue: PartnerInput(
            name: partner?.name ?? "", kategorie: partner?.kategorie, status: partner?.status ?? .potentiell,
            website: partner?.website, notizen: partner?.notizen
        ))
    }

    var body: some View {
        FormSheet(title: partner == nil ? "Neuer Partner" : "Partner bearbeiten", canSave: !input.name.trimmed.isEmpty, action: action) {
            if let partner {
                try await session.client.send(.put, "crm/partner/\(partner.id)", body: input)
            } else {
                try await session.client.send(.post, "crm/partner", body: input)
            }
            await onSaved()
        } content: {
            TextField("Name", text: $input.name)
            TextField("Kategorie", text: optional($input.kategorie))
            Picker("Status", selection: $input.status) {
                ForEach(PartnerStatus.selectable) { Text($0.label).tag($0) }
            }
            TextField("Website", text: optional($input.website)).keyboardType(.URL).textInputAutocapitalization(.never)
            TextField("Notizen", text: optional($input.notizen), axis: .vertical).lineLimit(3...8)
        }
    }
}

private struct AnsprechpartnerFormView: View {
    let partnerId: String
    let contact: Ansprechpartner?
    let onSaved: () async -> Void

    @Environment(AppSession.self) private var session
    @State private var input: AnsprechpartnerInput
    @State private var action = ActionRunner()

    init(partnerId: String, contact: Ansprechpartner?, onSaved: @escaping () async -> Void) {
        self.partnerId = partnerId
        self.contact = contact
        self.onSaved = onSaved
        _input = State(initialValue: AnsprechpartnerInput(
            name: contact?.name ?? "", position: contact?.position, email: contact?.email,
            telefon: contact?.telefon, notizen: contact?.notizen
        ))
    }

    var body: some View {
        FormSheet(title: contact == nil ? "Neuer Ansprechpartner" : "Ansprechpartner bearbeiten", canSave: !input.name.trimmed.isEmpty, action: action) {
            if let contact {
                try await session.client.send(.put, "crm/partner/\(partnerId)/ansprechpartner/\(contact.id)", body: input)
            } else {
                try await session.client.send(.post, "crm/partner/\(partnerId)/ansprechpartner", body: input)
            }
            await onSaved()
        } content: {
            TextField("Name", text: $input.name).textContentType(.name)
            TextField("Position", text: optional($input.position))
            TextField("E-Mail", text: optional($input.email)).keyboardType(.emailAddress).textInputAutocapitalization(.never)
            TextField("Telefon", text: optional($input.telefon)).keyboardType(.phonePad)
            TextField("Notizen", text: optional($input.notizen), axis: .vertical).lineLimit(2...6)
        }
    }
}

private struct KontaktFormView: View {
    let partnerId: String
    let entry: Kontakteintrag?
    let contacts: [Ansprechpartner]
    let onSaved: () async -> Void

    @Environment(AppSession.self) private var session
    @State private var input: KontakteintragInput
    @State private var action = ActionRunner()

    init(partnerId: String, entry: Kontakteintrag?, contacts: [Ansprechpartner], onSaved: @escaping () async -> Void) {
        self.partnerId = partnerId
        self.entry = entry
        self.contacts = contacts
        self.onSaved = onSaved
        _input = State(initialValue: KontakteintragInput(
            ansprechpartnerId: entry?.ansprechpartnerId?.lowercased(), datum: entry?.datum ?? .now,
            kanal: entry?.kanal ?? .email, reaktion: entry?.reaktion ?? .keineAntwort,
            zusammenfassung: entry?.zusammenfassung ?? "", naechsteSchritte: entry?.naechsteSchritte
        ))
    }

    var body: some View {
        FormSheet(title: entry == nil ? "Kontakt erfassen" : "Kontakt bearbeiten", canSave: !input.zusammenfassung.trimmed.isEmpty, action: action) {
            if let entry {
                try await session.client.send(.put, "crm/partner/\(partnerId)/kontakt/\(entry.id)", body: input)
            } else {
                try await session.client.send(.post, "crm/partner/\(partnerId)/kontakt", body: input)
            }
            await onSaved()
        } content: {
            DatePicker("Datum", selection: $input.datum, displayedComponents: .date)
            Picker("Kanal", selection: $input.kanal) {
                ForEach(KontaktKanal.selectable) { Label($0.label, systemImage: $0.symbol).tag($0) }
            }
            Picker("Reaktion", selection: $input.reaktion) {
                ForEach(KontaktReaktion.selectable) { Text($0.label).tag($0) }
            }
            Picker("Ansprechpartner", selection: $input.ansprechpartnerId) {
                Text("Keiner").tag(String?.none)
                ForEach(contacts) { Text($0.name).tag(Optional($0.id.lowercased())) }
            }
            TextField("Zusammenfassung", text: $input.zusammenfassung, axis: .vertical).lineLimit(3...8)
            TextField("Nächste Schritte", text: optional($input.naechsteSchritte), axis: .vertical).lineLimit(2...5)
        }
    }
}

/// Einheitliches Formular-Sheet mit Abbrechen/Sichern für einfache Bearbeitungsdialoge.
struct FormSheet<Content: View>: View {
    let title: String
    let canSave: Bool
    let action: ActionRunner
    let save: () async throws -> Void
    @ViewBuilder let content: () -> Content

    @Environment(\.dismiss) private var dismiss

    init(title: String, canSave: Bool, action: ActionRunner, save: @escaping () async throws -> Void, @ViewBuilder content: @escaping () -> Content) {
        self.title = title
        self.canSave = canSave
        self.action = action
        self.save = save
        self.content = content
    }

    var body: some View {
        NavigationStack {
            Form { content() }
                .navigationTitle(title)
                .navigationBarTitleDisplayMode(.inline)
                .toolbar {
                    ToolbarItem(placement: .cancellationAction) { Button("Abbrechen", role: .cancel) { dismiss() } }
                    ToolbarItem(placement: .confirmationAction) {
                        Button("Sichern", role: .confirm) {
                            Task {
                                if await action.run(save) { dismiss() }
                            }
                        }
                        .disabled(!canSave || action.isRunning)
                        .requiresOnline()
                    }
                }
                .actionAlert(action)
                .interactiveDismissDisabled(action.isRunning)
        }
    }
}
