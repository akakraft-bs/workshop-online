import CoreLocation
import Foundation
import Observation
import UserNotifications

/// Standort-Check-in an der Halle.
///
/// Beim Betreten des Umkreises (Geofence) fragt eine Mitteilung, ob man einchecken möchte;
/// beim Verlassen wird automatisch ausgecheckt. Die Halle-Position pflegen Admins
/// (`/halle/standort`), sie wird lokal gemerkt, damit die Überwachung auch nach einem
/// Neustart im Hintergrund ohne Netz weiterläuft. Abschaltbar im Profil.
@Observable
final class HallenGeofence: NSObject {
    nonisolated static let notificationCategory = "HALLE_CHECKIN"
    nonisolated static let checkInAction = "HALLE_CHECKIN_JA"
    private static let monitorName = "de.akakraft-bs.halle"
    private static let conditionID = "halle"
    private static let promptID = "halle-checkin-frage"
    /// Am Rand des Umkreises springt das GPS gern hin und her – so oft wird höchstens gefragt.
    private static let promptCooldown: TimeInterval = 30 * 60

    private enum Keys {
        static let enabled = "halle.geofence.enabled"
        static let standort = "halle.geofence.standort"
        static let lastPrompt = "halle.geofence.lastPrompt"
    }

    /// Nutzer-Einstellung (Profil). Standardmäßig an – iOS fragt beim ersten Einschalten nach der Freigabe.
    var isEnabled: Bool {
        didSet {
            UserDefaults.standard.set(isEnabled, forKey: Keys.enabled)
            Task { await apply() }
        }
    }

    private(set) var authorization: CLAuthorizationStatus
    private(set) var standort: HallenStandort?

    /// Hintergrund-Überwachung braucht „Immer“; mit „Beim Verwenden“ greift sie nur bei geöffneter App.
    var hasAlwaysPermission: Bool { authorization == .authorizedAlways }
    var isDenied: Bool { authorization == .denied || authorization == .restricted }

    private let manager = CLLocationManager()
    private var monitor: CLMonitor?
    private var eventsTask: Task<Void, Never>?
    private weak var session: AppSession?

    override init() {
        isEnabled = UserDefaults.standard.object(forKey: Keys.enabled) as? Bool ?? true
        standort = UserDefaults.standard.data(forKey: Keys.standort)
            .flatMap { try? JSONDecoder().decode(HallenStandort.self, from: $0) }
        authorization = manager.authorizationStatus
        super.init()
        manager.delegate = self
        UNUserNotificationCenter.current().delegate = self
        UNUserNotificationCenter.current().setNotificationCategories([
            UNNotificationCategory(
                identifier: Self.notificationCategory,
                actions: [
                    UNNotificationAction(identifier: Self.checkInAction, title: "Einchecken", options: []),
                ],
                intentIdentifiers: [],
                options: []
            ),
        ])
    }

    /// Beim App-Start aufrufen – auch wenn iOS die App wegen eines Geofence-Ereignisses im
    /// Hintergrund startet, muss der Monitor sofort wieder angelegt werden.
    func attach(_ session: AppSession) {
        self.session = session
        Task { await apply() }
    }

    /// Lädt die Halle-Position vom Server (nach Anmeldung / beim Vorladen) und fragt bei Bedarf die Freigaben an.
    func refreshStandort() async {
        guard let session, session.state == .signedIn else { return }
        do {
            let data = try await session.client.getData("halle/standort")
            let decoded = try? session.client.decode(HallenStandort?.self, from: data)
            store(decoded ?? nil)
        } catch {
            return // offline: zuletzt bekannter Standort bleibt aktiv
        }
        if isEnabled, standort != nil { requestPermissions() }
        await apply()
    }

    /// Fragt Standort- („Immer“) und Mitteilungs-Freigabe an. iOS zeigt jede Abfrage nur einmal.
    func requestPermissions() {
        switch authorization {
        case .notDetermined, .authorizedWhenInUse:
            manager.requestAlwaysAuthorization()
        default:
            break
        }
        Task { _ = try? await UNUserNotificationCenter.current().requestAuthorization(options: [.alert, .sound]) }
    }

    /// Nach dem Abmelden: Überwachung beenden, damit niemand für ein fremdes Konto eingecheckt wird.
    func stop() async {
        eventsTask?.cancel()
        eventsTask = nil
        await monitor?.remove(Self.conditionID)
        UNUserNotificationCenter.current().removeDeliveredNotifications(withIdentifiers: [Self.promptID])
    }

    // MARK: - Überwachung

