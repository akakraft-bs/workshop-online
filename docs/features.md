# AkaKraft Online – Funktionsumfang

Referenz für alle Clients (Web, iOS, Android). Beschreibt Seiten, Berechtigungen und Funktionen
sowie den jeweils genutzten API-Endpunkt. Alle Pfade sind relativ zur API-Basis
(`https://app.akakraft-bs.de/api`).

---

## 1. Rollen & Berechtigungsstufen

### Rollen

| Rolle (API)          | Anzeige              | Gruppe   |
|----------------------|----------------------|----------|
| `None`               | Kein Zugriff         | –        |
| `Member`             | Mitglied             | Mitglied |
| `Getraenkewart`      | Getränkewart         | Vorstand |
| `Grillwart`          | Grillwart            | Vorstand |
| `Hallenwart`         | Hallenwart           | Vorstand |
| `Veranstaltungswart` | Veranstaltungswart   | Vorstand |
| `Treasurer`          | Kassenwart           | Vorstand |
| `ViceChairman`       | 2. Vorsitzender      | Vorstand |
| `Chairman`           | 1. Vorsitzender      | Vorstand |
| `Moderator`          | Moderator            | –        |
| `Admin`              | Administrator        | –        |

Ein Nutzer kann mehrere Rollen haben.

### Berechtigungsstufen (Backend-Policies)

| Stufe              | Policy            | Wer                                         |
|--------------------|-------------------|---------------------------------------------|
| **Angemeldet**     | `JwtApi`          | jeder mit gültigem Token (auch `None`)      |
| **Mitglied**       | `AnyRole`         | mindestens eine Rolle ≠ `None`              |
| **Privilegiert**   | `VorstandOrAdmin` | Vorstand, Moderator oder Admin              |
| **Admin**          | `AdminOnly`       | `Admin`                                     |

In diesem Dokument bedeutet **Eigentümer**, dass der Nutzer den Datensatz selbst erstellt hat
bzw. ihn betrifft (z. B. eigene Ausleihe). „Eigentümer oder Privilegiert“ ist die häufigste Regel.

> Nutzer mit ausschließlich `None` sehen nur die Seite **Freischaltung ausstehend**.

---

## 2. Authentifizierung

| Funktion                         | Endpunkt                              | Hinweis |
|----------------------------------|---------------------------------------|---------|
| Login E-Mail/Passwort            | `POST /auth/login`                    | Antwort `{token, expiresAt}`; Refresh-Token als HttpOnly-Cookie `refresh_token`. `403 {error:"email_not_confirmed"}` wenn unbestätigt |
| Login mit Google (Web)           | `GET /auth/login/google` → `/auth/callback` | Leitet auf `{Frontend}/auth/callback?token=…` weiter |
| Login mit Google (iOS)           | `GET /auth/login/google?client=ios&code_challenge=…` → `/auth/callback` → `POST /auth/google/exchange` | Callback leitet auf `akakraft://auth?code=…`; Code ist 2 Min. gültig, einmalig und nur mit PKCE-Verifier (S256) einlösbar |
| Server-Version abfragen          | `GET /version` (anonym) | Liefert `{ version }` aus `APP_VERSION` (Docker-Build-Arg `BUILD_VERSION`, sonst `dev`); die iOS-App fällt bei älteren Backends auf `/app/version.json` zurück |
| Registrieren                     | `POST /auth/register`                 | E-Mail, Passwort (≥ 8 Zeichen), Anzeigename; Bestätigungsmail; Admins erhalten Push |
| E-Mail bestätigen                | `POST /auth/confirm-email`            | Token aus Mail-Link; loggt direkt ein |
| Bestätigung erneut senden        | `POST /auth/resend-confirmation`      | antwortet immer 200 |
| Passwort vergessen               | `POST /auth/request-password-reset`   | antwortet immer 200 |
| Passwort zurücksetzen            | `POST /auth/reset-password`           | Token aus Mail-Link |
| Token erneuern                   | `POST /auth/refresh`                  | rotiert das Refresh-Token (30 Tage gültig) |
| Aktueller Nutzer                 | `GET /auth/me`                        | `UserDto` inkl. Rollen |
| Abmelden                         | `POST /auth/logout`                   | widerruft Refresh-Token |

---

## 3. Navigation

