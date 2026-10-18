import Foundation
import Network
import Observation

@Observable
final class ConnectivityMonitor {
    private(set) var networkAvailable = true
    /// Zum Testen des Offline-Modus in den Einstellungen.
    var simulateOffline = false

    var isOnline: Bool { networkAvailable && !simulateOffline }

    private let monitor = NWPathMonitor()

    init() {
        monitor.pathUpdateHandler = { [weak self] path in
            let available = path.status == .satisfied
            Task { @MainActor in self?.networkAvailable = available }
        }
        monitor.start(queue: DispatchQueue(label: "de.akakraft-bs.connectivity"))
    }

    deinit {
        monitor.cancel()
    }
}
