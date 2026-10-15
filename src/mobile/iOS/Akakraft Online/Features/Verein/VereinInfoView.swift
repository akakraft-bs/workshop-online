import SwiftUI

struct VereinInfoView: View {
    @Environment(AppSession.self) private var session
    @State private var remote = Remote<VereinInfo>("verein/info")
    @State private var editing: Schluesselhinterlegung?
    @State private var showCreate = false
    @State private var action = ActionRunner()

    var body: some View {
        RemoteContent(remote: remote) { info in
            List {
                Section("Vorstand & Ämter") {
                    ForEach(info.amtstraeger) { person in
                        VStack(alignment: .leading, spacing: 6) {
                            HStack(spacing: 10) {
                                AvatarView(name: person.userName, size: 34)
                                VStack(alignment: .leading) {
                                    Text(person.userName).font(.headline)
                                    Text(person.roleLabel).font(.subheadline).foregroundStyle(.secondary)
                                }
                            }
                            ContactLinks(phone: person.phone, address: person.address)
                        }
                        .padding(.vertical, 2)
                    }
                }

                Section {
                    ForEach(info.schluessel.sorted { $0.sortOrder < $1.sortOrder }) { schluessel in
                        VStack(alignment: .leading, spacing: 6) {
                            Text(schluessel.name).font(.headline)
                            ContactLinks(phone: schluessel.phone, address: schluessel.address)
                        }
                        .padding(.vertical, 2)
                        .swipeActions {
                            if session.permissions.isPrivileged {
                                Button("Löschen", systemImage: "trash", role: .destructive) {
                                    Task { await delete(schluessel) }
                                }
                                Button("Bearbeiten", systemImage: "pencil") { editing = schluessel }.tint(.blue)
                            }
                        }
                    }
                } header: {
                    Text("Schlüsselhinterlegung")
                } footer: {
                    if info.schluessel.isEmpty { Text("Keine Hinterlegungen eingetragen.") }
                }
                Section {} footer: { LastUpdatedFooter(date: remote.lastUpdated) }
            }
        }
        .refreshable { await remote.load(session) }
        .task { await remote.load(session) }
        .navigationTitle("Verein")
        .navigationBarTitleDisplayMode(.inline)
        .toolbar {
            if session.permissions.isPrivileged {
                ToolbarItem(placement: .topBarTrailing) {
                    Button("Schlüsselhinterlegung hinzufügen", systemImage: "plus") { showCreate = true }
                        .requiresOnline()
                }
            }
        }
        .sheet(isPresented: $showCreate) {
            SchluesselFormView(schluessel: nil) { await remote.load(session) }
        }
        .sheet(item: $editing) { schluessel in
            SchluesselFormView(schluessel: schluessel) { await remote.load(session) }
        }
        .actionAlert(action)
    }

    private func delete(_ schluessel: Schluesselhinterlegung) async {
        if await action.run({ try await session.client.send(.delete, "verein/info/schluessel/\(schluessel.id)") }) {
            await remote.load(session)
        }
    }
}

/// Telefon- und Adress-Links (Anrufen, Karten).
struct ContactLinks: View {
    let phone: String?
    let address: String?

    var body: some View {
        if let phone = phone.nonEmpty, let url = URL(string: "tel:\(phone.filter { $0.isNumber || $0 == "+" })") {
            Link(destination: url) { Label(phone, systemImage: "phone") }
                .font(.subheadline)
        }
        if let address = address.nonEmpty,
           let encoded = address.addingPercentEncoding(withAllowedCharacters: .urlQueryAllowed),
           let url = URL(string: "maps://?q=\(encoded)") {
            Link(destination: url) { Label(address, systemImage: "mappin.and.ellipse") }
                .font(.subheadline)
        }
    }
}

private struct SchluesselFormView: View {
    let schluessel: Schluesselhinterlegung?
    let onSaved: () async -> Void

    @Environment(AppSession.self) private var session
    @Environment(\.dismiss) private var dismiss
    @State private var input: SchluesselInput
    @State private var action = ActionRunner()

    init(schluessel: Schluesselhinterlegung?, onSaved: @escaping () async -> Void) {
        self.schluessel = schluessel
        self.onSaved = onSaved
        _input = State(initialValue: SchluesselInput(name: schluessel?.name ?? "", address: schluessel?.address ?? "", phone: schluessel?.phone))
    }

    var body: some View {
        NavigationStack {
            Form {
                TextField("Name", text: $input.name)
                TextField("Adresse", text: $input.address, axis: .vertical)
                TextField("Telefon", text: optional($input.phone)).keyboardType(.phonePad)
            }
            .navigationTitle(schluessel == nil ? "Neue Hinterlegung" : "Hinterlegung bearbeiten")
            .navigationBarTitleDisplayMode(.inline)
            .toolbar {
                ToolbarItem(placement: .cancellationAction) { Button("Abbrechen", role: .cancel) { dismiss() } }
                ToolbarItem(placement: .confirmationAction) {
                    Button("Sichern", role: .confirm) {
                        Task {
                            let success = await action.run {
                                if let schluessel {
                                    try await session.client.send(.put, "verein/info/schluessel/\(schluessel.id)", body: input)
                                } else {
                                    try await session.client.send(.post, "verein/info/schluessel", body: input)
                                }
                            }
                            if success {
                                await onSaved()
                                dismiss()
                            }
                        }
                    }
                    .disabled(input.name.trimmed.isEmpty || input.address.trimmed.isEmpty || action.isRunning)
                    .requiresOnline()
                }
            }
            .actionAlert(action)
        }
        .presentationDetents([.medium, .large])
    }
}