| # | Menüpunkt          | Route               | Sichtbar für  | Badge |
|---|--------------------|---------------------|---------------|-------|
| 1 | Dashboard          | `/dashboard`        | Mitglied      | – |
| 2 | Hallenbelegung     | `/kalender`         | Mitglied      | – |
| 3 | Hallenbuch         | `/hallenbuch`       | Mitglied      | – |
| 4 | Campus-Parken      | `/parkplatz`        | Mitglied      | – |
| 5 | Mängelmelder       | `/mangel`           | Mitglied      | offene Mängel |
| 6 | Veranstaltungen    | `/veranstaltungen`  | Mitglied      | – |
| 7 | Werkzeug           | `/werkzeug`         | Mitglied      | – |
| 8 | Verbrauchsmaterial | `/verbrauchsmaterial` | Mitglied    | Bestand niedrig |
| 9 | Wunschliste        | `/wunsch`           | Mitglied      | – |
| 10 | Umfragen          | `/umfrage`          | Mitglied      | offene, unbeantwortete |
| 11 | Verein            | `/verein`           | Mitglied      | – |
| 12 | Projekte          | `/projekte`         | Mitglied      | – |
| 13 | Aufgaben          | `/aufgaben`         | Mitglied      | offene Aufgaben |
| 14 | Vorstandsbereich  | `/vorstand`         | Vorstand, Admin | – |
| 15 | Adminbereich      | `/admin`            | Admin         | ungelesenes Feedback |

Badges: `GET /users/me/badges` → `{pendingUmfragen, openMaengel, lowStock, unseenFeedback, openAufgaben}`.

Nutzermenü: **Profil/Einstellungen**, **Meine Fahrzeuge** (`/fahrzeuge`), **Abmelden**.
Global: **Feedback senden** (schwebender Button).

---

## 4. Seiten & Funktionen

### 4.1 Dashboard

| Funktion | Wer | Endpunkt |
|---|---|---|
| Nachricht des Tages (MOTD) anzeigen, Schweregrad `Info`/`Warning`/`Critical` | Mitglied | `GET /motd` |
| MOTD setzen / entfernen | Privilegiert | `PUT /motd`, `DELETE /motd` |
| Wer ist gerade an der Halle? | Mitglied | `GET /halle/anwesend` |
| An der Halle einchecken (optionale Nachricht, läuft automatisch ab) / auschecken | Mitglied | `POST /halle/checkin`, `DELETE /halle/checkin` |
| Offene Umfragen, an denen man noch nicht teilgenommen hat | Mitglied | `GET /umfrage` |
| Nächste Veranstaltungen | Mitglied | `GET /calendar/upcoming-events` |
| Material mit niedrigem Bestand (einklappbar), „Nachbestellt“ markieren | Mitglied / Privilegiert | `GET /verbrauchsmaterial`, `POST /verbrauchsmaterial/{id}/nachbestellen` |
| Offene Mängel (einklappbar) | Mitglied | `GET /mangel` |
| Schnellzugriff auf Favoriten-Seiten | Mitglied | `GET/PUT /users/me/preferences` (`favoriteRoutes`) |

### 4.2 Hallenbelegung (Kalender)

Kalender vom Typ `Hallenbelegung`, Monats-/Listenansicht.

| Funktion | Wer | Endpunkt |
|---|---|---|
| Kalender & Farben laden | Mitglied | `GET /calendar/configs` |
| Termine eines Zeitraums | Mitglied | `GET /calendar/events?from&to&type=Hallenbelegung` |
| Termin anlegen (optional mit eigenem Fahrzeug) | Schreibrolle des Kalenders oder Privilegiert | `POST /calendar/events` |
| Termin bearbeiten / löschen | Schreibrolle des Kalenders oder Privilegiert | `PUT/DELETE /calendar/events/{calendarId}/{eventId}` |

Schreibrecht: privilegiert **oder** eine der im Kalender hinterlegten `writeRoles`.

### 4.3 Veranstaltungen

Wie 4.2, aber Kalender vom Typ `Veranstaltungen`. Umfragen können mit einer Veranstaltung verknüpft sein.

### 4.4 Hallenbuch

Erfassung der Schraubzeiten in der Halle.

