import SwiftUI
import SwiftData

@main
struct AkakraftApp: App {
    @State private var connectivity: ConnectivityMonitor
    @State private var session: AppSession
    @State private var offlineSync = OfflineSync()
    @State private var geofence: HallenGeofence
    private let container: ModelContainer

    init() {
        let container = OfflineStore.makeContainer()
        let connectivity = ConnectivityMonitor()
        self.container = container
        _connectivity = State(initialValue: connectivity)
        let session = AppSession(
            serverStore: ServerStore(),
            connectivity: connectivity,
            cache: ResponseCache(container: container)
        )
        _session = State(initialValue: session)
        // Früh anlegen: iOS startet die App bei Geofence-Ereignissen ggf. nur im Hintergrund.
        let geofence = HallenGeofence()
        geofence.attach(session)
        _geofence = State(initialValue: geofence)
    }

    var body: some Scene {
        WindowGroup {
            RootView()
                .environment(session)
                .environment(connectivity)
                .environment(offlineSync)
                .environment(geofence)
        }
        .modelContainer(container)
    }
}
