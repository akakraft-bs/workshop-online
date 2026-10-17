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
    shell/        Routes, MainScreen (Navigation), MoreScreen
    profil/       Profil & Einstellungen
    components/   AppScaffold, RemoteContent, RemoteImage/Avatar, OfflineBanner, PlaceholderScreen
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

Neue Seite in die Navigation einhängen: Route in `ui/shell/Routes.kt` anlegen und im passenden Tab-Graphen in `MainScreen.kt` registrieren. Den Platzhalter (`PlaceholderScreen` bzw. `FeatureRoute`) dann ersetzen.

## Funktionsabdeckung

| Bereich | Android |
|---|---|
| Login, Freischaltung ausstehend | ✅ |
| Login mit Google (Custom Tab + PKCE) | ✅ |
| Registrierung, Passwort vergessen | 🌐 Link in die Web-App |
| App-Shell (Navigation, Badges, Offline-Hinweis, „Mehr“) | ✅ |
| Profil & Einstellungen (Server, Offline-Daten, Abmelden) | ✅ – Profil bearbeiten und Feedback fehlen noch |
| Offline-Cache & Vorladen inkl. Vorschaubilder | ✅ |
| Dashboard (MOTD, Hallen-Check-in, offene Umfragen, Veranstaltungen, Bestand, Mängel) | ⏳ Platzhalter mit Link in die Web-App |
| Hallenbelegung & Veranstaltungen inkl. Termin-CRUD nach Schreibrollen | ⏳ |
| Hallenbuch inkl. 7-Tage-Regel und Statistik | ⏳ |
| Campus-Parken (Check-in, Freigeben, Verlauf, Kennzeichen, Problem melden) | ⏳ |
| Werkzeug inkl. Ausleihe, Historie, Verwaltung mit Foto & Anleitung | ⏳ |
| Verbrauchsmaterial inkl. Bestand, Nachbestellung, Verwaltung | ⏳ |
| Mängel inkl. Foto, Anmerkungen, Status, Zurückziehen | ⏳ |
| Wunschliste, Umfragen, Aufgaben | ⏳ |
| Verein: Info, Schlüssel, Dokumente (Upload), Zugänge, Projekte | ⏳ |
| Fahrzeuge, Feedback | ⏳ |
| Suche | ⏳ |
| CRM (Partner, Ansprechpartner, Kontakthistorie) | ⏳ |
| Admin: Nutzer & Rollen, Feedback, Ablageorte, Push-Test, Wartung | ⏳ |
| Push-Benachrichtigungen | ⏳ braucht Firebase-Konfiguration (`google-services.json`) |

⏳ = noch nicht nativ umgesetzt. Bis dahin zeigt der Bereich einen Platzhalter mit Link in die Web-App.

## Offene Punkte

1. **Fachbereiche portieren**: in der Reihenfolge der Tabs (Übersicht, Kalender, Hallenbuch), danach die Bereiche unter „Mehr“. Die iOS-Views und -Modelle (`src/mobile/iOS/…/Features`, `Models`) dienen als Vorlage.
2. **App-Icon:** Noch das Standard-Icon von Android Studio. Adaptive Icon (Vordergrund + Hintergrund + monochrom für Themed Icons) aus `Logo_komplett_700.svg` erzeugen.
3. **Push:** Firebase-Android-SDK und `google-services.json`, Registrierung über `POST /push/tokens`, Laufzeit-Berechtigung `POST_NOTIFICATIONS`.
4. **Dokumente offline:** wie auf iOS noch nicht vorab gespeichert.
5. **Icons:** Aktuell `material-icons-extended`. Für den neuesten Material-Stil können später Material Symbols als Vector Drawables folgen.

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
