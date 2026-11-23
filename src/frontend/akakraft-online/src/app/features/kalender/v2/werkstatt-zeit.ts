import { CalendarEvent } from '../../../models/calendar.model';

/**
 * Zeit-Logik der Hallenbelegung V2 (siehe docs/Hallebelegung V2.md).
 *
 * Ein „Werkstatt-Tag" läuft von 03:00 bis 03:00 Uhr am Folgetag, damit
 * Nachtsessions (z. B. Fr 18:00 – Sa 02:30) als ein Balken in einer Zeile bleiben.
 * Alles hier ist reine Logik ohne Angular – damit gut testbar.
 */

export const TAG_START_STUNDE = 3;
const MS_PRO_MINUTE = 60_000;

export interface Intervall {
  start: Date;
  end: Date;
}

/** Datum (00:00) des Werkstatt-Tags, zu dem der Zeitpunkt gehört: `(t − 3h).Date`. */
export function werkstattTag(t: Date): Date {
  const d = new Date(t);
  d.setHours(d.getHours() - TAG_START_STUNDE);
  d.setHours(0, 0, 0, 0);
  return d;
}

/** Beginn (03:00) und Ende (03:00 am Folgetag) eines Werkstatt-Tags. */
export function tagesGrenzen(tag: Date): Intervall {
  return {
    start: new Date(tag.getFullYear(), tag.getMonth(), tag.getDate(), TAG_START_STUNDE),
    end: new Date(tag.getFullYear(), tag.getMonth(), tag.getDate() + 1, TAG_START_STUNDE),
  };
}

export function addTage(d: Date, n: number): Date {
  return new Date(d.getFullYear(), d.getMonth(), d.getDate() + n, d.getHours(), d.getMinutes());
}

export function gleicherTag(a: Date, b: Date): boolean {
  return a.getFullYear() === b.getFullYear() && a.getMonth() === b.getMonth() && a.getDate() === b.getDate();
}

export function istWochenende(d: Date): boolean {
  return d.getDay() === 0 || d.getDay() === 6;
}

/**
 * Zeitraum eines Termins. Ganztägige Termine (Backend liefert inklusives Ende)
 * belegen ihre Werkstatt-Tage komplett, also von 03:00 bis 03:00 nach dem letzten Tag.
 */
export function terminIntervall(ev: CalendarEvent): Intervall | null {
  if (!ev.start) return null;
  const start = new Date(ev.start);
  const end = ev.end ? new Date(ev.end) : start;
  if (ev.isAllDay) {
    return {
      start: tagesGrenzen(new Date(start.getFullYear(), start.getMonth(), start.getDate())).start,
      end: tagesGrenzen(new Date(end.getFullYear(), end.getMonth(), end.getDate())).end,
    };
  }
  return { start, end: end > start ? end : new Date(start.getTime() + MS_PRO_MINUTE) };
}

export function ueberlappt(a: Intervall, b: Intervall): boolean {
  return a.start < b.end && b.start < a.end;
}

/** Position innerhalb einer Zeile als Anteil 0…1 (geklemmt). */
export function anteil(t: Date, zeile: Intervall): number {
  const gesamt = zeile.end.getTime() - zeile.start.getTime();
  return Math.min(1, Math.max(0, (t.getTime() - zeile.start.getTime()) / gesamt));
}

/** Überlappende/angrenzende Belegungen zusammenfassen, sortiert. */
export function belegungZusammenfassen(intervalle: Intervall[]): Intervall[] {
  const sortiert = [...intervalle].sort((a, b) => a.start.getTime() - b.start.getTime());
  const ergebnis: Intervall[] = [];
  for (const iv of sortiert) {
    const letztes = ergebnis[ergebnis.length - 1];
    if (letztes && iv.start <= letztes.end) {
      if (iv.end > letztes.end) letztes.end = iv.end;
    } else {
      ergebnis.push({ start: iv.start, end: iv.end });
    }
  }
  return ergebnis;
}

/**
 * Nächstes freies Fenster ab `ab`. `end === null` heißt: frei bis Ende des bekannten
 * Zeitraums (`horizont`), also keine weitere Belegung bekannt.
 */
export function naechstesFreiesFenster(belegung: Intervall[], ab: Date, horizont: Date): { start: Date; end: Date | null } | null {
  let kandidat = ab;
  for (const b of belegungZusammenfassen(belegung)) {
    if (b.end <= kandidat) continue;
    if (b.start > kandidat) return { start: kandidat, end: b.start };
    kandidat = b.end;
  }
  return kandidat < horizont ? { start: kandidat, end: null } : null;
}

