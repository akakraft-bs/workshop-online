import AuthenticationServices
import SwiftUI

struct LoginView: View {
    @Environment(AppSession.self) private var session
    @Environment(ConnectivityMonitor.self) private var connectivity
    @Environment(\.webAuthenticationSession) private var webAuthenticationSession

    @State private var email = ""
    @State private var password = ""
    @State private var isSubmitting = false
    @State private var errorMessage: String?
    @State private var needsConfirmation = false
    @State private var showServerSheet = false
    @FocusState private var focusedField: Field?

    private enum Field { case email, password }

    var body: some View {
        NavigationStack {
            ScrollView {
                VStack(spacing: 28) {
                    header

                    VStack(spacing: 12) {
                        TextField("E-Mail", text: $email)
                            .textContentType(.username)
                            .keyboardType(.emailAddress)
                            .textInputAutocapitalization(.never)
                            .autocorrectionDisabled()
                            .focused($focusedField, equals: .email)
                            .submitLabel(.next)
                            .onSubmit { focusedField = .password }
                            .padding()
                            .background(.fill.tertiary, in: .rect(cornerRadius: 14))

                        SecureField("Passwort", text: $password)
                            .textContentType(.password)
                            .focused($focusedField, equals: .password)
                            .submitLabel(.go)
                            .onSubmit { Task { await submit() } }
                            .padding()
                            .background(.fill.tertiary, in: .rect(cornerRadius: 14))
                    }

                    if let message = errorMessage ?? session.signOutReason {
                        Label(message, systemImage: "exclamationmark.circle.fill")
                            .font(.footnote)
                            .foregroundStyle(.red)
                            .frame(maxWidth: .infinity, alignment: .leading)
                    }

                    if needsConfirmation {
                        ResendConfirmationButton(email: email)
                    }

                    Button {
                        Task { await submit() }
                    } label: {
                        Group {
                            if isSubmitting { ProgressView() } else { Text("Anmelden") }
                        }
                        .frame(maxWidth: .infinity)
                        .padding(.vertical, 6)
                    }
                    .buttonStyle(.glassProminent)
                    .controlSize(.large)
                    .disabled(!canSubmit)

                    HStack {
                        VStack { Divider() }
                        Text("oder").font(.footnote).foregroundStyle(.secondary)
                        VStack { Divider() }
                    }

                    Button {
                        Task { await signInWithGoogle() }
                    } label: {
                        Label("Mit Google anmelden", systemImage: "globe")
                            .frame(maxWidth: .infinity)
                            .padding(.vertical, 6)
                    }
                    .buttonStyle(.glass)
                    .controlSize(.large)
                    .disabled(isSubmitting || !connectivity.isOnline)

                    if !connectivity.isOnline {
                        Label("Keine Verbindung – Anmeldung erst online möglich.", systemImage: "wifi.slash")
                            .font(.footnote)
                            .foregroundStyle(.secondary)
                    }

                    VStack(spacing: 14) {
                        NavigationLink("Passwort vergessen?") { ForgotPasswordView(prefilledEmail: email) }
                        NavigationLink("Noch kein Konto? Registrieren") { RegisterView() }
                    }
                    .font(.subheadline)

                    serverFooter
                }
                .padding(24)
                .frame(maxWidth: 480)
                .frame(maxWidth: .infinity)
            }
            .scrollDismissesKeyboard(.interactively)
            .sheet(isPresented: $showServerSheet) { ServerSelectionView() }
        }
    }

    private var header: some View {
        VStack(spacing: 12) {
            Image("Logo")
                .resizable()
                .scaledToFit()
                .foregroundStyle(Color.primary)
                .frame(width: 120, height: 120)
                .accessibilityLabel("akaKraft Braunschweig")
            Text("AkaKraft Online")
                .font(.largeTitle.bold())
            Text("Melde dich mit deinem Vereinskonto an.")
                .font(.subheadline)
                .foregroundStyle(.secondary)
        }
        .padding(.top, 32)
    }

    @ViewBuilder
    private var serverFooter: some View {
        if AppConfig.allowsCustomServer {
            Button {
                showServerSheet = true
            } label: {
                Label(session.server.displayName, systemImage: "server.rack")
                    .font(.footnote)
            }
            .buttonStyle(.borderless)
            .padding(.top, 8)
        }
    }

    private var canSubmit: Bool {
        !isSubmitting && connectivity.isOnline && email.contains("@") && !password.isEmpty
    }

    private func signInWithGoogle() async {
        let pkce = PKCE()
        isSubmitting = true
        errorMessage = nil
        defer { isSubmitting = false }
        do {
            let callback = try await webAuthenticationSession.authenticate(
                using: session.client.googleLoginURL(challenge: pkce.challenge),
                callback: .customScheme("akakraft"),
                additionalHeaderFields: [:]
            )
            guard let code = URLComponents(url: callback, resolvingAgainstBaseURL: false)?
                .queryItems?.first(where: { $0.name == "code" })?.value
            else { throw APIError.transport("Google-Anmeldung fehlgeschlagen.") }
            try await session.loginWithGoogle(code: code, verifier: pkce.verifier)
        } catch let error as ASWebAuthenticationSessionError where error.code == .canceledLogin {
            // Nutzer hat abgebrochen.
        } catch {
            errorMessage = APIError.from(error).localizedDescription
        }
    }

    private func submit() async {
        guard canSubmit else { return }
        isSubmitting = true
        errorMessage = nil
        needsConfirmation = false
        defer { isSubmitting = false }
        do {
            try await session.login(email: email, password: password)
        } catch APIError.emailNotConfirmed {
            needsConfirmation = true
            errorMessage = APIError.emailNotConfirmed.localizedDescription
        } catch {
            errorMessage = APIError.from(error).localizedDescription
        }
    }
}

private struct ResendConfirmationButton: View {
    let email: String
    @Environment(AppSession.self) private var session
    @State private var sent = false

    var body: some View {
        Button(sent ? "Bestätigungsmail gesendet" : "Bestätigungsmail erneut senden") {
            Task {
                _ = try? await session.client.sendPublic(.post, "auth/resend-confirmation", body: ["email": email.trimmed])
                sent = true
            }
        }
        .font(.footnote)
        .disabled(sent)
    }
}
