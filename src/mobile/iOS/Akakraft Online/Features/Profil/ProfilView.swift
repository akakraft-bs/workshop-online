import SwiftUI

struct ProfilView: View {
    @Environment(AppSession.self) private var session
    @Environment(ConnectivityMonitor.self) private var connectivity
    @Environment(OfflineSync.self) private var offlineSync
    @State private var preferences = Remote<UserPreferences>("users/me/preferences")
    @State private var displayName = ""
    @State private var phone = ""
    @State private var address = ""
    @State private var loadedPreferences = false
    @State private var action = ActionRunner()
    @State private var confirmLogout = false
    @State private var confirmClearCache = false
    @State private var cacheBytes = 0
    @State private var showServerSheet = false
    @State private var serverVersion: String?

    var body: some View {
        @Bindable var connectivity = connectivity
        Form {
            if let user = session.user {
                Section {
                    HStack(spacing: 14) {
                        AvatarView(name: user.shownName, pictureURL: user.pictureUrl.asURL, size: 56)
                        VStack(alignment: .leading, spacing: 2) {
                            Text(user.shownName).font(.title3.bold())
                            Text(user.email).font(.subheadline).foregroundStyle(.secondary)
                        }
                    }
                    .padding(.vertical, 4)
                    LabeledContent("Rollen", value: user.roles.filter { $0 != .none }.map(\.label).joined(separator: ", "))
                }
            }

            Section {
                TextField("Anzeigename", text: $displayName)
                    .textContentType(.name)
                TextField("Telefon", text: $phone)
                    .textContentType(.telephoneNumber)
                    .keyboardType(.phonePad)
                TextField("Adresse", text: $address, axis: .vertical)
                    .textContentType(.fullStreetAddress)
            } header: {
                Text("Profil")
            } footer: {
                Text("Telefon und Adresse sind für Vorstandsmitglieder in der Vereinsinfo sichtbar.")
            }

            Section {
                LabeledContent("Server", value: session.server.displayName)
                // Werte nur als einzelner Text: Label oder ProgressView als Wert lassen die Zeile aufblähen.
                LabeledContent("Status") {
                    Text("\(Image(systemName: connectivity.isOnline ? "wifi" : "wifi.slash")) \(connectivity.isOnline ? "Online" : "Offline")")
                        .foregroundStyle(connectivity.isOnline ? .green : .orange)
                }
                LabeledContent("Offline-Stand", value: offlineSyncStatus)
                LabeledContent("Gespeicherte Daten", value: cacheBytes.byteString)
                Button("Offline-Daten jetzt laden", systemImage: "arrow.down.circle") {
                    Task {
                        await offlineSync.sync(session)
                        cacheBytes = session.cache.totalBytes + ImagePipeline.shared.diskUsage
                    }
                }
                .disabled(offlineSync.isRunning)
                .requiresOnline()
                Button("Offline-Daten löschen", systemImage: "trash") { confirmClearCache = true }
                #if DEBUG
                Toggle("Offline-Modus simulieren", isOn: $connectivity.simulateOffline)
                #endif
                if AppConfig.allowsCustomServer {
                    Button("Server wechseln", systemImage: "server.rack") { showServerSheet = true }
                }
            } header: {
                Text("Offline & Server")
            } footer: {
                Text("Beim Öffnen der App werden alle Bereiche samt Vorschaubildern geladen und bleiben offline lesbar – auch Seiten, die du noch nie geöffnet hast. Änderungen sind nur mit Verbindung möglich.")
            }

            Section {
                NavigationLink {
                    FeedbackView()
                } label: {
                    Label("Feedback senden", systemImage: "bubble.left.and.exclamationmark.bubble.right")
                }
                Link(destination: session.server.webURL(route: "")) {
                    Label("Web-App öffnen", systemImage: "safari")
                }
                LabeledContent("App-Version", value: Bundle.main.appVersion)
                LabeledContent("Server-Version", value: serverVersion ?? "–")
            }

            Section {
                Button("Abmelden", systemImage: "rectangle.portrait.and.arrow.right", role: .destructive) {
                    confirmLogout = true
                }
            }
        }
        .navigationTitle("Profil & Einstellungen")
        .navigationBarTitleDisplayMode(.inline)
        .task { serverVersion = await loadServerVersion() }
        .task {
            cacheBytes = session.cache.totalBytes + ImagePipeline.shared.diskUsage
            await preferences.load(session)
            if let prefs = preferences.value, !loadedPreferences {
                displayName = prefs.displayName ?? ""
                phone = prefs.phone ?? ""
                address = prefs.address ?? ""
                loadedPreferences = true
            }
        }
        .confirmationDialog("Abmelden?", isPresented: $confirmLogout, titleVisibility: .visible) {
            Button("Abmelden", role: .destructive) { Task { await session.logout() } }
        } message: {
            Text("Die offline gespeicherten Daten werden von diesem Gerät gelöscht.")
        }
        .confirmationDialog("Offline-Daten löschen?", isPresented: $confirmClearCache, titleVisibility: .visible) {
            Button("Löschen", role: .destructive) {
                session.cache.removeEverything()
                ImagePipeline.shared.clear()
                cacheBytes = session.cache.totalBytes + ImagePipeline.shared.diskUsage
            }
        } message: {
            Text("Seiten werden beim nächsten Öffnen mit Verbindung neu geladen.")
        }
        .sheet(isPresented: $showServerSheet) { ServerSelectionView() }
        .toolbar {
            // Wie in iOS-Einstellungen üblich: „Sichern“ erscheint erst, wenn etwas geändert wurde.
            if hasChanges {
                ToolbarItem(placement: .confirmationAction) {
                    Button("Sichern", role: .confirm) { Task { await save() } }
                        .disabled(action.isRunning)
                        .requiresOnline()
                }
            }
        }
        .actionAlert(action)
    }

