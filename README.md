# AkaKraft Online

Webbasierte Vereinsverwaltung für die AkaKraft Braunschweig. Die App bietet Mitgliedern Zugriff auf Werkzeuginventar, Verbrauchsmaterialien, Hallenbelegung, Veranstaltungsplanung und weitere Vereinsfunktionen – alles hinter Google-Login mit rollenbasierter Zugriffskontrolle.

> Teile dieses Projekts wurden mit Unterstützung von [Claude Code](https://claude.ai/code) (Anthropic) generiert.

---

## Funktionsumfang

| Bereich | Beschreibung |
|---|---|
| **Dashboard** | Nächste Veranstaltungen, konfigurierbarer Schnellzugriff (gerätübergreifend gespeichert) |
| **Hallenbelegung** | Wochenkalender auf Basis von Google Calendar, Termine anlegen/bearbeiten/löschen |
| **Veranstaltungen** | Agenda-Übersicht aller Veranstaltungskalender, Einträge verwalten |
| **Werkzeug** | Inventarverwaltung mit Ausleihe und Rückgabe |
| **Verbrauchsmaterial** | Bestandsübersicht mit Mengen und Mindestbeständen |
| **Nutzerverwaltung** | Rollen vergeben und entziehen (Admin) |
| **Kalender-Einstellungen** | Google-Kalender abonnieren, Typ und Farbe konfigurieren (Admin) |
| **Feedback** | Nutzer-Feedback einreichen und verwalten |
| **Profil** | Anzeigename setzen (wird als Präfix bei Hallenbelegungseinträgen verwendet) |

---

## Tech Stack

| Bereich | Technologie |
|---|---|
| Backend | ASP.NET Core 10 (Minimal API), Clean Architecture |
| Frontend | Angular 21, Angular Material 3, Standalone Components, Signals |
| Datenbank | PostgreSQL 18 via Entity Framework Core 10 |
| Authentifizierung | Google OAuth 2.0 + JWT Bearer + httpOnly Refresh-Token-Cookie |
| Dateiablage | MinIO (S3-kompatibel) |
| Kalender | Google Calendar API (Service Account) |
| Deployment | Kubernetes (k3s) |

---

## Projektstruktur

```
workshop-online/
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
| `Admin` | Voller Zugriff inkl. Nutzerverwaltung und Kalender-Einstellungen |

Alle Rollen von `Getraenkewart` bis `Chairman` gelten als **Vorstand** und können über die Policy `VorstandOnly` gemeinsam berechtigt werden.

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

Beim ersten Start liest das Backend die Variable `Admin:Email`. Ist ein Nutzer mit dieser E-Mail-Adresse bereits registriert, erhält er automatisch die Admin-Rolle. Vor dem ersten Login muss der Nutzer daher einmalig die App aufrufen und sich per Google anmelden – danach ist die Rolle gesetzt.

Alternativ direkt per SQL:

```sql
INSERT INTO "UserRoles" ("UserId", "Role", "AssignedAt")
SELECT "Id", 'Admin', NOW()
FROM "Users"
WHERE "Email" = 'deine@email.de';
```

---

## Google OAuth einrichten

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
