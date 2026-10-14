import SwiftUI

struct RootView: View {
    @Environment(AppSession.self) private var session
    @Environment(ConnectivityMonitor.self) private var connectivity
    @Environment(\.scenePhase) private var scenePhase

    var body: some View {
        Group {
            switch session.state {
            case .launching:
                ProgressView()
                    .task { await session.restore() }
            case .signedOut:
                LoginView()
            case .pendingApproval:
                PendingApprovalView()
            case .signedIn:
                MainTabView()
            }
        }
        .animation(.default, value: session.state)
        .onChange(of: connectivity.isOnline) { _, isOnline in
            if isOnline, session.state != .launching, session.state != .signedOut {
                Task { await session.refreshUser() }
            }
        }
        .onChange(of: scenePhase) { _, phase in
            if phase == .active, session.state == .pendingApproval {
                Task { await session.refreshUser() }
            }
        }
    }
}