    private var hasChanges: Bool {
        guard loadedPreferences, let prefs = preferences.value else { return false }
        return displayName.trimmed != (prefs.displayName ?? "").trimmed
            || phone.trimmed != (prefs.phone ?? "").trimmed
            || address.trimmed != (prefs.address ?? "").trimmed
    }

    private func save() async {
        let input = UserPreferences(
            favoriteRoutes: preferences.value?.favoriteRoutes ?? [],
            displayName: displayName.nonEmpty,
            phone: phone.nonEmpty,
            address: address.nonEmpty
        )
        if await action.run({ try await session.client.send(.put, "users/me/preferences", body: input) }) {
            preferences.update { $0 = input }
            await session.refreshUser()
        }
    }

    private var offlineSyncStatus: String {
        if offlineSync.isRunning { return "wird geladen …" }
        return offlineSync.lastCompleted?.formatted(date: .abbreviated, time: .shortened) ?? "noch nicht geladen"
    }

    /// Backend-Version über `GET /version`; ältere Backends ohne den Endpunkt
    /// liefern stattdessen die Release-Version der Web-App (`version.json`).
    private func loadServerVersion() async -> String? {
        struct VersionInfo: Decodable { let version: String }
        if let data = try? await session.client.sendPublic(.get, "version", body: nil),
           let info = try? JSONDecoder().decode(VersionInfo.self, from: data) {
            return info.version
        }
        guard connectivity.isOnline,
              let (data, response) = try? await URLSession.shared.data(from: session.server.webURL(route: "version.json")),
              (response as? HTTPURLResponse)?.statusCode == 200,
              let info = try? JSONDecoder().decode(VersionInfo.self, from: data)
        else { return nil }
        return info.version
    }
}

struct FeedbackView: View {
    @Environment(AppSession.self) private var session
    @Environment(\.dismiss) private var dismiss
    @State private var text = ""
    @State private var sent = false
    @State private var action = ActionRunner()

    private struct FeedbackBody: Encodable {
        let text: String
        let pageUrl: String
        let appVersion: String?
    }

    var body: some View {
        Form {
            if sent {
                Section {
                    Label("Danke für dein Feedback!", systemImage: "heart.fill").foregroundStyle(.pink)
                }
            } else {
                Section {
                    TextField("Was können wir verbessern? Hast du einen Fehler gefunden?", text: $text, axis: .vertical)
                        .lineLimit(5...12)
                } footer: {
                    Text("Wird mit der App-Version an die Administratoren gesendet.")
                }
                Section {
                    Button("Senden", systemImage: "paperplane.fill") {
                        Task {
                            let body = FeedbackBody(text: text.trimmed, pageUrl: "ios-app", appVersion: "iOS \(Bundle.main.appVersion)")
                            if await action.run({ try await session.client.send(.post, "feedback", body: body) }) {
                                sent = true
                            }
                        }
                    }
                    .disabled(text.trimmed.isEmpty || action.isRunning)
                    .requiresOnline()
                }
            }
        }
        .navigationTitle("Feedback")
        .navigationBarTitleDisplayMode(.inline)
        .actionAlert(action)
    }
}

extension Bundle {
    var appVersion: String {
        let version = infoDictionary?["CFBundleShortVersionString"] as? String ?? "?"
        let build = infoDictionary?["CFBundleVersion"] as? String ?? "?"
        return "\(version) (\(build))"
    }
}
