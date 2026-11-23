import { CalendarEvent } from '../../../models/calendar.model';
import {
  anteil, belegungZusammenfassen, freieLueckeUm, naechstesFreiesFenster, tagesGrenzen,
  terminIntervall, textFarbeFuer, wochenendeAb, werkstattTag, zeitraum,
} from './werkstatt-zeit';

const d = (y: number, m: number, t: number, h = 0, min = 0) => new Date(y, m - 1, t, h, min);
const ev = (p: Partial<CalendarEvent>): CalendarEvent =>
  ({ id: 'x', calendarId: 'c', calendarName: 'C', calendarColor: '#000', title: 'T', isAllDay: false, ...p });

describe('werkstatt-zeit', () => {
  it('Werkstatt-Tag beginnt um 03:00', () => {
    expect(werkstattTag(d(2026, 10, 17, 2, 59))).toEqual(d(2026, 10, 16));
    expect(werkstattTag(d(2026, 10, 17, 3, 0))).toEqual(d(2026, 10, 17));
    expect(werkstattTag(d(2026, 10, 17, 23, 0))).toEqual(d(2026, 10, 17));
  });

  it('Tagesgrenzen laufen von 03:00 bis 03:00 am Folgetag', () => {
    const g = tagesGrenzen(d(2026, 10, 16));
    expect(g.start).toEqual(d(2026, 10, 16, 3));
    expect(g.end).toEqual(d(2026, 10, 17, 3));
  });

  it('Mitternacht liegt bei 7/8 der Zeile', () => {
    expect(anteil(d(2026, 10, 17, 0), tagesGrenzen(d(2026, 10, 16)))).toBeCloseTo(0.875);
  });

  it('Nachtsession Fr 18:00 – Sa 02:30 bleibt komplett in der Freitagszeile', () => {
    const iv = terminIntervall(ev({ start: d(2026, 10, 16, 18).toISOString(), end: d(2026, 10, 17, 2, 30).toISOString() }))!;
    const fr = tagesGrenzen(d(2026, 10, 16));
    expect(anteil(iv.start, fr)).toBeGreaterThan(0);
    expect(anteil(iv.end, fr)).toBeLessThan(1);
  });

  it('ganztägige Termine belegen ihre Werkstatt-Tage komplett', () => {
    const iv = terminIntervall(ev({ isAllDay: true, start: d(2026, 10, 17).toISOString(), end: d(2026, 10, 18).toISOString() }))!;
    expect(iv.start).toEqual(d(2026, 10, 17, 3));
    expect(iv.end).toEqual(d(2026, 10, 19, 3));
  });

  it('fasst überlappende Belegungen zusammen', () => {
    const r = belegungZusammenfassen([
      { start: d(2026, 1, 1, 12), end: d(2026, 1, 1, 14) },
      { start: d(2026, 1, 1, 10), end: d(2026, 1, 1, 12) },
      { start: d(2026, 1, 1, 16), end: d(2026, 1, 1, 17) },
    ]);
    expect(r).toEqual([
      { start: d(2026, 1, 1, 10), end: d(2026, 1, 1, 14) },
      { start: d(2026, 1, 1, 16), end: d(2026, 1, 1, 17) },
    ]);
  });

  describe('naechstesFreiesFenster', () => {
    const belegung = [
      { start: d(2026, 1, 1, 10), end: d(2026, 1, 1, 12) },
      { start: d(2026, 1, 1, 12), end: d(2026, 1, 1, 14) },
      { start: d(2026, 1, 1, 18), end: d(2026, 1, 1, 20) },
    ];
    const horizont = d(2026, 1, 8);

    it('jetzt frei bis zur nächsten Belegung', () => {
      expect(naechstesFreiesFenster(belegung, d(2026, 1, 1, 8), horizont)).toEqual({ start: d(2026, 1, 1, 8), end: d(2026, 1, 1, 10) });
    });

    it('während Belegung: frei ab Ende der zusammenhängenden Belegung', () => {
      expect(naechstesFreiesFenster(belegung, d(2026, 1, 1, 11), horizont)).toEqual({ start: d(2026, 1, 1, 14), end: d(2026, 1, 1, 18) });
    });

    it('nach der letzten Belegung offen bis Horizont', () => {
      expect(naechstesFreiesFenster(belegung, d(2026, 1, 1, 19), horizont)).toEqual({ start: d(2026, 1, 1, 20), end: null });
    });
  });

  it('freieLueckeUm liefert die Lücke zwischen Belegungen bzw. null', () => {
    const belegung = [
      { start: d(2026, 1, 1, 10), end: d(2026, 1, 1, 12) },
      { start: d(2026, 1, 1, 18), end: d(2026, 1, 1, 20) },
    ];
    const rahmen = tagesGrenzen(d(2026, 1, 1));
    expect(freieLueckeUm(belegung, d(2026, 1, 1, 15), rahmen)).toEqual({ start: d(2026, 1, 1, 12), end: d(2026, 1, 1, 18) });
    expect(freieLueckeUm(belegung, d(2026, 1, 1, 11), rahmen)).toBeNull();
    expect(freieLueckeUm(belegung, d(2026, 1, 1, 22), rahmen)).toEqual({ start: d(2026, 1, 1, 20), end: rahmen.end });
  });

  it('wochenendeAb liefert den Samstag (Sonntag zählt zum laufenden Wochenende)', () => {
    expect(wochenendeAb(d(2026, 11, 23))).toEqual(d(2026, 11, 28)); // Mo → Sa
    expect(wochenendeAb(d(2026, 11, 28))).toEqual(d(2026, 11, 28)); // Sa
    expect(wochenendeAb(d(2026, 11, 29))).toEqual(d(2026, 11, 28)); // So
  });

  it('formatiert Zeiträume', () => {
    expect(zeitraum(d(2026, 10, 17), d(2026, 10, 23))).toBe('17.–23. Okt');
    expect(zeitraum(d(2026, 10, 31), d(2026, 11, 1))).toBe('31. Okt – 1. Nov');
  });

  it('wählt kontrastreiche Textfarbe für die Platzfarben', () => {
    expect(textFarbeFuer('#1F4FB8')).toBe('#ffffff');
    expect(textFarbeFuer('#F0A04B')).toBe('#1C1E21');
    expect(textFarbeFuer('#2E7D6B')).toBe('#ffffff');
  });
});
