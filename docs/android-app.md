# Android-App – Architektur & Stand

Native Kotlin-App unter `src/mobile/Android` (Jetpack Compose, Material 3, minSdk 26 / targetSdk 37, Smartphone, Tablet & Foldable).
Funktionsumfang siehe [features.md](features.md), Gegenstück auf iOS: [ios-app.md](ios-app.md).

Package / Application-ID: `de.akakraftbs.android.workshoponline` (nach Veröffentlichung im Play Store nicht mehr änderbar).

## Grundprinzipien

| Thema | Umsetzung |
|---|---|
| **Design** | Jetpack Compose mit Material 3 im Vereins-Navy `#293748` (wie die Web-App). Material You (dynamische Farben) ist in `AkakraftTheme(dynamicColor = …)` vorbereitet, aber standardmäßig aus. `NavigationSuiteScaffold` zeigt auf Smartphones eine Navigationsleiste unten mit max. 5 Zielen (Übersicht, Kalender, Hallenbuch, Suche, Mehr) und auf Tablets/Foldables eine Navigation Rail, dort zusätzlich mit Inventar und Mängeln. Tab-Startseiten haben eine große, einklappende Titelleiste, Unterseiten eine kompakte mit Zurück-Pfeil. Edge-to-Edge, Badges an den Navigationszielen |
| **Offline** | Jede GET-Antwort wird als rohes JSON (gzip-komprimiert) in Room (`CachedResponse`) gespeichert und offline angezeigt. `OfflineSync` lädt alle Listen-Endpunkte vor (dieselbe Liste wie iOS): nach der Anmeldung, beim Zurückkehren in die App und bei wiederkehrender Verbindung (max. alle 10 Min.). Zusätzlich gibt es einen `OfflineSyncWorker` (WorkManager, alle 4 Std., nur mit Netz und genug Akku). Bilder: gesehene im Coil-Disk-Cache (400 MB), außerdem verkleinerte Vorschauen (max. 600 px, JPEG) von Werkzeug, Material, offenen Mängeln und Aufgaben im `PreviewImageStore` (`noBackupFilesDir`) |
| **Read-only offline** | Schreibende Requests laufen ausschließlich über `ApiClient.send`/`upload` und werfen offline `ApiError.Offline`. Bedienelemente nutzen `enabled = LocalIsOnline.current`. Über der Navigationsleiste erscheint dann ein Offline-Hinweis |
| **Server wechselbar** | `ServerConfiguration` (Basis-URL, API-Pfad `/api`, Web-Pfad `/app`). Zugangsdaten und Cache sind **pro Server und Nutzer** getrennt. Die Server-Auswahl im Login und in den Einstellungen ist über `AppConfig.allowsCustomServer` freigeschaltet (in Debug-Builds immer) |
| **Auth** | E-Mail/Passwort oder Google: Custom Tab, Einmal-Code + PKCE, Rücksprung über `akakraft://auth`. Die App nutzt bewusst denselben Backend-Ablauf wie iOS (`client=ios`), eine Backend-Änderung war nicht nötig. Den PKCE-Verifier sichert `SavedStateHandle`, damit der Login auch nach einem Prozess-Neustart klappt. JWT und Refresh-Token (aus `Set-Cookie` gelesen, beim Erneuern als `Cookie`-Header gesendet) liegen AES-GCM-verschlüsselt im DataStore, den Schlüssel hält der Android Keystore. Bei 401 wird automatisch erneuert, parallele Requests warten auf denselben Refresh |
| **Berechtigungen** | `Permissions` bildet die Backend-Policies ab (`isMember`, `isPrivileged`, `isAdmin`, `isOwnerOrPrivileged`). Das Backend bleibt maßgeblich |
| **Backup** | Aus (`allowBackup=false`, Ausschluss-Regeln für Cloud-Backup und Geräteübertragung). Die Zugangsdaten sind an den Keystore des Geräts gebunden und wären auf einem neuen Gerät ohnehin unlesbar |

## Aufbau

