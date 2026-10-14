import SwiftUI

/// Alle Bereiche, die nicht in der Tab-Leiste stehen – wie das Seitenmenü der Web-App.
struct MoreView: View {
    let badges: Badges?

    @Environment(AppSession.self) private var session

    var body: some View {
        List {
            if let user = session.user {
                Section {
                    NavigationLink {
                        ProfilView()
                    } label: {
                        HStack(spacing: 14) {
                            AvatarView(name: user.shownName, pictureURL: user.pictureUrl.asURL, size: 48)
                            VStack(alignment: .leading, spacing: 2) {
                                Text(user.shownName).font(.headline)
                                Text("Profil & Einstellungen").font(.subheadline).foregroundStyle(.secondary)
                            }
                        }
                        .padding(.vertical, 2)
                    }
                }
            }

            Section("Halle") {
                link("Hallenbuch", "book.closed", tint: .brown) { HallenbuchListView() }
                link("Campus-Parken", "parkingsign.circle", tint: .blue) { ParkenView() }
                link("Meine Fahrzeuge", "car", tint: .gray) { FahrzeugeView() }
            }

            Section("Mitmachen") {
                link("Umfragen", "chart.bar.doc.horizontal", tint: .indigo, badge: badges?.pendingUmfragen) { UmfrageListView() }
                link("Wunschliste", "sparkles", tint: .pink) { WunschListView() }
                link("Aufgaben", "checklist", tint: .green, badge: badges?.openAufgaben) { AufgabenListView() }
            }

            Section("Verein") {
                link("Vereinsinfo", "person.3", tint: .teal) { VereinInfoView() }
                link("Dokumente", "folder", tint: .cyan) { DokumenteView() }
                link("Zugänge", "key", tint: .yellow) { ZugaengeView() }
                link("Projekte", "hammer", tint: .orange) { ProjekteListView() }
            }

            if session.permissions.canSeeVorstandsbereich || session.permissions.isAdmin {
                Section("Verwaltung") {
                    if session.permissions.canSeeVorstandsbereich {
                        link("Partner & Sponsoren", "person.crop.rectangle.stack", tint: .purple) { CrmListView() }
                    }
                    if session.permissions.isAdmin {
                        link("Adminbereich", "shield.lefthalf.filled", tint: .red, badge: badges?.unseenFeedback) { AdminView() }
                    }
                }
            }
        }
        .navigationTitle("Mehr")
    }

    private func link<Destination: View>(
        _ title: String, _ symbol: String, tint: Color, badge: Int? = nil,
        @ViewBuilder destination: @escaping () -> Destination
    ) -> some View {
        NavigationLink {
            destination()
        } label: {
            Label {
                Text(title)
            } icon: {
                Image(systemName: symbol)
                    .foregroundStyle(.white)
                    .font(.footnote.weight(.semibold))
                    .frame(width: 28, height: 28)
                    .background(tint.gradient, in: .rect(cornerRadius: 7))
            }
        }
        .badge(badge ?? 0)
    }
}
