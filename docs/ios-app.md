# iOS-App – Architektur & Stand

Native SwiftUI-App unter `src/mobile/iOS` (Xcode 27, iOS 27, iPhone & iPad).
Funktionsumfang siehe [features.md](features.md).

## Grundprinzipien

| Thema | Umsetzung |
|---|---|
| **Design** | SwiftUI mit Liquid Glass: `TabView` (`.sidebarAdaptable` → Sidebar auf dem iPad), Tab-Leiste minimiert beim Scrollen. iPhone: max. 5 Tabs (Übersicht, Kalender, Hallenbuch, Suche, Mehr) – Inventar und Mängel liegen dort unter „Mehr“, auf dem iPad als eigene Tabs. Der Such-Tab hat bewusst keine `.search`-Rolle, damit „Mehr“ ganz rechts bleibt, Sheets mit Detents, Swipe-Actions, Kontextmenüs, SF Symbols |
| **Offline** | Jede GET-Antwort wird als rohes JSON in SwiftData (`CachedResponse`) gespeichert und offline angezeigt. `OfflineSync` lädt beim Start, beim Zurückkehren in die App und bei wiederkehrender Verbindung (max. alle 10 Min.) alle Listen-Endpunkte vor – auch nie geöffnete Seiten sind offline durchsuchbar. Bilder: gesehene im URL-Cache (400 MB), außerdem verkleinerte Vorschauen (max. 600 px, JPEG) von Werkzeug, Material, offenen Mängeln und Aufgaben in `PreviewImageStore` |
| **Read-only offline** | Schreibende Requests laufen ausschließlich über `APIClient.send`/`upload` und werfen offline `APIError.offline`. Zusätzlich deaktiviert `.requiresOnline()` alle Bedienelemente. Über der Tab-Leiste erscheint dann ein Offline-Hinweis |
| **Server wechselbar** | `ServerConfiguration` (Basis-URL, API-Pfad `/api`, Web-Pfad `/app`). Tokens im Keychain und Cache sind **pro Server und Nutzer** getrennt. Aktivieren: `AppConfig.allowsCustomServer = true` → Server-Auswahl im Login und in den Einstellungen |
| **Auth** | E-Mail/Passwort oder Google (`ASWebAuthenticationSession`, Einmal-Code + PKCE, Rücksprung über `akakraft://auth`). JWT im Keychain. Das Refresh-Token (HttpOnly-Cookie) wird aus `Set-Cookie` gelesen, im Keychain gespeichert und beim Erneuern als `Cookie`-Header gesendet. Bei 401 wird automatisch erneuert |
| **Berechtigungen** | `Permissions` bildet die Backend-Policies ab (`isMember`, `isPrivileged`, `isAdmin`, `isOwnerOrPrivileged`). Das Backend bleibt maßgeblich |

## Aufbau

```
Akakraft Online/
  App/            Einstieg, Root-Routing (Login / Freischaltung / App)
  Core/
    Server/       ServerConfiguration, ServerStore
    Networking/   APIClient, APIError, JSON (.NET-Datumsformate)
    Auth/         AppSession, Keychain
    Cache/        CachedResponse (SwiftData), Remote<T>, ActionRunner
    Connectivity/ NWPathMonitor
  Models/         Codable-DTOs je Bereich
  Features/       Dashboard, Halle, Inventar, Maengel, Gemeinschaft, Verein, Verwaltung, Profil, Shell
  Shared/         RemoteContent, RemoteImage, Foto-Upload, StatusPill
```

Neue Seite mit Offline-Unterstützung:

```swift
@State private var remote = Remote<[Foo]>("foo")
RemoteContent(remote: remote) { items in List(items) { … } }
  .task { await remote.load(session) }
```

## Funktionsabdeckung

| Bereich | iOS |
|---|---|
| Login, Registrierung, Passwort vergessen, Freischaltung ausstehend | ✅ |
| Login mit Google (System-Browser + PKCE) | ✅ |
| Dashboard (MOTD, Hallen-Check-in, offene Umfragen, Veranstaltungen, Bestand, Mängel) | ✅ |
| Hallenbelegung & Veranstaltungen inkl. Termin-CRUD nach Schreibrollen | ✅ |
| Hallenbuch inkl. 7-Tage-Regel und Statistik | ✅ |
| Campus-Parken (Check-in, Freigeben, Verlauf, Kennzeichen, Problem melden) | ✅ |
| Werkzeug inkl. Ausleihe, Historie, Verwaltung mit Foto & Anleitung | ✅ |
| Verbrauchsmaterial inkl. Bestand, Nachbestellung, Verwaltung | ✅ |
| Mängel inkl. Foto, Anmerkungen, Status, Zurückziehen | ✅ |
| Wunschliste, Umfragen, Aufgaben | ✅ |
| Verein: Info, Schlüssel, Dokumente (Upload), Zugänge, Projekte | ✅ |
| Fahrzeuge, Profil, Feedback | ✅ |
| CRM (Partner, Ansprechpartner, Kontakthistorie) | ✅ |
| Admin: Nutzer & Rollen, Feedback, Ablageorte, Push-Test, Wartung | ✅ |
| Admin: Kalender-Einstellungen, Parkkonten-Zugang, Ablageorte zusammenführen | 🌐 Link in die Web-App |
| Push-Benachrichtigungen | ⏳ braucht APNs-Schlüssel & Firebase-Konfiguration |
| Favoriten-Schnellzugriff | – Tab-Leiste und „Mehr“ übernehmen die Rolle |

## Offene Punkte

1. **Push:** Firebase-iOS-SDK, `GoogleService-Info.plist` und ein APNs-Schlüssel im Firebase-Projekt. Die Registrierung erfolgt über `POST /push/tokens`.
2. **Dokumente offline:** Dokumente werden aktuell online geöffnet und nicht vorab gespeichert.

Hinweis Google-Login: Der Einmal-Code liegt im Arbeitsspeicher des Backends. Bei mehr als einer Backend-Instanz braucht es Sticky Sessions oder einen verteilten Cache.

## App-Icon

Erzeugt aus `Logo_komplett_700.svg`: hell (Logo auf Weiß), dunkel (weißes Logo, transparent) und getönt (Graustufen). Das Logo liegt zusätzlich als Template-Bild `Logo` im Asset-Katalog.

## Lokal testen

- **Debug-Builds** (Start aus Xcode) verbinden sich mit `http://localhost:5153`, **Release-Builds** mit `https://app.akakraft-bs.de`.
- Lokal wird **ohne** `/api`-Präfix angefragt, wie beim Web-Frontend. So stimmt auch die bei Google hinterlegte Rücksprungadresse (`http://localhost:5153/auth/callback/google`).
- HTTP ist nur für lokale Adressen erlaubt (`NSAllowsLocalNetworking` in `Config/Info.plist`).
- In Debug-Builds steht der Server unten im Login und lässt sich dort umschalten. Auf einem echten iPhone funktioniert `localhost` nicht: Dort den Vereinsserver oder die IP des Macs eintragen (z. B. `http://192.168.1.20:5153`). Das Backend muss dann auf `0.0.0.0` lauschen.
- Tokens und Offline-Daten sind je Server getrennt; ein Wechsel vermischt also nichts.

## Bauen & Testen

```bash
cd src/mobile/iOS
xcodebuild -project "Akakraft Online.xcodeproj" -scheme "Akakraft Online" \
  -destination 'platform=iOS Simulator,name=iPhone 18 Pro' test
```

Offline-Modus testen: In Debug-Builds unter *Mehr → Profil & Einstellungen → „Offline-Modus simulieren“*.