| Funktion | Wer | Endpunkt |
|---|---|---|
| Einträge seitenweise anzeigen | Mitglied | `GET /hallenbuch?page&pageSize` → `PagedResult` |
| Eintrag anlegen: Start, Ende, Beschreibung, Gastschrauber (Art: Kasten Premiumbier / 20 € PayPal, bezahlt?), Familie geschraubt, Fahrzeug | Mitglied | `POST /hallenbuch` |
| Eintrag bearbeiten / löschen | Eigentümer **innerhalb 7 Tagen** oder Privilegiert | `PUT/DELETE /hallenbuch/{id}` |
| Statistik als CSV-Download (Stunden je Nutzer: eigen/Gast/Familie) für Zeitraum | Privilegiert (Endpunkt: VorstandOrAdmin) | `GET /hallenbuch/statistik?from&to` (liefert `text/csv`) |

### 4.5 Campus-Parken

Gemeinsame Parkkonten des Uni-Parkportals.

| Funktion | Wer | Endpunkt |
|---|---|---|
| Übersicht: Konten (frei/belegt), eigene Berechtigung, anstehende Reservierungen | Mitglied | `GET /parkplatz/overview` |
| Einchecken (Konto, Fahrzeug/Kennzeichen, Einfahrt, voraussichtlich bis; Bestätigung bei fehlender Reservierung) | Mitglied | `POST /parkplatz/checkin` |
| Belegung ändern / freigeben | Eigentümer oder Privilegiert | `PUT /parkplatz/claims/{id}`, `POST /parkplatz/claims/{id}/freigeben` |
| Problem mit Konto melden | Mitglied | `POST /parkplatz/accounts/{id}/problem` |
| Verlauf (Belegungen & Kennzeichen-Änderungen) | Mitglied | `GET /parkplatz/historie?limit` |
| Kennzeichen eines Kontos anzeigen / hinzufügen / entfernen | Mitglied | `GET/POST/DELETE /parkplatz/accounts/{id}/kennzeichen` |
| Kennzeichen-Änderungshistorie | Mitglied | `GET /parkplatz/accounts/{id}/kennzeichen/historie` |
| Konten verwalten, Portal-Zugangsdaten setzen | Privilegiert (UI: Admin) | `GET /parkplatz/accounts`, `PUT /parkplatz/accounts/{id}`, `PUT /parkplatz/accounts/{id}/zugang` |

### 4.6 Mängelmelder

Status: `Offen` → `Kenntnisgenommen` → `Behoben` / `Abgelehnt`; vom Melder `Zurueckgezogen`.
Kategorie: `Halle`, `Werkzeug`, `Sonstiges`.

| Funktion | Wer | Endpunkt |
|---|---|---|
| Liste mit Filter nach Status/Kategorie | Mitglied | `GET /mangel` |
| Mangel melden (Titel, Beschreibung, Kategorie, Foto) | Mitglied | `POST /uploads/mangel`, `POST /mangel` |
| Inhalt bearbeiten | Eigentümer oder Privilegiert | `PATCH /mangel/{id}` |
| Zurückziehen | Eigentümer | `POST /mangel/{id}/zurueckziehen` |
| Status ändern (mit Notiz) | Privilegiert (UI) | `PATCH /mangel/{id}/status` |
| Anmerkung hinzufügen | Mitglied | `POST /mangel/{id}/anmerkungen` |
| Anmerkung bearbeiten / löschen | Eigentümer der Anmerkung oder Privilegiert | `PUT/DELETE /mangel/{id}/anmerkungen/{anmerkungId}` |

### 4.7 Werkzeug

| Funktion | Wer | Endpunkt |
|---|---|---|
| Liste (Karten/Tabelle), Suche, Kategorie-Filter, „nur ausgeliehen“, Sortierung (Name, neueste, Kategorie, Ablageort) | Mitglied | `GET /werkzeug`, `GET /storage-locations` |
| Detail: Bild, Beschreibung, Maße, Ablageort (farbig), Anleitung (PDF), Ausleihstatus, Überfällig | Mitglied | – |
| Ausleihen mit Rückgabedatum | Mitglied | `POST /werkzeug/{id}/ausleihen` |
| Zurückgeben | Ausleiher oder Privilegiert | `POST /werkzeug/{id}/zurueckgeben` |
| Rückgabedatum ändern | Ausleiher oder Privilegiert | `PATCH /werkzeug/{id}/rueckgabedatum` |
| Ausleih-Historie | Mitglied | `GET /werkzeug/{id}/historie` |
| Anlegen / Bearbeiten (inkl. Bild-Upload, Anleitung aus Dokumenten) / Löschen | Privilegiert | `POST/PUT/DELETE /werkzeug`, `POST /uploads/werkzeug` |

