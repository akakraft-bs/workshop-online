# AkaKraft Online

Webbasierte Vereinsverwaltung für die AkaKraft Braunschweig. Die App bietet Mitgliedern Zugriff auf Werkzeuginventar, Verbrauchsmaterialien, Hallenbelegung, Veranstaltungsplanung und weitere Vereinsfunktionen – alles hinter Login mit rollenbasierter Zugriffskontrolle.

> Teile dieses Projekts wurden mit Unterstützung von [Claude Code](https://claude.ai/code) (Anthropic) generiert.

---

## Funktionsumfang

| Bereich | Beschreibung |
|---|---|
| **Dashboard** | Nächste Veranstaltungen, konfigurierbarer Schnellzugriff (geräteübergreifend gespeichert), Nachricht des Tages |
| **Nachricht des Tages** | Vorstand/Admin kann eine tagesaktuelle Mitteilung mit Priorität (Info/Warnung/Kritisch) für alle Mitglieder setzen |
| **Hallenbelegung** | Wochenkalender auf Basis von Google Calendar, Termine anlegen/bearbeiten/löschen |
| **Veranstaltungen** | Agenda-Übersicht aller Veranstaltungskalender, Einträge verwalten |
| **Werkzeug** | Inventarverwaltung mit Ausleihe und Rückgabe, Ablageort-Autocomplete |
| **Verbrauchsmaterial** | Bestandsübersicht mit Mengen und Mindestbeständen, Badge bei niedrigem Bestand |
| **Mängelmelder** | Mängel und Schäden melden, mit Bild, Status und Anmerkungen; Badge für offene Mängel |
| **Wunschliste** | Anschaffungsvorschläge einreichen, per Up-/Downvote bewerten, Preis angeben, abschließen |
| **Umfragen** | Abstimmungen erstellen und teilnehmen; Enthaltung möglich; manuelle Push-Erinnerung (Ersteller/Vorstand); Badge für ausstehende Abstimmungen |
| **Hallenbuch** | Manuelle Nutzungseinträge der Halle inkl. Statistik-Export |
| **Aufgaben** | Aufgaben anlegen, zuweisen und erledigen; Badge für offene Aufgaben |
| **Verein** | Vereinsinfo, Dokumente und Zugangsdaten (rollengeschützt) |
| **Projekte** | Projektübersicht des Vereins |
| **Vorstandsbereich** | Einstiegsbereich für Vorstandsrollen (CRM und weitere Vorstandsfunktionen) |
| **Partner & Sponsoren (CRM)** | Potentielle und aktive Partner verwalten, Kontaktpersonen hinterlegen, Kontakthistorie mit Kanal und Reaktion pflegen (Vorstand) |
| **Adminbereich** | Zentraler Einstieg für Admin-Funktionen: Nutzerverwaltung, Kalender-Einstellungen, Ablageorte, Feedback-Verwaltung, Test-Benachrichtigungen, Thumbnail-Generierung |
| **Push-Benachrichtigungen** | Web-Push-Abonnement für Nutzer, Test-Versand durch Admin |
| **Profil** | Anzeigename und Kontaktdaten setzen |

---

## Tech Stack

| Bereich | Technologie |
|---|---|
| Backend | ASP.NET Core 10 (Minimal API), Clean Architecture |
| Frontend | Angular 21, Angular Material 3, Standalone Components, Signals |
| Datenbank | PostgreSQL 18 via Entity Framework Core 10 |
| Authentifizierung | Google OAuth 2.0 oder E-Mail/Passwort + JWT Bearer + httpOnly Refresh-Token-Cookie |
| Dateiablage | MinIO (S3-kompatibel) |
| Kalender | Google Calendar API (Service Account) |
| Versionierung | Semantic Release (Conventional Commits → SemVer), Version im App-Footer |
| Deployment | Kubernetes (k3s), Images via GitHub Container Registry (ghcr.io) |

---

## Projektstruktur

```
workshop-online/
├── .github/
│   └── workflows/
│       ├── release-and-build.yml   # Semantic Release + Docker-Images (main branch)
│       ├── build-frontend.yml      # Manueller Frontend-Build
│       └── build-backend.yml       # Manueller Backend-Build
├── infra/
│   └── docker-compose.yml          # Lokale Entwicklung: PostgreSQL + Adminer + MinIO
├── k8s/                             # Kubernetes-Manifeste (Produktion)
│   ├── namespace.yaml
│   ├── ingress.yaml
│   ├── secrets.yaml                 # Vorlage – echte Secrets niemals einchecken
│   ├── backend/
│   ├── frontend/
│   ├── postgres/
│   └── minio/
└── src/
    ├── backend/
    │   ├── AkaKraft.Domain/         # Entitäten, Enums
    │   ├── AkaKraft.Application/    # Interfaces, DTOs
    │   ├── AkaKraft.Infrastructure/ # EF Core, Services, Migrationen
    │   └── AkaKraft.WebApi/         # Minimal-API-Endpoints, Program.cs
    └── frontend/
        └── akakraft-online/         # Angular-App
```

---

## Rollen & Berechtigungen

Neu registrierte Nutzer erhalten automatisch die Rolle `None` und haben keinen Zugriff. Ein Administrator vergibt Rollen über die Nutzerverwaltung.

| Rolle | Beschreibung |
|---|---|
| `None` | Kein Zugriff (Standard bei Neuregistrierung) |
| `Member` | Mitglied – Lesezugriff auf Inventar und Kalender |
| `Getraenkewart` | Vorstandsrolle |
| `Grillwart` | Vorstandsrolle |
| `Hallenwart` | Vorstandsrolle |
| `Veranstaltungswart` | Vorstandsrolle – kann Veranstaltungskalender bearbeiten |
| `Treasurer` | Kassenwart (Vorstandsrolle) |
| `ViceChairman` | 2. Vorsitzender – erweiterte Schreibrechte |
| `Chairman` | 1. Vorsitzender – erweiterte Schreibrechte |
| `Moderator` | Moderationsrechte (Mängelmelder, Wunschliste, Aufgaben) |
| `Admin` | Voller Zugriff inkl. Nutzerverwaltung und Kalender-Einstellungen |

Alle Rollen von `Getraenkewart` bis `Chairman` gelten als **Vorstand** und werden gemeinsam mit `Admin` und `Moderator` über die Policy `VorstandOrAdmin` berechtigt. `Moderator` wird dabei wie Vorstand als „privilegiert" behandelt.

Schreibrechte auf einzelne Kalender werden pro Kalender über die Kalender-Einstellungen konfiguriert (Rollen-Whitelist).

---

## Lokale Entwicklung

### Voraussetzungen

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Node.js 22+](https://nodejs.org/)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/)
- dotnet-ef CLI: `dotnet tool install --global dotnet-ef`

### 1. Infrastruktur starten

```bash
cd infra
docker compose up -d
# PostgreSQL :5432 · Adminer :8080 · MinIO API :9000 · MinIO Console :9001
```

Passe die Passwörter in `infra/docker-compose.yml` nach Bedarf an.

### 2. Backend-Secrets setzen

```bash
cd src/backend/AkaKraft.WebApi
dotnet user-secrets set "Authentication:Google:ClientId"      "<client-id>"
dotnet user-secrets set "Authentication:Google:ClientSecret"  "<client-secret>"
dotnet user-secrets set "Authentication:Jwt:Key"              "<min-32-zeichen-zufallsschluessel>"
dotnet user-secrets set "Authentication:Jwt:Issuer"           "AkaKraft"
dotnet user-secrets set "Authentication:Jwt:Audience"         "AkaKraft"
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=akakraft;Username=postgres;Password=<passwort>"
dotnet user-secrets set "Admin:Email"                         "<deine@email.de>"
dotnet user-secrets set "Minio:Endpoint"                      "localhost:9000"
dotnet user-secrets set "Minio:AccessKey"                     "minioadmin"
dotnet user-secrets set "Minio:SecretKey"                     "CHANGE_ME_MINIO"
dotnet user-secrets set "Minio:BucketName"                    "akakraft"
dotnet user-secrets set "Minio:UseSSL"                        "false"
dotnet user-secrets set "Frontend:BaseUrl"                    "http://localhost:4200"
```

Für die Google Calendar API zusätzlich das Service-Account-JSON hinterlegen:

```bash
dotnet user-secrets set "GoogleCalendar:ServiceAccountJson" "$(cat pfad/zum/service-account.json)"
```

Für den E-Mail-Versand (Registrierungsbestätigung, Passwort-Reset):

```bash
dotnet user-secrets set "Email:SmtpHost"         "smtp.example.com"
dotnet user-secrets set "Email:SmtpPort"         "587"
dotnet user-secrets set "Email:SmtpUsername"     "<user>"
dotnet user-secrets set "Email:SmtpPassword"     "<passwort>"
dotnet user-secrets set "Email:FromAddress"      "noreply@example.com"
dotnet user-secrets set "Email:FromName"         "AkaKraft"
```

### 3. Backend starten

Migrationen werden beim Start **automatisch** angewendet.

```bash
cd src/backend
dotnet run --project AkaKraft.WebApi
# → https://localhost:7160/api
```

### 4. Frontend starten

```bash
cd src/frontend/akakraft-online
npm install
npm start
# → http://localhost:4200
```

---

## Ersten Admin anlegen

Beim ersten Start liest das Backend die Variable `Admin:Email`. Ist ein Nutzer mit dieser E-Mail-Adresse bereits registriert, erhält er automatisch die Admin-Rolle. Vor dem ersten Login muss der Nutzer daher einmalig die App aufrufen und sich anmelden – danach ist die Rolle gesetzt.

Alternativ direkt per SQL:

```sql
INSERT INTO "UserRoles" ("UserId", "Role", "AssignedAt")
SELECT "Id", 'Admin', NOW()
FROM "Users"
WHERE "Email" = 'deine@email.de';
```

---

## Authentifizierung

Die App unterstützt zwei Anmeldewege:

- **Google OAuth 2.0** – Login per Google-Konto
- **E-Mail/Passwort** – Registrierung mit Bestätigungsmail, Passwort-Reset per E-Mail

Beide Wege erzeugen ein JWT (Laufzeit konfigurierbar über `Authentication:Jwt:ExpiryMinutes`, Standard: 480 Minuten) und einen httpOnly Refresh-Token-Cookie (30 Tage). Der Refresh-Token wird bei jeder Nutzung rotiert.

### Google OAuth einrichten

1. In der [Google Cloud Console](https://console.cloud.google.com/) ein Projekt anlegen
2. *APIs & Services → OAuth consent screen* konfigurieren (Scopes: `email`, `profile`)
3. *Credentials → OAuth 2.0 Client ID* erstellen (Typ: Webanwendung)
4. Autorisierte Weiterleitungs-URI eintragen:
   - Lokal: `https://localhost:7160/api/auth/callback/google`
   - Produktion: `https://<domain>/api/auth/callback/google`
5. Client ID und Secret per `dotnet user-secrets` eintragen (siehe oben)

---

## Google Calendar API einrichten

1. In der Google Cloud Console die **Google Calendar API** aktivieren
2. Unter *IAM & Admin → Service Accounts* einen Service Account anlegen
3. JSON-Schlüssel herunterladen und als Secret hinterlegen (siehe oben)
4. Jeden Google-Kalender, der in der App erscheinen soll, mit der E-Mail-Adresse des Service Accounts **teilen** (mindestens Lesezugriff, für Schreiben: Bearbeiter)
5. In den **Kalender-Einstellungen** der App den Kalender abonnieren und Typ/Farbe konfigurieren

---

## EF Core Migrationen

Neue Migration erstellen (immer aus dem `workshop-online`-Verzeichnis):

```bash
dotnet ef migrations add <MigrationsName> \
  --project src/backend/AkaKraft.Infrastructure \
  --startup-project src/backend/AkaKraft.WebApi
```

Migration manuell anwenden (lokal, falls Auto-Migration deaktiviert):

```bash
dotnet ef database update \
  --project src/backend/AkaKraft.Infrastructure \
  --startup-project src/backend/AkaKraft.WebApi
```

---

## Versionierung & CI/CD

Die App nutzt **Semantic Release** mit Conventional Commits für automatisches Versioning nach SemVer:

| Commit-Typ | Beispiel | Versions-Bump |
|---|---|---|
| `fix:` | `fix: Fehler bei Datumsanzeige behoben` | Patch (1.0.0 → 1.0.1) |
| `feat:` | `feat: Wunschliste hinzugefügt` | Minor (1.0.0 → 1.1.0) |
| `feat!:` / `BREAKING CHANGE:` | `feat!: API-Struktur geändert` | Major (1.0.0 → 2.0.0) |

Bei jedem Push auf `main` läuft der Workflow `.github/workflows/release-and-build.yml`:
1. Semantic Release ermittelt die neue Version und erstellt ein GitHub Release
2. Docker-Images für Frontend und Backend werden gebaut und in die GitHub Container Registry gepusht
3. Die Versionsnummer wird als Build-Arg in das Frontend-Image gebacken und im App-Footer als Link auf das GitHub Release angezeigt

---

## Kubernetes-Deployment (Produktion)

Die App läuft auf einem k3s-Cluster hinter einem Ingress-Controller. Container-Images werden über die GitHub Container Registry (`ghcr.io`) bereitgestellt.

### Secrets anlegen

```bash
kubectl apply -f k8s/namespace.yaml

kubectl create secret generic akakraft-secrets \
  --namespace akakraft \
  --from-literal=db-password='<passwort>' \
  --from-literal=jwt-key='<min-32-zeichen-schluessel>' \
  --from-literal=google-client-id='<client-id>' \
  --from-literal=google-client-secret='<client-secret>' \
  --from-literal=admin-email='<admin@email.de>' \
  --from-literal=minio-secret-key='<minio-passwort>'

kubectl create secret generic google-calendar-secret \
  --namespace akakraft \
  --from-file=service-account-json=k8s/secrets/akakraft-online-*.json

kubectl create secret docker-registry ghcr-pull-secret \
  --namespace akakraft \
  --docker-server=ghcr.io \
  --docker-username=<github-username> \
  --docker-password=<github-pat>
```

### Manifeste deployen

```bash
kubectl apply -f k8s/postgres/
kubectl apply -f k8s/minio/
kubectl apply -f k8s/backend/
kubectl apply -f k8s/frontend/
kubectl apply -f k8s/ingress.yaml
```

### Rollout-Update nach neuem Image

```bash
kubectl rollout restart deployment/akakraft-backend  -n akakraft
kubectl rollout restart deployment/akakraft-frontend -n akakraft
```

---

## KI-Unterstützung

Teile der Implementierung – darunter die Clean-Architecture-Projektstruktur, EF Core Konfiguration, Google OAuth- und Google Calendar-Integration, JWT- und Refresh-Token-Handling sowie Angular-Komponenten und Services – wurden mit Unterstützung von **Claude Code** (Anthropic) erstellt und anschließend manuell geprüft und angepasst.
