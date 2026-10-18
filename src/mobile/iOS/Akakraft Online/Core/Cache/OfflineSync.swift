import Foundation
import Observation

/// Lädt beim Start bzw. bei wiederkehrender Verbindung alle Listen in den Cache,
/// damit sie offline verfügbar sind – auch Seiten, die noch nie geöffnet wurden.
/// Bilder werden dabei nur als verkleinerte Vorschau gespeichert.
@Observable
final class OfflineSync {
    /// Mindestabstand zwischen zwei automatischen Durchläufen.
    static let interval: TimeInterval = 10 * 60

    private(set) var isRunning = false
    private(set) var lastCompleted: Date?
    private(set) var failedCount = 0

    /// Startet einen Durchlauf, wenn online und der letzte lange genug her ist.
    func syncIfNeeded(_ session: AppSession) async {
        if let lastCompleted, Date.now.timeIntervalSince(lastCompleted) < Self.interval { return }
        await sync(session)
    }

    func sync(_ session: AppSession) async {
        guard !isRunning, session.state == .signedIn, session.connectivity.isOnline else { return }
        isRunning = true
        defer { isRunning = false }

        var failures = 0
        let images = ImageCollector()

        await withTaskGroup(of: Bool.self) { group in
            for (path, query) in Self.endpoints(for: session.permissions) {
                group.addTask { await Self.attempt { try await session.prefetch(path, query: query) } }
            }
            group.addTask {
                await Self.attempt {
                    let tools = try await session.fetch([Werkzeug].self, path: "werkzeug").value
                    images.add(tools.map { ($0.thumbnailUrl, $0.imageUrl) })
                }
            }
            group.addTask {
                await Self.attempt {
                    let items = try await session.fetch([Verbrauchsmaterial].self, path: "verbrauchsmaterial").value
                    images.add(items.map { ($0.thumbnailUrl, $0.imageUrl) })
                }
            }
            group.addTask {
                await Self.attempt {
                    let defects = try await session.fetch([Mangel].self, path: "mangel").value
                    images.add(defects.filter { $0.status == .offen || $0.status == .kenntnisgenommen }.map { (nil, $0.imageUrl) })
                }
            }
            group.addTask {
                await Self.attempt {
                    let tasks = try await session.fetch([Aufgabe].self, path: "aufgaben").value
                    images.add(tasks.map { (nil, $0.fotoUrl) })
                }
            }
            for await succeeded in group where !succeeded {
                failures += 1
            }
        }

        await Self.storePreviews(images.pairs, session: session)
        failedCount = failures
        if failures == 0 || lastCompleted == nil { lastCompleted = .now }
    }

    // MARK: - Intern

    private static func attempt(_ work: () async throws -> Void) async -> Bool {
        do {
            try await work()
            return true
        } catch {
            return false
        }
    }

    /// Lädt Vorschaubilder mit begrenzter Parallelität; vorhandene werden übersprungen.
    private static func storePreviews(_ pairs: [(String?, String?)], session: AppSession) async {
        let jobs: [(source: URL, keys: [URL])] = pairs.compactMap { thumbnail, original in
            let keys = [thumbnail.asURL, original.asURL].compactMap(\.self)
            guard let source = keys.first else { return nil }
            return (source, keys)
        }
        await withTaskGroup(of: Void.self) { group in
            var iterator = jobs.makeIterator()
            for _ in 0..<4 {
                guard let job = iterator.next() else { break }
                group.addTask { await ImagePipeline.shared.storePreview(from: job.source, for: job.keys) }
            }
            for await _ in group {
                guard session.connectivity.isOnline, let job = iterator.next() else { continue }
                group.addTask { await ImagePipeline.shared.storePreview(from: job.source, for: job.keys) }
            }
        }
    }

    /// Alle lesenden Listen-Endpunkte; Pfad und Query müssen exakt denen der Ansichten entsprechen,
    /// sonst trifft der Cache-Schlüssel nicht.
    private static func endpoints(for permissions: Permissions) -> [(String, [URLQueryItem])] {
        let month = Calendar.current.dateInterval(of: .month, for: .now)?.start ?? .now
        let nextMonth = Calendar.current.date(byAdding: .month, value: 1, to: month) ?? month

        var list: [(String, [URLQueryItem])] = [
            ("users/me/badges", []),
            ("users/me/preferences", []),
            ("motd", []),
            ("halle/anwesend", []),
            ("halle/standort", []),
            ("calendar/upcoming-events", []),
            ("calendar/configs", []),
            ("storage-locations", []),
            ("verbrauchsmaterial/units", []),
            ("umfrage", []),
            ("wunsch", []),
            ("hallenbuch", HallenbuchListView.firstPageQuery),
            ("fahrzeuge", []),
            ("parkplatz/overview", []),
            ("parkplatz/historie", [URLQueryItem(name: "limit", value: "100")]),
            ("verein/info", []),
            ("verein/dokumente", []),
            ("verein/zugaenge", []),
            ("verein/projekte", []),
        ]
        for kind in CalendarKind.allCases {
            list.append(("calendar/events", CalendarEventsView.query(for: month, kind: kind)))
            list.append(("calendar/events", CalendarEventsView.query(for: nextMonth, kind: kind)))
        }
        if permissions.canSeeVorstandsbereich {
            list.append(("crm/partner", []))
            list.append(("users/assignable", []))
        }
        if permissions.isAdmin {
            list.append(("users", []))
            list.append(("admin/ablageorte", []))
        }
        return list
    }
}

/// Sammelt Bild-URLs aus parallelen Abrufen (alle auf dem MainActor).
private final class ImageCollector {
    private(set) var pairs: [(String?, String?)] = []

    func add(_ items: [(String?, String?)]) {
        pairs += items.filter { $0.0 != nil || $0.1 != nil }
    }
}
