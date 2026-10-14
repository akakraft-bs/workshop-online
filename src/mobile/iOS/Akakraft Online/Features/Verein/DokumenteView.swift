import SwiftUI
import UniformTypeIdentifiers

struct DokumenteView: View {
    @Environment(AppSession.self) private var session
    @Environment(\.openURL) private var openURL
    @State private var remote = Remote<[DokumentOrdner]>("verein/dokumente")
    @State private var searchText = ""
    @State private var showNewFolder = false
    @State private var newFolderName = ""
    @State private var uploadTarget: DokumentOrdner?
    @State private var isUploading = false
    @State private var action = ActionRunner()

    var body: some View {
        RemoteContent(remote: remote) { ordner in
            List {
                if ordner.isEmpty {
                    ContentUnavailableView("Keine Dokumente", systemImage: "folder")
                }
                ForEach(ordner.sorted { $0.name.localizedStandardCompare($1.name) == .orderedAscending }) { folder in
                    let docs = filtered(folder.dokumente)
                    if searchText.isEmpty || !docs.isEmpty {
                        Section {
                            ForEach(docs) { dokument in
                                documentRow(dokument)
                            }
                            if docs.isEmpty {
                                Text("Leerer Ordner").foregroundStyle(.secondary)
                            }
                        } header: {
                            HStack {
                                Label(folder.name, systemImage: "folder")
                                Spacer()
                                if session.permissions.isPrivileged {
                                    Menu("Ordner", systemImage: "ellipsis") {
                                        Button("Datei hochladen", systemImage: "square.and.arrow.up") { uploadTarget = folder }
                                        Button("Ordner löschen", systemImage: "trash", role: .destructive) {
                                            Task { await deleteFolder(folder) }
                                        }
                                        .disabled(!folder.dokumente.isEmpty)
                                    }
                                    .labelStyle(.iconOnly)
                                    .requiresOnline()
                                }
                            }
                        }
                    }
                }
                if isUploading {
                    HStack { ProgressView(); Text("Datei wird hochgeladen …") }
                }
                Section {} footer: { LastUpdatedFooter(date: remote.lastUpdated) }
            }
        }
        .searchable(text: $searchText, prompt: "Dokumente durchsuchen")
        .refreshable { await remote.load(session) }
        .task { await remote.load(session) }
        .navigationTitle("Dokumente")
        .toolbar {
            if session.permissions.isPrivileged {
                ToolbarItem(placement: .topBarTrailing) {
                    Button("Neuer Ordner", systemImage: "folder.badge.plus") { showNewFolder = true }
                        .requiresOnline()
                }
            }
        }
        .alert("Neuer Ordner", isPresented: $showNewFolder) {
            TextField("Name", text: $newFolderName)
            Button("Abbrechen", role: .cancel) { newFolderName = "" }
            Button("Anlegen") { Task { await createFolder() } }
        }
        .fileImporter(
            isPresented: Binding(get: { uploadTarget != nil }, set: { if !$0 { uploadTarget = nil } }),
            allowedContentTypes: [.item]
        ) { result in
            guard let folder = uploadTarget else { return }
            if case .success(let url) = result {
                Task { await upload(url, to: folder) }
            }
        }
        .actionAlert(action)
    }

    @ViewBuilder
    private func documentRow(_ dokument: Dokument) -> some View {
        Button {
            if let url = dokument.fileUrl.asURL { openURL(url) }
        } label: {
            HStack(spacing: 12) {
                Image(systemName: dokument.symbol).font(.title3).foregroundStyle(.tint).frame(width: 28)
                VStack(alignment: .leading, spacing: 2) {
                    Text(dokument.fileName).foregroundStyle(.primary).lineLimit(2)
                    Text([dokument.uploadedByName, dokument.uploadedAt.dayString, dokument.fileSizeBytes?.byteString]
                        .compactMap { $0 }.joined(separator: " · "))
                        .font(.caption)
                        .foregroundStyle(.secondary)
                }
            }
        }
        .contextMenu {
            if let url = dokument.fileUrl.asURL {
                ShareLink(item: url) { Label("Teilen", systemImage: "square.and.arrow.up") }
                Button("Link kopieren", systemImage: "doc.on.doc") { UIPasteboard.general.url = url }
            }
        }
        .swipeActions {
            if session.permissions.isPrivileged {
                Button("Löschen", systemImage: "trash", role: .destructive) {
                    Task {
                        if await action.run({ try await session.client.send(.delete, "verein/dokumente/\(dokument.id)") }) {
                            await remote.load(session)
                        }
                    }
                }
            }
        }
    }

    private func filtered(_ docs: [Dokument]) -> [Dokument] {
        let query = searchText.trimmed.lowercased()
        return docs
            .filter { query.isEmpty || $0.fileName.lowercased().contains(query) }
            .sorted { $0.fileName.localizedStandardCompare($1.fileName) == .orderedAscending }
    }

    private struct FolderBody: Encodable { let name: String }

    private func createFolder() async {
        let name = newFolderName.trimmed
        newFolderName = ""
        guard !name.isEmpty else { return }
        if await action.run({ try await session.client.send(.post, "verein/dokumente/ordner", body: FolderBody(name: name)) }) {
            await remote.load(session)
        }
    }

    private func deleteFolder(_ folder: DokumentOrdner) async {
        if await action.run({ try await session.client.send(.delete, "verein/dokumente/ordner/\(folder.id)") }) {
            await remote.load(session)
        }
    }

    private func upload(_ fileURL: URL, to folder: DokumentOrdner) async {
        isUploading = true
        defer { isUploading = false }
        await action.run {
            let accessed = fileURL.startAccessingSecurityScopedResource()
            defer { if accessed { fileURL.stopAccessingSecurityScopedResource() } }
            let data = try Data(contentsOf: fileURL)
            let mime = UTType(filenameExtension: fileURL.pathExtension)?.preferredMIMEType ?? "application/octet-stream"
            let response = try await session.client.upload("uploads/dokument", fileData: data, fileName: fileURL.lastPathComponent, mimeType: mime)
            guard let url = try session.client.decode(FileUploadResult.self, from: response).resolvedURL else {
                throw APIError.transport("Upload fehlgeschlagen.")
            }
            let input = DokumentInput(folderId: folder.id, fileName: fileURL.lastPathComponent, fileUrl: url, fileSizeBytes: data.count)
            try await session.client.send(.post, "verein/dokumente", body: input)
        }
        await remote.load(session)
    }
}
