import SwiftUI

struct RegisterView: View {
    @Environment(AppSession.self) private var session
    @Environment(\.dismiss) private var dismiss

    @State private var displayName = ""
    @State private var email = ""
    @State private var password = ""
    @State private var passwordRepeat = ""
    @State private var isSubmitting = false
    @State private var errorMessage: String?
    @State private var successMessage: String?

    var body: some View {
        Form {
            if let successMessage {
                Section {
                    Label(successMessage, systemImage: "envelope.badge")
                    Button("Zur Anmeldung") { dismiss() }
                }
            } else {
                Section {
                    TextField("Anzeigename", text: $displayName)
                        .textContentType(.name)
                    TextField("E-Mail", text: $email)
                        .textContentType(.emailAddress)
                        .keyboardType(.emailAddress)
                        .textInputAutocapitalization(.never)
                        .autocorrectionDisabled()
                } footer: {
                    Text("Der Anzeigename ist für andere Mitglieder sichtbar.")
                }

                Section {
                    SecureField("Passwort", text: $password)
                        .textContentType(.newPassword)
                    SecureField("Passwort wiederholen", text: $passwordRepeat)
                        .textContentType(.newPassword)
                } footer: {
                    Text("Mindestens 8 Zeichen. Nach der Bestätigung deiner E-Mail schaltet dich der Vorstand frei.")
                }

                if let errorMessage {
                    Section { Text(errorMessage).foregroundStyle(.red) }
                }

                Section {
                    Button {
                        Task { await submit() }
                    } label: {
                        if isSubmitting { ProgressView() } else { Text("Konto erstellen") }
                    }
                    .disabled(!isValid || isSubmitting)
                    .requiresOnline()
                }
            }
        }
        .navigationTitle("Registrieren")
        .navigationBarTitleDisplayMode(.inline)
    }

    private var isValid: Bool {
        !displayName.trimmed.isEmpty && email.contains("@") && password.count >= 8 && password == passwordRepeat
    }

    private struct RegisterBody: Encodable {
        let email: String
        let password: String
        let displayName: String
    }

    private func submit() async {
        isSubmitting = true
        errorMessage = nil
        defer { isSubmitting = false }
        do {
            _ = try await session.client.sendPublic(
                .post, "auth/register",
                body: RegisterBody(email: email.trimmed, password: password, displayName: displayName.trimmed)
            )
            successMessage = "Fast geschafft! Bitte bestätige deine E-Mail-Adresse über den Link in der Mail."
        } catch {
            errorMessage = APIError.from(error).localizedDescription
        }
    }
}

struct ForgotPasswordView: View {
    @Environment(AppSession.self) private var session
    @State private var email: String
    @State private var sent = false
    @State private var isSubmitting = false

    init(prefilledEmail: String) {
        _email = State(initialValue: prefilledEmail)
    }

    var body: some View {
        Form {
            Section {
                TextField("E-Mail", text: $email)
                    .textContentType(.emailAddress)
                    .keyboardType(.emailAddress)
                    .textInputAutocapitalization(.never)
                    .autocorrectionDisabled()
            } footer: {
                Text(sent
                     ? "Falls ein Konto existiert, haben wir dir einen Link zum Zurücksetzen geschickt."
                     : "Du erhältst eine E-Mail mit einem Link zum Zurücksetzen deines Passworts.")
            }
            Section {
                Button("Link senden") {
                    Task {
                        isSubmitting = true
                        _ = try? await session.client.sendPublic(.post, "auth/request-password-reset", body: ["email": email.trimmed])
                        isSubmitting = false
                        sent = true
                    }
                }
                .disabled(!email.contains("@") || isSubmitting)
                .requiresOnline()
            }
        }
        .navigationTitle("Passwort vergessen")
        .navigationBarTitleDisplayMode(.inline)
    }
}

struct PendingApprovalView: View {
    @Environment(AppSession.self) private var session
    @State private var isRefreshing = false

    var body: some View {
        NavigationStack {
            ContentUnavailableView {
                Label("Freischaltung ausstehend", systemImage: "hourglass")
            } description: {
                Text("Hallo \(session.user?.shownName ?? "")! Dein Konto ist angelegt. Sobald dich der Vorstand freigeschaltet hat, kannst du die App nutzen.")
            } actions: {
                Button {
                    Task {
                        isRefreshing = true
                        await session.refreshUser()
                        isRefreshing = false
                    }
                } label: {
                    if isRefreshing { ProgressView() } else { Text("Erneut prüfen") }
                }
                .buttonStyle(.glassProminent)
                .requiresOnline()

                Button("Abmelden", role: .destructive) {
                    Task { await session.logout() }
                }
            }
        }
    }
}

struct ServerSelectionView: View {
    @Environment(AppSession.self) private var session
    @Environment(\.dismiss) private var dismiss
    @State private var input = ""
    @State private var invalid = false

    var body: some View {
        NavigationStack {
            Form {
                Section {
                    TextField("app.akakraft-bs.de", text: $input)
                        .keyboardType(.URL)
                        .textInputAutocapitalization(.never)
                        .autocorrectionDisabled()
                } header: {
                    Text("Serveradresse")
                } footer: {
                    Text(invalid ? "Ungültige Adresse." : "Aktuell: \(session.server.baseURL.absoluteString)")
                }
                Section {
                    Button("Vereinsserver (app.akakraft-bs.de)") { input = ServerConfiguration.akakraft.baseURL.absoluteString }
                    #if DEBUG
                    Button("Lokales Backend (localhost:5153)") { input = ServerConfiguration.local.baseURL.absoluteString }
                    #endif
                }
            }
            .navigationTitle("Server")
            .navigationBarTitleDisplayMode(.inline)
            .toolbar {
                ToolbarItem(placement: .cancellationAction) { Button("Abbrechen", role: .cancel) { dismiss() } }
                ToolbarItem(placement: .confirmationAction) {
                    Button("Verbinden", role: .confirm) {
                        guard let server = ServerConfiguration.fromUserInput(input) else {
                            invalid = true
                            return
                        }
                        Task {
                            await session.switchServer(to: server)
                            dismiss()
                        }
                    }
                }
            }
            .onAppear { input = session.server.baseURL.absoluteString }
        }
    }
}