/** Freie Lücke, die den Zeitpunkt `t` enthält (innerhalb von `rahmen`), oder null wenn belegt. */
export function freieLueckeUm(belegung: Intervall[], t: Date, rahmen: Intervall): Intervall | null {
  let start = rahmen.start;
  let end = rahmen.end;
  for (const b of belegungZusammenfassen(belegung)) {
    if (b.start <= t && t < b.end) return null;
    if (b.end <= t && b.end > start) start = b.end;
    if (b.start > t && b.start < end) end = b.start;
  }
  return { start, end };
}

/** Samstag (00:00) des Wochenendes, das am oder nach `tag` liegt (So zählt zum laufenden Wochenende). */
export function wochenendeAb(tag: Date): Date {
  const d = new Date(tag.getFullYear(), tag.getMonth(), tag.getDate());
  if (d.getDay() === 0) return addTage(d, -1);
  return addTage(d, (6 - d.getDay() + 7) % 7);
}

/** Auf das nächste Vielfache von `minuten` aufrunden. */
export function aufrunden(t: Date, minuten: number): Date {
  const ms = minuten * MS_PRO_MINUTE;
  return new Date(Math.ceil(t.getTime() / ms) * ms);
}

/** Auf das vorherige Vielfache von `minuten` abrunden. */
export function abrunden(t: Date, minuten: number): Date {
  const ms = minuten * MS_PRO_MINUTE;
  return new Date(Math.floor(t.getTime() / ms) * ms);
}

// ---------------------------------------------------------------------------
// Formatierung (deutsch)
// ---------------------------------------------------------------------------

const WOCHENTAGE = ['So', 'Mo', 'Di', 'Mi', 'Do', 'Fr', 'Sa'];
const MONATE = ['Jan', 'Feb', 'Mär', 'Apr', 'Mai', 'Jun', 'Jul', 'Aug', 'Sep', 'Okt', 'Nov', 'Dez'];

export function uhrzeit(t: Date): string {
  return `${String(t.getHours()).padStart(2, '0')}:${String(t.getMinutes()).padStart(2, '0')}`;
}

export function wochentag(t: Date): string {
  return WOCHENTAGE[t.getDay()];
}

/** „17. Okt" */
export function tagMonat(t: Date): string {
  return `${t.getDate()}. ${MONATE[t.getMonth()]}`;
}

/** „17.–23. Okt" bzw. „28. Okt – 3. Nov" */
export function zeitraum(von: Date, bis: Date): string {
  if (von.getMonth() === bis.getMonth()) return `${von.getDate()}.–${bis.getDate()}. ${MONATE[bis.getMonth()]}`;
  return `${tagMonat(von)} – ${tagMonat(bis)}`;
}

/** „Heute" / „Morgen" / „Sa 25. Okt" relativ zum Kalendertag von `jetzt`. */
export function tagRelativ(t: Date, jetzt: Date): string {
  if (gleicherTag(t, jetzt)) return 'Heute';
  if (gleicherTag(t, addTage(jetzt, 1))) return 'Morgen';
  return `${wochentag(t)} ${tagMonat(t)}`;
}

/** Zeitpunkt mit Tagesbezug: „18:00", „morgen 10:00", „Sa 25. Okt 10:00". */
export function zeitpunktRelativ(t: Date, jetzt: Date): string {
  if (gleicherTag(t, jetzt)) return uhrzeit(t);
  return `${tagRelativ(t, jetzt).replace('Morgen', 'morgen')} ${uhrzeit(t)}`;
}

/** Textfarbe mit ausreichendem Kontrast zur Hintergrundfarbe (#RRGGBB). */
export function textFarbeFuer(hex: string): string {
  const m = /^#?([0-9a-f]{6})$/i.exec(hex.trim());
  if (!m) return '#ffffff';
  const n = parseInt(m[1], 16);
  const kanal = (c: number) => {
    const s = c / 255;
    return s <= 0.03928 ? s / 12.92 : ((s + 0.055) / 1.055) ** 2.4;
  };
  const l = 0.2126 * kanal((n >> 16) & 255) + 0.7152 * kanal((n >> 8) & 255) + 0.0722 * kanal(n & 255);
  // Kontrast gegen Weiß vs. gegen #1C1E21 vergleichen
  const gegenWeiss = 1.05 / (l + 0.05);
  const gegenDunkel = (l + 0.05) / (0.0137 + 0.05);
  return gegenWeiss >= gegenDunkel ? '#ffffff' : '#1C1E21';
}