### 4.8 Verbrauchsmaterial

| Funktion | Wer | Endpunkt |
|---|---|---|
| Liste, Suche, Kategorie-Filter, „Bestand niedrig“ (Menge ≤ Mindestmenge) | Mitglied | `GET /verbrauchsmaterial` |
| Bestand +/- anpassen | Privilegiert | `PATCH /verbrauchsmaterial/{id}/quantity` (`{delta}`) |
| Als nachbestellt markieren | Privilegiert | `POST /verbrauchsmaterial/{id}/nachbestellen` |
| Anlegen / Bearbeiten (Bild, Einheit, Mindestmenge, Ablageort) / Löschen | Privilegiert | `POST/PUT/DELETE /verbrauchsmaterial`, `POST /uploads/verbrauchsmaterial` |

### 4.9 Wunschliste

Status: `Offen`, `Angeschafft`, `Abgelehnt`.

| Funktion | Wer | Endpunkt |
|---|---|---|
| Liste, sortiert nach Stimmen | Mitglied | `GET /wunsch` |
| Wunsch anlegen (Titel, Beschreibung, Link, Preisspanne) | Mitglied | `POST /wunsch` |
| Up-/Downvote (erneut tippen hebt auf) – nur offene Wünsche | Mitglied | `POST /wunsch/{id}/vote` |
| Bearbeiten | Eigentümer oder Privilegiert | `PUT /wunsch/{id}` |
| Schließen als Angeschafft/Abgelehnt mit Notiz | Privilegiert | `POST /wunsch/{id}/close` |

### 4.10 Umfragen

| Funktion | Wer | Endpunkt |
|---|---|---|
| Liste offener & geschlossener Umfragen | Mitglied | `GET /umfrage` |
| Erstellen (Frage, Beschreibung, Optionen, Mehrfachauswahl, Ergebnisse sichtbar / nach Schluss, Deadline, verknüpfte Veranstaltung) | Mitglied | `POST /umfrage` |
| Abstimmen / Stimme ändern | Mitglied, nur offen | `POST /umfrage/{id}/vote` |
| Enthalten | Mitglied, nur offen | `POST /umfrage/{id}/abstain` |
| Bearbeiten, Schließen, Löschen, Erinnerung senden | Ersteller oder Privilegiert | `PUT/DELETE /umfrage/{id}`, `POST …/close`, `POST …/remind` |

Ergebnisse sichtbar für: Ersteller, Privilegierte, alle wenn `resultsVisible`, oder nach Schluss wenn `revealAfterClose`.
Sonst liefert die API `voteCount`/`voters` als `null`.

### 4.11 Verein

Unterseiten:

| Unterseite | Funktion | Wer | Endpunkt |
|---|---|---|---|
| **Info** | Amtsträger mit Telefon/Adresse, Schlüsselhinterlegungen | Mitglied | `GET /verein/info` |
| | Schlüsselhinterlegung anlegen/bearbeiten/löschen | Privilegiert | `POST/PUT/DELETE /verein/info/schluessel` |
| **Dokumente** | Ordner mit Dokumenten anzeigen/öffnen | Mitglied | `GET /verein/dokumente` |
| | Ordner anlegen/löschen, Dokument hochladen/löschen | Privilegiert | `POST/DELETE /verein/dokumente/ordner`, `POST /uploads/dokument`, `POST/DELETE /verein/dokumente` |
| **Zugänge** | Gemeinsame Zugangsdaten (Anbieter + Zugangsdaten) anzeigen/kopieren | Mitglied | `GET /verein/zugaenge` |
| | Anlegen/bearbeiten/löschen | Privilegiert | `POST/PUT/DELETE /verein/zugaenge` |

### 4.12 Projekte

| Funktion | Wer | Endpunkt |
|---|---|---|
| Projekte mit Zeitplan (geplanter Start, Dauer in Wochen, tatsächlicher Start/Ende, erwartetes Ende), Status, Projektplan-Datei | Mitglied | `GET /verein/projekte` |
| Anlegen / Bearbeiten / Löschen, Projektplan hochladen | Privilegiert | `POST/PUT/DELETE /verein/projekte`, `POST /uploads/projektplan` |

