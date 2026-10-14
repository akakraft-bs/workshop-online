import SwiftUI

struct WerkzeugFormView: View {
    let werkzeug: Werkzeug?
    let categories: [String]
    let locations: [StorageLocation]
    let onSaved: (Werkzeug?) async -> Void

    @Environment(AppSession.self) private var session
    @Environment(\.dismiss) private var dismiss
    @State private var input: WerkzeugInput
    @State private var action = ActionRunner()
    @State private var dokumente = Remote<[DokumentOrdner]>("verein/dokumente")
    @State private var allLocations = Remote<[StorageLocation]>("storage-locations")

    init(werkzeug: Werkzeug?, categories: [String], locations: [StorageLocation], onSaved: @escaping (Werkzeug?) async -> Void) {
        self.werkzeug = werkzeug
        self.categories = categories
        self.locations = locations
        self.onSaved = onSaved
        _input = State(initialValue: WerkzeugInput(
            name: werkzeug?.name ?? "",
            description: werkzeug?.description ?? "",
            category: werkzeug?.category ?? "",
            imageUrl: werkzeug?.imageUrl,
            thumbnailUrl: werkzeug?.thumbnailUrl,
            dimensions: werkzeug?.dimensions,
            storageLocation: werkzeug?.storageLocation,
            anleitungDokumentId: werkzeug?.anleitungDokumentId
        ))
    }

    var body: some View {
        NavigationStack {
            Form {
                Section {
                    TextField("Name", text: $input.name)
                    SuggestingTextField(title: "Kategorie", text: $input.category, suggestions: categories)
                    TextField("Beschreibung", text: $input.description, axis: .vertical)
                        .lineLimit(3...8)
                }
                Section {
                    TextField("Maße", text: optional($input.dimensions))
                    SuggestingTextField(
                        title: "Ablageort",
                        text: optional($input.storageLocation),
                        suggestions: (allLocations.value ?? locations).map(\.name)
                    )
                }
                PhotoUploadSection(uploadPath: "uploads/werkzeug", imageUrl: $input.imageUrl, thumbnailUrl: $input.thumbnailUrl)
                Section("Anleitung") {
                    Picker("Dokument", selection: $input.anleitungDokumentId) {
                        Text("Keine").tag(String?.none)
                        ForEach(dokumente.value ?? []) { ordner in
                            Section(ordner.name) {
                                ForEach(ordner.dokumente) { Text($0.fileName).tag(Optional($0.id)) }
                            }
                        }
                    }
                }
            }
            .navigationTitle(werkzeug == nil ? "Neues Werkzeug" : "Werkzeug bearbeiten")
            .navigationBarTitleDisplayMode(.inline)
            .task {
                await dokumente.load(session)
                await allLocations.load(session)
            }
            .toolbar {
                ToolbarItem(placement: .cancellationAction) { Button("Abbrechen", role: .cancel) { dismiss() } }
                ToolbarItem(placement: .confirmationAction) {
                    Button("Sichern", role: .confirm) { Task { await save() } }
                        .disabled(!isValid || action.isRunning)
                        .requiresOnline()
                }
            }
            .actionAlert(action)
            .interactiveDismissDisabled(action.isRunning)
        }
    }

    private var isValid: Bool { !input.name.trimmed.isEmpty && !input.category.trimmed.isEmpty }

    private func save() async {
        var payload = input
        payload.dimensions = input.dimensions.nonEmpty
        payload.storageLocation = input.storageLocation.nonEmpty
        var saved: Werkzeug?
        let success = await action.run {
            saved = if let werkzeug {
                try await session.client.send(.put, "werkzeug/\(werkzeug.id)", body: payload, decoding: Werkzeug.self)
            } else {
                try await session.client.send(.post, "werkzeug", body: payload, decoding: Werkzeug.self)
            }
        }
        if success {
            await onSaved(saved)
            dismiss()
        }
    }
}

/// TextField mit Vorschlägen aus vorhandenen Werten (z. B. Kategorien, Ablageorte).
struct SuggestingTextField: View {
    let title: String
    @Binding var text: String
    let suggestions: [String]

    var body: some View {
        HStack {
            TextField(title, text: $text)
            if !suggestions.isEmpty {
                Menu {
                    ForEach(Array(Set(suggestions)).sorted(), id: \.self) { suggestion in
                        Button(suggestion) { text = suggestion }
                    }
                } label: {
                    Image(systemName: "chevron.up.chevron.down")
                        .foregroundStyle(.secondary)
                }
                .accessibilityLabel("\(title) auswählen")
            }
        }
    }
}

/// Bindet einen optionalen String an ein TextField (leer ⇢ nil).
func optional(_ binding: Binding<String?>) -> Binding<String> {
    Binding(
        get: { binding.wrappedValue ?? "" },
        set: { binding.wrappedValue = $0.isEmpty ? nil : $0 }
    )
}
