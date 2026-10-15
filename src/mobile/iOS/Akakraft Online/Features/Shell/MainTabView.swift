import SwiftUI

enum AppTab: Hashable {
    case dashboard, kalender, hallenbuch, inventar, maengel, mehr, suche
}

struct MainTabView: View {
    @Environment(AppSession.self) private var session
    @Environment(ConnectivityMonitor.self) private var connectivity
    @Environment(OfflineSync.self) private var offlineSync
    @Environment(\.scenePhase) private var scenePhase
    @Environment(\.horizontalSizeClass) private var sizeClass
    @SceneStorage("main.tab") private var selection: AppTab = .dashboard
    @State private var badges = Remote<Badges>("users/me/badges")
    @State private var users = Remote<[User]>("users")

    var body: some View {
        TabView(selection: $selection) {
            // Das iPhone zeigt höchstens 5 Tabs, sonst fasst iOS den Rest in einem eigenen
            // „More“-Tab zusammen. Dort stehen daher nur die wichtigsten Bereiche; Inventar
            // und Mängel liegen auf dem iPhone unter „Mehr“, auf dem iPad als eigene Tabs.
            Tab("Übersicht", systemImage: "house", value: AppTab.dashboard) {
                NavigationStack { DashboardView().rootTitle() }
            }
            Tab("Kalender", systemImage: "calendar", value: AppTab.kalender) {
                NavigationStack { KalenderView().rootTitle() }
            }
            Tab("Hallenbuch", systemImage: "book.closed", value: AppTab.hallenbuch) {
                NavigationStack { HallenbuchListView().rootTitle() }
            }
            Tab("Inventar", systemImage: "wrench.and.screwdriver", value: AppTab.inventar) {
                NavigationStack { InventarView().rootTitle() }
            }
            .hidden(isCompact)
            Tab("Mängel", systemImage: "exclamationmark.triangle", value: AppTab.maengel) {
                NavigationStack { MangelListView().rootTitle() }
            }
            .badge(badges.value?.openMaengel ?? 0)
            .hidden(isCompact)
            // Bewusst ohne `role: .search`: Such-Tabs stellt iOS immer ganz rechts dar,
            // „Mehr“ soll aber der letzte Tab sein.
            Tab("Suche", systemImage: "magnifyingglass", value: AppTab.suche) {
                NavigationStack { SearchView().rootTitle() }
            }
            Tab("Mehr", systemImage: "ellipsis", value: AppTab.mehr) {
                NavigationStack { MoreView(badges: badges.value, usersWithoutAccess: usersWithoutAccess).rootTitle() }
            }
            .badge(moreBadge)
        }
        .tabViewStyle(.sidebarAdaptable)
        .tabBarMinimizeBehavior(.onScrollDown)
        .tabViewBottomAccessory(isEnabled: !connectivity.isOnline) {
            OfflineAccessory()
        }
        .task {
            await loadBadges()
            await offlineSync.sync(session)
        }
        .onChange(of: selection) { Task { await loadBadges() } }
        .onChange(of: connectivity.isOnline) { _, online in
            guard online else { return }
            Task {
                await loadBadges()
                await offlineSync.syncIfNeeded(session)
            }
        }
        .onChange(of: scenePhase) { _, phase in
            guard phase == .active else { return }
            Task {
                await loadBadges()
                await offlineSync.syncIfNeeded(session)
            }
        }
        .onChange(of: isCompact, initial: true) { _, compact in
            if compact, selection == .inventar || selection == .maengel { selection = .dashboard }
        }
    }

    /// Offene Freischaltungen – nur für Admins (der Endpunkt ist AdminOnly).
    private var usersWithoutAccess: Int {
        session.permissions.isAdmin ? users.value?.withoutAccessCount ?? 0 : 0
    }

    private func loadBadges() async {
        await badges.load(session)
        if session.permissions.isAdmin { await users.load(session) }
    }

    private var isCompact: Bool { sizeClass == .compact }

    private var moreBadge: Int {
        guard let badges = badges.value else { return 0 }
        return badges.pendingUmfragen + badges.openAufgaben + (session.permissions.isAdmin ? badges.unseenFeedback : 0)
            + (isCompact ? badges.openMaengel : 0) + usersWithoutAccess
    }
}

extension View {
    /// Tab-Startseiten: großer Titel in der Kopfzeile neben den Aktionen (`inlineLarge`)
    /// statt darunter – spart den leeren Bereich über der Überschrift.
    func rootTitle() -> some View {
        toolbarTitleDisplayMode(.inlineLarge)
    }
}

struct OfflineAccessory: View {
    var body: some View {
        Label("Offline – nur Ansicht, keine Änderungen", systemImage: "wifi.slash")
            .font(.subheadline)
            .padding(.horizontal)
            .accessibilityLabel("Offline. Daten werden nur angezeigt, Änderungen sind nicht möglich.")
    }
}

extension AppTab: RawRepresentable {
    init?(rawValue: String) {
        switch rawValue {
        case "dashboard": self = .dashboard
        case "kalender": self = .kalender
        case "hallenbuch": self = .hallenbuch
        case "inventar": self = .inventar
        case "maengel": self = .maengel
        case "mehr": self = .mehr
        case "suche": self = .suche
        default: return nil
        }
    }

    var rawValue: String {
        switch self {
        case .dashboard: "dashboard"
        case .kalender: "kalender"
        case .hallenbuch: "hallenbuch"
        case .inventar: "inventar"
        case .maengel: "maengel"
        case .mehr: "mehr"
        case .suche: "suche"
        }
    }
}