### 4.13 Aufgaben

| Funktion | Wer | Endpunkt |
|---|---|---|
| Aufgaben mit Priorität (1–5), Status, Foto, Zuständigem | Mitglied | `GET /aufgaben` |
| Anlegen, bearbeiten (inkl. zuweisen, erledigt), löschen | Privilegiert | `POST/PUT/DELETE /aufgaben`, `POST /uploads/aufgabe`, `GET /users/assignable` |

### 4.14 Meine Fahrzeuge

| Funktion | Wer | Endpunkt |
|---|---|---|
| Eigene Fahrzeuge (Marke, Modell, Kennzeichen, Standard) verwalten | Mitglied (nur eigene) | `GET/POST/PUT/DELETE /fahrzeuge` |

Fahrzeuge werden in Hallenbuch, Hallenbelegung und Campus-Parken ausgewählt.

### 4.15 Profil & Einstellungen

| Funktion | Wer | Endpunkt |
|---|---|---|
| Anzeigename, Telefon, Adresse, Favoriten | Angemeldet | `GET/PUT /users/me/preferences` |
| Push-Benachrichtigungen registrieren / abmelden | Angemeldet | `POST /push/tokens`, `DELETE /push/tokens/{token}` |

### 4.16 Feedback

| Funktion | Wer | Endpunkt |
|---|---|---|
| Feedback mit aktueller Seite und App-Version senden (Web: Route + Release-Version, iOS/Android: `ios-app`/`android-app` + Plattform-Version); der Server ergänzt seine eigene Version (`serverVersion`) | Mitglied | `POST /feedback` |

### 4.17 Vorstandsbereich

| Funktion | Wer | Endpunkt |
|---|---|---|
| **CRM – Partner & Sponsoren**: Liste mit Status (`Potentiell`, `Angeschrieben`, `InVerhandlung`, `Aktiv`, `Abgelehnt`, `Inaktiv`), Kategorie, letzter Kontakt | Privilegiert | `GET /crm/partner` |
| Partner-Detail mit Ansprechpartnern und Kontakthistorie (Kanal, Reaktion, Zusammenfassung, nächste Schritte) | Privilegiert | `GET /crm/partner/{id}` |
| Partner, Ansprechpartner, Kontakteinträge anlegen/bearbeiten/löschen | Privilegiert | `POST/PUT/DELETE /crm/partner/…` |

### 4.18 Adminbereich

| Unterseite | Funktion | Endpunkt |
|---|---|---|
| Nutzerverwaltung | Nutzer auflisten, Rollen vergeben/entziehen, Nutzer löschen | `GET /users`, `POST/DELETE /users/{id}/roles/{role}`, `DELETE /users/{id}` |
| Ablageorte | Farben vergeben, umbenennen, zusammenführen, löschen | `/admin/ablageorte/…` |
| Kalender-Einstellungen | Google-Kalender abonnieren, Name/Farbe/Typ/Sichtbarkeit/Schreibrollen/Parkberechtigung | `/admin/calendar/…` |
| Campus-Parken | Parkkonten & Portal-Zugangsdaten | `/parkplatz/accounts/…` |
| Feedback | Liste, Status `New`/`Read`/`Done` | `GET /admin/feedback`, `PATCH /admin/feedback/{id}/status` |
| Test-Benachrichtigung | Push an einen/alle Nutzer | `POST /admin/push/test` |
| Wartung | Leerzeichen bereinigen, Thumbnails generieren | `POST /admin/trim-text-fields`, `POST /admin/backfill-thumbnails` |

Alle Admin-Funktionen: nur `Admin`.

---

## 5. Querschnittliches

- **Bilder** liegen im Objektspeicher; die API liefert absolute URLs (`imageUrl`, `thumbnailUrl`). Uploads sind `multipart/form-data` mit Feld `file` (max. 10 MB).
- **Datumswerte** sind UTC (ISO 8601). Enums werden als Strings serialisiert.
- **Anzeigenamen**: Wo vorhanden, ersetzt der `displayName` aus den Einstellungen den Namen aus dem Konto.
- **Push-Benachrichtigungen** laufen über Firebase Cloud Messaging (z. B. neue Mängel, Umfrage-Erinnerungen, neue Registrierungen).
