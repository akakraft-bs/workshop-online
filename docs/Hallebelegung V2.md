# Feature-Spezifikation: Hallenbelegung

Seite in der AkaKraft-App (Angular + ASP.NET), auf der Mitglieder sehen, welcher Werkstatt-Arbeitsplatz wann belegt oder frei ist, und Plätze reservieren können.

## Datenquelle

- Drei Arbeitsplätze, je ein Google Calendar:
  | Id | Name | Kurzname | Farbe |
  |----|------|----------|-------|
  | H  | Hebebühne | Hebebühne | `#1F4FB8` (Text weiß) |
  | N  | Platz neben der Hebebühne | Neben Bühne | `#F0A04B` (Text dunkel `#1C1E21`) |
  | M  | Motorradplatz unter dem Zwischenboden | Motorrad | `#2E7D6B` (Text weiß) |
- Die Platzfarben sind auf allen Plattformen gleich und unterscheiden sich auch in der Helligkeit, damit sie bei Farbschwäche unterscheidbar bleiben.
- Termin = Bezeichnung (Aktion, z. B. „Zahnriemen A3“), Person, Start, Ende.
- Freie Zeitfenster: Google Calendar **FreeBusy-API** für alle drei Kalender in einem Request, serverseitig im Backend.

## Kernregel: Werkstatt-Tag 03:00–03:00

- Die Halle ist rund um die Uhr nutzbar, die Zeitachse zeigt daher 24 h.
- Ein „Werkstatt-Tag“ läuft von **03:00 bis 03:00 Uhr am Folgetag**. Nachtsessions (z. B. Fr 18:00 – Sa 02:30) bleiben so ein durchgehender Balken in der Zeile von Freitag.
- Berechnung: `werkstattTag = (jetzt − 3h).Date`.
- Termine, die über 03:00 hinausgehen, werden am Zeilenende abgeschnitten und in der Folgezeile fortgesetzt (Randfall, kommt selten vor).
- Vor 03:00 steht der Vortag als erste Zeile mit dem Badge **„Läuft noch“**, darunter der Kalendertag mit dem Badge **„Heute“**.

## Ansicht „7 Tage“ (Standard)

- Die Liste beginnt mit dem **aktuellen Werkstatt-Tag**, nicht mit Montag, und zeigt 7 Tage (plus die Zeile „Läuft noch“, falls nötig).
- Navigation: ‹ / › springt jeweils 7 Tage. Header z. B. „Ab heute · 17.–23. Okt“.
- Umschalter: Tag | 7 Tage | Wochenenden.
- FreeBusy-Abfragezeitraum: `werkstattTag 03:00` bis `+7 Tage`.

### Bereiche der Seite (von oben nach unten)

1. **Als Nächstes frei**: eine Karte pro Platz.
   - Großer Text: nächstes freies Fenster („Jetzt · bis 18:00“, „Heute ab 20:00“).
   - Unterzeile: was läuft gerade oder was kommt danach („Läuft noch: Zahnriemen A3 · Alex“).
   - Zusätzlich eine dunkle Karte mit dem **nächsten komplett freien Wochenende** (alle drei Plätze Sa und So frei).
2. **Belegung der nächsten 7 Tage** (Timeline):
   - Eine Zeile pro Tag mit 3 Spuren (eine pro Platz). Links stehen Wochentag, Datum und gegebenenfalls ein Badge.
   - Zeitachse 03 → 03 Uhr, Beschriftung alle 3 h. **00:00 wird fett** dargestellt und bekommt eine vertikale Linie (bei 7/8 der Breite).
   - Nachtbereich 22:00–06:00 leicht grau hinterlegt.
   - Ein Termin ist ein farbiger Balken mit Titel, Person und Uhrzeit. Tooltip/Tap zeigt Details.
   - Aktueller Tag: rote Jetzt-Linie, die vergangene Zeit ist ausgegraut.
   - Wochenend-Zeilen haben einen leicht anderen Hintergrund.
   - Desktop: die Timeline scrollt horizontal ab einer Mindestbreite von ca. 980 px.
3. **Wochenenden im Überblick**: Kacheln für die nächsten 6 Wochenenden.
   - Pro Platz ein Kästchen für Samstag und eins für Sonntag (gefüllt = belegt, leer = frei).
   - Komplett freie Wochenenden sind grün hinterlegt und bekommen das Badge „Ganz frei“.
   - Link „Weitere Wochenenden“.

### Interaktion

- Button „Platz reservieren“ (Desktop) bzw. FAB oder „+“ (mobil).
- Ein Tap auf eine freie Lücke öffnet den Reservierungsdialog mit vorausgefülltem Platz und Zeitfenster.
- Ein Tap auf einen Termin zeigt die Details.

## Mobile Varianten

Mobil sieht die Seite gleich aus, nur kompakter:
- **„Jetzt in der Halle“**: Liste mit einem Eintrag pro Platz (Farbmarker, Name, aktueller oder nächster Termin, Status-Pill „Belegt“ / „Frei bis 10“ / „Frei“).
- **„Nächste 7 Tage“**: pro Tag drei dünne Minibalken (6–7 px) auf der Achse 03→03 Uhr, mit Mitternachtsstrich und roter Jetzt-Linie. Ein Tap auf einen Tag öffnet die Tagesansicht.
- **„Freie Wochenenden“**: 2 Kacheln mit den nächsten komplett freien Wochenenden.

### iOS (Liquid Glass)
- Großer Titel, schwebende Glas-Buttons oben (Kalender, Filter, Mehr).
- Inhalte in weißen Gruppen-Karten (Radius 26) auf `#F2F2F7`.
- Unten eine schwebende Glas-Tab-Bar (Start · **Halle** · Termine · Profil), daneben ein separater runder blauer Glas-Button „+“.
- Status-Farben: frei `#248A3D`, belegt / läuft noch `#D70015`, Jetzt-Linie `#FF3B30`.
- Nativ in SwiftUI: `.glassEffect()` statt `backdrop-filter`.

### Android (Material 3 / Material You)
- Top App Bar mit Avatar, Titel „Hallenbelegung“ und Datum/Uhrzeit.
- Filter-Chips: Alle | Hebebühne | Neben | Motorrad.
- Tonal-Surface-Karten (`surfaceContainerLow`), Listeneinträge 72 dp hoch mit runden Avataren in der Platzfarbe.
- Status-Labels: belegt `errorContainer`, frei grüner Container.
- Extended FAB „Reservieren“, Navigation Bar mit Active-Indicator-Pill.
- Dynamic Color darf die UI-Palette steuern, die drei Platzfarben bleiben aber fest.

## PWA / Web
- Responsive: Unter Tablet-Breite wird auf das mobile Layout umgeschaltet.
- Barrierefreiheit: echte `<button>`-Elemente, `aria-label` an Icon-Buttons, Kontrast mindestens 4.5:1.
