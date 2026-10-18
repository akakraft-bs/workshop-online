import SwiftUI

struct ZugaengeView: View {
    @Environment(AppSession.self) private var session
    @State private var remote = Remote<[VereinZugang]>("verein/zugaenge")
    @State private var editing: VereinZugang?
    @State private var showCreate = false
    @State private var copiedId: String?
    @State private var action = ActionRunner()

    var body: some View {
        RemoteContent(remote: remote) { items in
            List {
                if items.isEmpty {
                    ContentUnavailableView("Keine Zugänge", systemImage: "key")
                }
                ForEach(items.sorted { $0.anbieter.localizedStandardCompare($1.anbieter) == .orderedAscending }) { zugang in
                    VStack(alignment: .leading, spacing: 6) {
                        HStack {
                            Text(zugang.anbieter).font(.headline)
                            Spacer()
                            Button {
                                UIPasteboard.general.string = zugang.zugangsdaten
                                copiedId = zugang.id
                            } label: {
                                Label(copiedId == zugang.id ? "Kopiert" : "Kopieren",
                                      systemImage: copiedId == zugang.id ? "checkmark" : "doc.on.doc")
                            }
                            .buttonStyle(.glass)
                            .controlSize(.small)
                        }
                        Text(zugang.zugangsdaten)
                            .font(.body.monospaced())
                            .textSelection(.enabled)
                    }
                    .padding(.vertical, 4)
                    .swipeActions {
                        if session.permissions.isPrivileged {
                            Button("Löschen", systemImage: "trash", role: .destructive) {
                                Task {
                                    if await action.run({ try await session.client.send(.delete, "verein/zugaenge/\(zugang.id)") }) {
                                        await remote.load(session)
                                    }
                                }
                            }
                            Button("Bearbeiten", systemImage: "pencil") { editing = zugang }.tint(.blue)
                        }
                    }
                }
                Section {} footer: {
                    VStack(spacing: 6) {
                        Text("Zugangsdaten sind vertraulich und nur für Vereinsmitglieder bestimmt.")
                        LastUpdatedFooter(date: remote.lastUpdated)
                    }
                    .font(.caption)
                    .frame(maxWidth: .infinity)
                }
            }
        }
        .refreshable { await remote.load(session) }
        .task { await remote.load(session) }
        .navigationTitle("Zugänge")
        .navigationBarTitleDisplayMode(.inline)
        .toolbar {
            if session.permissions.isPrivileged {
                ToolbarItem(placement: .topBarTrailing) {
                    Button("Zugang hinzufügen", systemImage: "plus") { showCreate = true }
                        .requiresOnline()
                }
            }
        }
        .sheet(isPresented: $showCreate) {
            ZugangFormView(zugang: nil) { await remote.load(session) }
        }
        .sheet(item: $editing) { zugang in
            ZugangFormView(zugang: zugang) { await remote.load(session) }
        }
        .actionAlert(action)
    }
}

private struct ZugangFormView: View {
    let zugang: VereinZugang?
    let onSaved: () async -> Void

    @Environment(AppSession.self) private var session
    @Environment(\.dismiss) private var dismiss
    @State private var input: VereinZugangInput
    @State private var action = ActionRunner()

    init(zugang: VereinZugang?, onSaved: @escaping () async -> Void) {
        self.zugang = zugang
        self.onSaved = onSaved
        _input = State(initialValue: VereinZugangInput(anbieter: zugang?.anbieter ?? "", zugangsdaten: zugang?.zugangsdaten ?? ""))
    }

    var body: some View {
        NavigationStack {
            Form {
                TextField("Anbieter", text: $input.anbieter)
                TextField("Zugangsdaten", text: $input.zugangsdaten, axis: .vertical)
                    .lineLimit(3...10)
                    .font(.body.monospaced())
                    .textInputAutocapitalization(.never)
                    .autocorrectionDisabled()
            }
            .navigationTitle(zugang == nil ? "Neuer Zugang" : "Zugang bearbeiten")
            .navigationBarTitleDisplayMode(.inline)
            .toolbar {
                ToolbarItem(placement: .cancellationAction) { Button("Abbrechen", role: .cancel) { dismiss() } }
                ToolbarItem(placement: .confirmationAction) {
                    Button("Sichern", role: .confirm) {
                        Task {
                            let success = await action.run {
                                if let zugang {
                                    try await session.client.send(.put, "verein/zugaenge/\(zugang.id)", body: input)
                                } else {
                                    try await session.client.send(.post, "verein/zugaenge", body: input)
                                }
                            }
                            if success {
                                await onSaved()
                                dismiss()
                            }
                        }
                    }
                    .disabled(input.anbieter.trimmed.isEmpty || input.zugangsdaten.trimmed.isEmpty || action.isRunning)
                    .requiresOnline()
                }
            }
            .actionAlert(action)
        }
    }
}