    private func store(_ standort: HallenStandort?) {
        self.standort = standort
        if let standort, let data = try? JSONEncoder().encode(standort) {
            UserDefaults.standard.set(data, forKey: Keys.standort)
        } else {
            UserDefaults.standard.removeObject(forKey: Keys.standort)
        }
    }

    private func apply() async {
        guard isEnabled, let standort, authorization == .authorizedAlways || authorization == .authorizedWhenInUse else {
            await stop()
            return
        }
        let monitor: CLMonitor
        if let existing = self.monitor {
            monitor = existing
        } else {
            monitor = await CLMonitor(Self.monitorName)
            self.monitor = monitor
        }
        let condition = CLMonitor.CircularGeographicCondition(
            center: CLLocationCoordinate2D(latitude: standort.latitude, longitude: standort.longitude),
            radius: CLLocationDistance(standort.radiusMeter)
        )
        await monitor.add(condition, identifier: Self.conditionID, assuming: .unsatisfied)

        guard eventsTask == nil else { return }
        eventsTask = Task { [weak self] in
            do {
                for try await event in await monitor.events {
                    guard event.identifier == Self.conditionID else { continue }
                    switch event.state {
                    case .satisfied: await self?.didEnter()
                    case .unsatisfied: await self?.didExit()
                    default: break
                    }
                }
            } catch {
                // Monitor beendet – wird beim nächsten apply() neu gestartet.
            }
            self?.eventsTask = nil
        }
    }

    private func didEnter() async {
        guard let session = await signedInSession() else { return }
        let lastPrompt = UserDefaults.standard.object(forKey: Keys.lastPrompt) as? Date ?? .distantPast
        guard Date.now.timeIntervalSince(lastPrompt) > Self.promptCooldown else { return }
        if await isCheckedIn(session) == true { return }

        UserDefaults.standard.set(Date.now, forKey: Keys.lastPrompt)
        let content = UNMutableNotificationContent()
        content.title = "An der Halle?"
        content.body = "Du bist in der Nähe der Halle. Möchtest du einchecken?"
        content.categoryIdentifier = Self.notificationCategory
        content.sound = .default
        try? await UNUserNotificationCenter.current().add(
            UNNotificationRequest(identifier: Self.promptID, content: content, trigger: nil)
        )
    }

    private func didExit() async {
        UNUserNotificationCenter.current().removeDeliveredNotifications(withIdentifiers: [Self.promptID])
        guard let session = await signedInSession(), await isCheckedIn(session) == true else { return }
        guard (try? await session.client.send(.delete, "halle/checkin")) != nil else { return }

        let content = UNMutableNotificationContent()
        content.title = "Ausgecheckt"
        content.body = "Du hast die Halle verlassen und wurdest automatisch ausgecheckt."
        try? await UNUserNotificationCenter.current().add(
            UNNotificationRequest(identifier: "halle-ausgecheckt", content: content, trigger: nil)
        )
    }

    fileprivate func checkIn() async {
        guard let session = await signedInSession() else { return }
        _ = try? await session.client.send(.post, "halle/checkin", body: HallenCheckInBody(message: nil))
    }

    /// Im Hintergrund-Start ist die Sitzung noch nicht wiederhergestellt.
    private func signedInSession() async -> AppSession? {
        guard let session else { return nil }
        if session.state == .launching { await session.restore() }
        return session.state == .signedIn ? session : nil
    }

    private func isCheckedIn(_ session: AppSession) async -> Bool? {
        guard let userId = session.user?.id,
              let present = try? await session.fetch([HallenCheck].self, path: "halle/anwesend").value
        else { return nil }
        return present.contains { $0.userId.caseInsensitiveCompare(userId) == .orderedSame }
    }
}

private nonisolated struct HallenCheckInBody: Encodable, Sendable {
    let message: String?
}

extension HallenGeofence: CLLocationManagerDelegate {
    nonisolated func locationManagerDidChangeAuthorization(_ manager: CLLocationManager) {
        let status = manager.authorizationStatus
        Task { @MainActor in
            self.authorization = status
            await self.apply()
        }
    }
}

extension HallenGeofence: UNUserNotificationCenterDelegate {
    nonisolated func userNotificationCenter(
        _ center: UNUserNotificationCenter,
        didReceive response: UNNotificationResponse
    ) async {
        guard response.actionIdentifier == HallenGeofence.checkInAction else { return }
        await checkIn()
    }

    nonisolated func userNotificationCenter(
        _ center: UNUserNotificationCenter,
        willPresent notification: UNNotification
    ) async -> UNNotificationPresentationOptions {
        [.banner, .sound]
    }
}
