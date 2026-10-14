import SwiftUI
import SwiftData

@main
struct AkakraftApp: App {
    @State private var connectivity: ConnectivityMonitor
    @State private var session: AppSession
    private let container: ModelContainer

    init() {
        let container = OfflineStore.makeContainer()
        let connectivity = ConnectivityMonitor()
        self.container = container
        _connectivity = State(initialValue: connectivity)
        _session = State(initialValue: AppSession(
            serverStore: ServerStore(),
            connectivity: connectivity,
            cache: ResponseCache(container: container)
        ))
    }

    var body: some Scene {
        WindowGroup {
            RootView()
                .environment(session)
                .environment(connectivity)
        }
        .modelContainer(container)
    }
}