```
app/src/main/java/de/akakraftbs/android/workshoponline/
  AkakraftApplication   Hilt, WorkManager- & Coil-Konfiguration, App-weite Auslöser (Vordergrund, Verbindung)
  MainActivity          Compose-Einstieg, nimmt den Google-Rücksprung entgegen (singleTop)
  core/
    server/       ServerConfiguration, ServerStore (DataStore)
    network/      ApiClient (OkHttp), ApiError, JsonCoding (.NET-Datumsformate, ResilientEnumSerializer)
    auth/         AppSession, CredentialStore (Keystore), Pkce, AuthRedirects (Custom Tabs)
    cache/        ResponseCache (Room), Remote<T>, ActionRunner, OfflineSync + Worker, ImagePipeline, Endpoints
    connectivity/ ConnectivityMonitor (ConnectivityManager)
    di/           AppModule (Hilt)
  model/          @Serializable-DTOs je Bereich
  ui/
    RootScreen    Root-Routing (Login / Freischaltung / App)
    auth/         Login, Server-Auswahl, Freischaltung ausstehend
    shell/        Routes, MainScreen (Navigation), AppDestinations, MoreScreen
    dashboard/ halle/ inventar/ maengel/ gemeinschaft/ verein/ verwaltung/ suche/ profil/
                  Fachbereiche (Routen, ViewModels, Bildschirme)
    components/   AppScaffold, FormScaffold, RemoteList, Formularfelder, RemoteImage/Avatar, Navigator, FeatureViewModel
    theme/        Farbschema, Typografie
```

Bibliotheken (Versionen in `gradle/libs.versions.toml`): Compose BOM, Material 3 + Adaptive Navigation Suite, Navigation Compose (typsichere Routen), Hilt, Room, WorkManager, DataStore, OkHttp, kotlinx.serialization, androidx.browser (Custom Tabs), Coil 3.

Hinweis: Retrofit wird bewusst nicht verwendet. Der Cache arbeitet mit rohem JSON je Pfad, und das passt besser zu einem schlanken, generischen OkHttp-Client als zu typisierten Retrofit-Interfaces.

Neue Seite mit Offline-Unterstützung:

```kotlin
@HiltViewModel
class FooViewModel @Inject constructor(private val session: AppSession) : ViewModel() {
    val foos = remote<List<Foo>>("foo")
    init { viewModelScope.launch { foos.load(session) } }
}

@Composable
fun FooScreen(viewModel: FooViewModel = hiltViewModel()) {
    AppScaffold(title = "Foo") { padding ->
        RemoteContent(viewModel.foos, Modifier.padding(padding)) { items -> LazyColumn { … } }
    }
}
```

Abfragen, die auch `OfflineSync` vorlädt (z. B. Hallenbuch-Seite 1, Kalendermonate), stehen in `Endpoints`. Pfad und Query-Reihenfolge müssen exakt übereinstimmen, sonst trifft der Cache-Schlüssel nicht.

Neue Seite in die Navigation einhängen: `@Serializable`-Route in der Datei des Bereichs anlegen, in dessen `…Destinations()` registrieren und per `LocalNavigator.current.open(route)` öffnen.

## Funktionsabdeckung

| Bereich | Android |
|---|---|
| Login, Registrierung, Passwort vergessen, Freischaltung ausstehend | ✅ |
| Login mit Google (Custom Tab + PKCE) | ✅ |
| Dashboard (MOTD inkl. Bearbeiten, Hallen-Check-in, offene Umfragen, Veranstaltungen, Bestand, Mängel) | ✅ |
| Hallenbelegung & Veranstaltungen inkl. Termin-CRUD nach Schreibrollen | ✅ |
| Hallenbuch inkl. 7-Tage-Regel und Statistik (CSV teilen) | ✅ |
| Campus-Parken (Check-in, Freigeben, Verlauf, Kennzeichen, Problem melden) | ✅ |
| Werkzeug inkl. Ausleihe, Historie, Verwaltung mit Foto & Anleitung | ✅ |
| Verbrauchsmaterial inkl. Bestand, Nachbestellung, Verwaltung | ✅ |
| Mängel inkl. Foto, Anmerkungen, Status, Zurückziehen | ✅ |
| Wunschliste, Umfragen, Aufgaben | ✅ |
| Verein: Info, Schlüssel, Dokumente (Upload), Zugänge, Projekte | ✅ |
| Fahrzeuge, Profil (inkl. Bearbeiten), Feedback | ✅ |
| Suche (offline auf dem Cache) | ✅ |
| CRM (Partner, Ansprechpartner, Kontakthistorie) | ✅ |
| Admin: Nutzer & Rollen, Feedback, Ablageorte, Push-Test, Wartung | ✅ |
| Admin: Kalender-Einstellungen, Parkkonten-Zugang | 🌐 Link in die Web-App (wie iOS) |
| Push-Benachrichtigungen | ⏳ braucht Firebase-Konfiguration (`google-services.json`) |

### Umsetzung der Ansichten

