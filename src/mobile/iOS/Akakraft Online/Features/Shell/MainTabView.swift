import SwiftUI

enum AppTab: Hashable {
    case dashboard, kalender, inventar, maengel, mehr, suche
}

struct MainTabView: View {
    @Environment(AppSession.self) private var session
    @Environment(ConnectivityMonitor.self) private var connectivity
    @Environment(\.scenePhase) private var scenePhase
    @SceneStorage("main.tab") private var selection: AppTab = .dashboard
    @State private var badges = Remote<Badges>("users/me/badges")

    var body: some View {
        TabView(selection: $selection) {
            Tab("Übersicht", systemImage: "house", value: AppTab.dashboard) {
                NavigationStack { DashboardView() }
            }
            Tab("Kalender", systemImage: "calendar", value: AppTab.kalender) {
                NavigationStack { KalenderView() }
            }
            Tab("Inventar", systemImage: "wrench.and.screwdriver", value: AppTab.inventar) {
                NavigationStack { InventarView() }
            }
            Tab("Mängel", systemImage: "exclamationmark.triangle", value: AppTab.maengel) {
                NavigationStack { MangelListView() }
            }
            .badge(badges.value?.openMaengel ?? 0)
            Tab("Mehr", systemImage: "ellipsis", value: AppTab.mehr) {
                NavigationStack { MoreView(badges: badges.value) }
            }
            .badge(moreBadge)
            Tab(value: AppTab.suche, role: .search) {
                NavigationStack { SearchView() }
            }
        }
        .tabViewStyle(.sidebarAdaptable)
        .tabBarMinimizeBehavior(.onScrollDown)
        .tabViewBottomAccessory(isEnabled: !connectivity.isOnline) {
            OfflineAccessory()
        }
        .task { await badges.load(session) }
        .onChange(of: selection) { Task { await badges.load(session) } }
        .onChange(of: connectivity.isOnline) { _, online in
            if online { Task { await badges.load(session) } }
        }
        .onChange(of: scenePhase) { _, phase in
            if phase == .active { Task { await badges.load(session) } }
        }
    }

    private var moreBadge: Int {
        guard let badges = badges.value else { return 0 }
        return badges.pendingUmfragen + badges.openAufgaben + (session.permissions.isAdmin ? badges.unseenFeedback : 0)
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
        case .inventar: "inventar"
        case .maengel: "maengel"
        case .mehr: "mehr"
        case .suche: "suche"
        }
    }
}
