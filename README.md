# AkaKraft Online

Webbasierte Vereinsverwaltung für AkaKraft. Die App ermöglicht Mitgliedern den Zugriff auf das Werkzeuginventar, Verbrauchsmaterialien und weitere Vereinsfunktionen.

> Teile dieses Projekts wurden mit Unterstützung von [Claude Code](https://claude.ai/code) (Anthropic) generiert.

---

## Tech Stack

| Bereich | Technologie |
|---|---|
| Backend | ASP.NET Core 10 (Minimal API) |
| Frontend | Angular 21 + Angular Material |
| Datenbank | PostgreSQL via Entity Framework Core |
| Authentifizierung | Google OAuth 2.0 + JWT |
| Infrastruktur | Docker Compose |

---

## Projektstruktur

```
workshop-online/
├── infra/
│   └── docker-compose.yml          # PostgreSQL + Adminer
└── src/
    ├── backend/
    │   ├── AkaKraft.Domain/         # Entitäten, Enums (User, Role)
    │   ├── AkaKraft.Application/    # Interfaces, DTOs
    │   ├── AkaKraft.Infrastructure/ # EF Core, Services, DbContext
    │   └── AkaKraft.WebApi/         # API-Endpoints, Auth, Program.cs
    └── frontend/
        └── akakraft-online/         # Angular App
```

---

## Voraussetzungen

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Node.js 22+](https://nodejs.org/)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/)
- [dotnet-ef CLI](https://learn.microsoft.com/en-us/ef/core/cli/dotnet): `dotnet tool install --global dotnet-ef`

---

## Lokale Entwicklung

### 1. Konfiguration

Lege im Backend-Projekt die Secrets lokal an (niemals in die Versionskontrolle einchecken):

```bash
cd src/backend/AkaKraft.WebApi
dotnet user-secrets set "Authentication:Google:ClientId"     "<deine-client-id>"
dotnet user-secrets set "Authentication:Google:ClientSecret" "<dein-client-secret>"
dotnet user-secrets set "Authentication:Jwt:Key"             "<min-32-zeichen-zufallsschluessel>"
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=akakraft;Username=postgres;Password=<dein-passwort>"
```

Passe außerdem das Passwort in `infra/docker-compose.yml` auf denselben Wert an.

### 2. Datenbank starten

```bash
cd infra
docker compose up -d
```

### 3. Migrationen ausführen (einmalig)

```bash
cd src/backend
dotnet ef migrations add InitialCreate \
  --project AkaKraft.Infrastructure \
  --startup-project AkaKraft.WebApi
dotnet ef database update \
  --project AkaKraft.Infrastructure \
  --startup-project AkaKraft.WebApi
```

### 4. Backend starten

```bash
cd src/backend
dotnet run --project AkaKraft.WebApi
# → https://localhost:7160
```

### 5. Frontend starten

```bash
cd src/frontend/akakraft-online
npm install
npm start
# → http://localhost:4200
```

---

## Rollen & Berechtigungen

Neu registrierte Nutzer erhalten automatisch die Rolle `None` und haben keinen Zugriff. Ein Administrator vergibt Rollen über die Nutzerverwaltung.

| Rolle | Beschreibung |
|---|---|
| `None` | Kein Zugriff (Standard bei Neuregistrierung) |
| `Member` | Mitglied – Lesezugriff auf Inventar |
| `Getraenkewart` | Getränkeverwaltung |
| `Grillwart` | Grillverwaltung |
| `Hallenwart` | Hallenverwaltung |
| `Veranstaltungswart` | Veranstaltungsplanung |
| `Treasurer` | Kassenwart |
| `ViceChairman` | 2. Vorsitzender |
| `Chairman` | 1. Vorsitzender |
| `Admin` | Voller Zugriff inkl. Nutzerverwaltung |

Alle Rollen außer `None`, `Member` und `Admin` gelten als **Vorstand** und können über die Policy `VorstandOnly` gemeinsam berechtigt werden.

---

## Google OAuth einrichten

1. In der [Google Cloud Console](https://console.cloud.google.com/) ein Projekt anlegen
2. *APIs & Services → OAuth consent screen* konfigurieren (Scopes: `email`, `profile`)
3. *Credentials → OAuth 2.0 Client ID* erstellen (Typ: Webanwendung)
4. Autorisierte Weiterleitungs-URI eintragen: `https://localhost:7160/auth/callback/google`
5. Client ID und Secret per `dotnet user-secrets` eintragen (siehe oben)

---

## Ersten Admin anlegen

Nach dem ersten Login via Google hat der Nutzer die Rolle `None`. Die Admin-Rolle muss einmalig direkt in der Datenbank gesetzt werden:

```sql
INSERT INTO "UserRoles" ("UserId", "Role", "AssignedAt")
SELECT "Id", 'Admin', NOW()
FROM "Users"
WHERE "Email" = 'deine@email.de';
```

Danach können weitere Rollen über die Web-Oberfläche vergeben werden.

---

## KI-Unterstützung

Teile der Implementierung – darunter die Clean-Architecture-Projektstruktur, EF Core Konfiguration, Google OAuth Integration, JWT-Handling sowie Angular-Komponenten und Services – wurden mit Unterstützung von **Claude Code** (Anthropic) erstellt und anschließend manuell geprüft und angepasst.