- **Je Bereich eine Datei** unter `ui/<bereich>/` mit Routen, ViewModels und Bildschirmen (z. B. `ui/maengel/Maengel.kt`). `NavGraphBuilder.<bereich>Destinations()` registriert die Routen, gesammelt in `ui/shell/AppDestinations.kt`.
- **Tabs** sind eigene Graphen mit ihrer Startseite; alle Unterseiten liegen auf oberster Ebene und sind so aus jedem Tab erreichbar (z. B. Mangel-Detail aus Übersicht, Mängel und Suche). Inventar und Mängel gibt es zusätzlich als Unterseite (`InventarPageRoute`, `MaengelPageRoute`) für „Mehr“ auf dem Smartphone.
- **Detailseiten** bekommen das Objekt als JSON in der Route (`toArg()`/`fromArg()`) und zeigen es sofort – auch offline und nach Prozess-Neustart. Beim Anzeigen wird der aktuelle Stand aus der (zwischengespeicherten) Liste übernommen.
- **Formulare** sind Vollbild-Dialoge (`FormScaffold`: Schließen links, Sichern rechts). Kurze Eingaben (Check-in, Ordnername, Anmerkung bearbeiten) laufen über Dialoge.
- **ViewModels** erben von `FeatureViewModel`: `reloadAll()` lädt die Seite, `act { … }` führt schreibende Aktionen aus (Fehler als Dialog). Listen laden bei jedem Anzeigen neu (`LaunchedEffect(Unit) { viewModel.reload() }`) – so sind sie nach Änderungen auf Unterseiten aktuell, ohne Rückruf-Ketten wie auf iOS.
- **Gemeinsame Bausteine** in `ui/components/`: `RemoteList` (Pull-to-Refresh, Lade-/Fehler-/Offline-Zustand, „Stand: …“), `StatusPill`, `DetailRow`, `OverflowMenu`, `DateTimeField`/`DateField`, `DropdownField`, `SuggestionTextField`, `PhotoUploadField` (System-Photo-Picker), `SearchField`.
- iOS-Wischaktionen sind als Drei-Punkte-Menüs umgesetzt, „+“-Schaltflächen als Extended FAB.

## Offene Punkte

1. **App-Icon:** Noch das Standard-Icon von Android Studio. Adaptive Icon (Vordergrund + Hintergrund + monochrom für Themed Icons) aus `Logo_komplett_700.svg` erzeugen.
2. **Push:** Firebase-Android-SDK und `google-services.json`, Registrierung über `POST /push/tokens`, Laufzeit-Berechtigung `POST_NOTIFICATIONS`.
3. **Dokumente offline:** wie auf iOS noch nicht vorab gespeichert.
4. **Icons:** Aktuell `material-icons-extended`. Für den neuesten Material-Stil können später Material Symbols als Vector Drawables folgen.

## Lokal testen

- **Debug-Builds** verbinden sich mit `http://localhost:5153`, **Release-Builds** mit `https://app.akakraft-bs.de`.
- Damit `localhost` im Emulator bzw. auf dem Gerät beim Mac ankommt, vorher die Portweiterleitung einrichten:
  ```bash
  adb reverse tcp:5153 tcp:5153
  ```
  So stimmt auch die bei Google hinterlegte Rücksprungadresse (`http://localhost:5153/auth/callback/google`). Die Weiterleitung muss nach jedem Neustart des Emulators bzw. Neuverbinden des Geräts neu gesetzt werden.
- Alternativ im Emulator `http://10.0.2.2:5153` (Server-Auswahl → „Emulator-Host“). Google-Login funktioniert dann aber nicht, weil die Rücksprungadresse nicht passt.
- Wie bei iOS wird lokal **ohne** `/api`-Präfix angefragt.
- HTTP ist nur für `localhost`, `127.0.0.1` und `10.0.2.2` erlaubt (`res/xml/network_security_config.xml`).
- In Debug-Builds steht der Server unten im Login und lässt sich dort sowie unter *Mehr → Profil & Einstellungen* umschalten. Zugangsdaten und Offline-Daten sind je Server getrennt.
- Offline-Modus testen: In Debug-Builds unter *Mehr → Profil & Einstellungen → „Offline-Modus simulieren“*, oder im Emulator das Netzwerk abschalten.

## Bauen & Testen

Android Studio öffnet `src/mobile/Android` direkt. Auf der Kommandozeile (JDK aus Android Studio):

```bash
cd src/mobile/Android
export JAVA_HOME="/Applications/Android Studio.app/Contents/jbr/Contents/Home"
./gradlew :app:testDebugUnitTest   # Unit-Tests (Datumsformate, PKCE, Server-Eingabe, Modelle)
./gradlew :app:assembleDebug       # APK unter app/build/outputs/apk/debug/
./gradlew :app:lintDebug           # Lint, Bericht unter app/build/reports/
./gradlew :app:installDebug        # auf laufendem Emulator/Gerät installieren
```
