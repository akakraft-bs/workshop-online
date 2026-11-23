import SwiftUI

struct FahrzeugeView: View {
    @Environment(AppSession.self) private var session
    @State private var remote = Remote<[Fahrzeug]>("fahrzeuge")
    @State private var editing: Fahrzeug?
    @State private var showCreate = false
    @State private var action = ActionRunner()

    var body: some View {
        RemoteContent(remote: remote) { items in
            List {
                if items.isEmpty {
                    ContentUnavailableView("Keine Fahrzeuge", systemImage: "car",
                                           description: Text("Fahrzeuge kannst du im Hallenbuch, bei der Hallenbelegung und beim Campus-Parken auswählen."))
                }
                ForEach(items) { fahrzeug in
                    Button {
                        editing = fahrzeug
                    } label: {
                        HStack {
                            (fahrzeug.art ?? .pkw).image
                                .foregroundStyle(Color.secondary)
                                .frame(width: 32)
                                .accessibilityLabel((fahrzeug.art ?? .pkw).label)
                            VStack(alignment: .leading) {
                                Text(fahrzeug.name)
                                    .font(.headline)
                                    .foregroundStyle(Color.primary)
                                HStack(spacing: 6) {
                                    Text((fahrzeug.art ?? .pkw).label)
                                    if fahrzeug.hatKennzeichen {
                                        Text("·")
                                        Text(fahrzeug.kennzeichen).monospaced()
                                    }
                                }
                                .font(.subheadline)
                                .foregroundStyle(Color.secondary)
                            }
                            Spacer()
                            if fahrzeug.istStandard {
                                StatusPill(text: "Standard", systemImage: "star.fill")
                            }
                        }
                    }
                    .swipeActions {
                        Button("Löschen", systemImage: "trash", role: .destructive) {
                            Task {
                                if await action.run({ try await session.client.send(.delete, "fahrzeuge/\(fahrzeug.id)") }) {
                                    await remote.load(session)
                                }
                            }
                        }
                    }
                }
                Section {} footer: { LastUpdatedFooter(date: remote.lastUpdated) }
            }
        }
        .refreshable { await remote.load(session) }
        .task { await remote.load(session) }
        .navigationTitle("Meine Fahrzeuge")
        .navigationBarTitleDisplayMode(.inline)
        .toolbar {
            ToolbarItem(placement: .topBarTrailing) {
                Button("Fahrzeug hinzufügen", systemImage: "plus") { showCreate = true }
                    .requiresOnline()
            }
        }
        .sheet(isPresented: $showCreate) {
            FahrzeugFormView(fahrzeug: nil) { await remote.load(session) }
        }
        .sheet(item: $editing) { fahrzeug in
            FahrzeugFormView(fahrzeug: fahrzeug) { await remote.load(session) }
        }
        .actionAlert(action)
    }
}

struct FahrzeugFormView: View {
    let fahrzeug: Fahrzeug?
    let onSaved: () async -> Void

    @Environment(AppSession.self) private var session
    @Environment(\.dismiss) private var dismiss
    @State private var input: FahrzeugInput
    @State private var action = ActionRunner()

    init(fahrzeug: Fahrzeug?, onSaved: @escaping () async -> Void) {
        self.fahrzeug = fahrzeug
        self.onSaved = onSaved
        _input = State(initialValue: FahrzeugInput(
            art: fahrzeug?.art.flatMap { $0 == .unknown ? nil : $0 } ?? .pkw,
            marke: fahrzeug?.marke ?? "", modell: fahrzeug?.modell,
            kennzeichen: fahrzeug?.kennzeichen ?? "", istStandard: fahrzeug?.istStandard ?? false
        ))
    }

    var body: some View {
        NavigationStack {
            Form {
                Picker("Art", selection: $input.art) {
                    ForEach(FahrzeugArt.selectable) { art in
                        Label { Text(art.label) } icon: { art.image }.tag(art)
                    }
                }
                TextField("Marke", text: $input.marke)
                TextField("Modell (optional)", text: optional($input.modell))
                Section {
                    TextField(input.art.kennzeichenPflicht ? "Kennzeichen" : "Kennzeichen (optional)", text: $input.kennzeichen)
                        .textInputAutocapitalization(.characters)
                        .autocorrectionDisabled()
                } footer: {
                    if !input.art.kennzeichenPflicht {
                        Text("Fahrräder werden beim Campus-Parken nicht angeboten.")
                    }
                }
                Toggle("Standardfahrzeug", isOn: $input.istStandard)
            }
            .navigationTitle(fahrzeug == nil ? "Neues Fahrzeug" : "Fahrzeug bearbeiten")
            .navigationBarTitleDisplayMode(.inline)
            .toolbar {
                ToolbarItem(placement: .cancellationAction) { Button("Abbrechen", role: .cancel) { dismiss() } }
                ToolbarItem(placement: .confirmationAction) {
                    Button("Sichern", role: .confirm) {
                        Task {
                            var payload = input
                            payload.marke = input.marke.trimmed
                            payload.kennzeichen = input.kennzeichen.trimmed.uppercased()
                            payload.modell = input.modell.nonEmpty
                            let success = await action.run {
                                if let fahrzeug {
                                    try await session.client.send(.put, "fahrzeuge/\(fahrzeug.id)", body: payload)
                                } else {
                                    try await session.client.send(.post, "fahrzeuge", body: payload)
                                }
                            }
                            if success {
                                await onSaved()
                                dismiss()
                            }
                        }
                    }
                    .disabled(input.marke.trimmed.isEmpty || (input.art.kennzeichenPflicht && input.kennzeichen.trimmed.isEmpty) || action.isRunning)
                    .requiresOnline()
                }
            }
            .actionAlert(action)
        }
        .presentationDetents([.medium, .large])
    }
}

extension FahrzeugArt {
    /// SF Symbol bzw. eigenes Template-Asset (Anhänger).
    var image: Image {
        if let assetImage { Image(assetImage) } else { Image(systemName: systemImage ?? "car") }
    }
}
