import SwiftUI

/// Zeigt Laden, Fehler oder Inhalt einer `Remote`-Ressource an.
struct RemoteContent<Value: Decodable, Content: View>: View {
    let remote: Remote<Value>
    @ViewBuilder let content: (Value) -> Content

    var body: some View {
        if let value = remote.value {
            content(value)
                .safeAreaInset(edge: .top, spacing: 0) {
                    if let failure = remote.failure {
                        ErrorBanner(message: failure.localizedDescription)
                    }
                }
        } else if let failure = remote.failure {
            ContentUnavailableView {
                Label(failure == .offline ? "Offline" : "Laden fehlgeschlagen",
                      systemImage: failure == .offline ? "wifi.slash" : "exclamationmark.triangle")
            } description: {
                Text(failure == .offline
                     ? "Für diese Seite ist noch nichts gespeichert. Öffne sie einmal mit Verbindung, dann ist sie auch offline verfügbar."
                     : failure.localizedDescription)
            }
        } else {
            ProgressView()
                .frame(maxWidth: .infinity, maxHeight: .infinity)
        }
    }
}

struct ErrorBanner: View {
    let message: String

    var body: some View {
        Label(message, systemImage: "exclamationmark.triangle.fill")
            .font(.footnote)
            .foregroundStyle(.white)
            .padding(.horizontal, 14)
            .padding(.vertical, 8)
            .background(.red.gradient, in: .capsule)
            .padding(.vertical, 6)
    }
}

/// Fußzeile „Stand: …“ – offline besonders wichtig, um das Alter der Daten zu sehen.
struct LastUpdatedFooter: View {
    let date: Date?
    @Environment(ConnectivityMonitor.self) private var connectivity

    var body: some View {
        if let date {
            HStack(spacing: 4) {
                if !connectivity.isOnline { Image(systemName: "wifi.slash") }
                Text("Stand: \(date.dayTimeString)")
            }
            .font(.caption)
            .foregroundStyle(.secondary)
            .frame(maxWidth: .infinity)
        }
    }
}

extension View {
    /// Deaktiviert schreibende Bedienelemente, solange keine Verbindung besteht.
    func requiresOnline() -> some View {
        modifier(RequiresOnlineModifier())
    }

    func actionAlert(_ runner: ActionRunner) -> some View {
        alert(
            "Aktion fehlgeschlagen",
            isPresented: Binding(
                get: { runner.errorMessage != nil },
                set: { if !$0 { runner.errorMessage = nil } }
            )
        ) {
            Button("OK", role: .cancel) {}
        } message: {
            Text(runner.errorMessage ?? "")
        }
    }
}

private struct RequiresOnlineModifier: ViewModifier {
    @Environment(ConnectivityMonitor.self) private var connectivity

    func body(content: Content) -> some View {
        content.disabled(!connectivity.isOnline)
    }
}
