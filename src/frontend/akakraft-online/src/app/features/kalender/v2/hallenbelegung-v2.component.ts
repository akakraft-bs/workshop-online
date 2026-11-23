import { Component, computed, DestroyRef, inject, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { NgTemplateOutlet } from '@angular/common';
import { BreakpointObserver } from '@angular/cdk/layout';
import { MatButtonModule } from '@angular/material/button';
import { MatButtonToggleModule } from '@angular/material/button-toggle';
import { MatDialog } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatTooltipModule } from '@angular/material/tooltip';
import { forkJoin } from 'rxjs';
import { AuthService } from '../../../core/auth/auth.service';
import { CalendarService } from '../../../core/calendar/calendar.service';
import { CalendarConfig, CalendarEvent } from '../../../models/calendar.model';
import { EventFormDialogComponent, EventFormDialogData, EventFormDialogResult } from '../event-form-dialog/event-form-dialog.component';
import { HallenAnsichtService } from '../hallen-ansicht.service';
import { TerminDetailsAktion, TerminDetailsData, TerminDetailsDialogComponent } from './termin-details-dialog.component';
import {
  abrunden, addTage, anteil, aufrunden, freieLueckeUm, gleicherTag, Intervall, istWochenende,
  naechstesFreiesFenster, tagesGrenzen, tagMonat, tagRelativ, terminIntervall, textFarbeFuer,
  ueberlappt, uhrzeit, wochenendeAb, werkstattTag, wochentag, zeitpunktRelativ, zeitraum,
} from './werkstatt-zeit';

export type Modus = 'tag' | 'woche' | 'wochenenden';

interface Platz {
  id: string;
  name: string;
  farbe: string;
  textFarbe: string;
}

interface Segment {
  event: CalendarEvent;
  links: number;   // Prozent
  breite: number;  // Prozent
  zeit: string;
  label: string;   // für aria-label / Tooltip
  weiterLinks: boolean;
  weiterRechts: boolean;
}

interface Spur {
  platz: Platz;
  segmente: Segment[];
}

interface Zeile {
  tag: Date;
  grenzen: Intervall;
  wochentag: string;
  datum: string;
  badge: 'Läuft noch' | 'Heute' | null;
  wochenende: boolean;
  spuren: Spur[];
  /** Prozent, falls „jetzt" in dieser Zeile liegt. */
  jetzt: number | null;
  /** Prozent der bereits vergangenen Zeit (0 = Zukunft, 100 = vorbei). */
  vergangen: number;
}

interface PlatzStatus {
  platz: Platz;
  haupt: string;
  unter: string;
  pill: { text: string; art: 'frei' | 'belegt' };
}

interface WochenendKachel {
  samstag: Date;
  label: string;
  ganzFrei: boolean;
  plaetze: { platz: Platz; sa: boolean; so: boolean }[];
}

const ACHSE = [3, 6, 9, 12, 15, 18, 21, 0, 3];
const WOCHENENDEN_UEBERSICHT = 6;
const WOCHENENDEN_IM_MODUS = 4;
const STANDARD_DAUER_MS = 3 * 60 * 60 * 1000;

@Component({
  selector: 'app-hallenbelegung-v2',
  imports: [NgTemplateOutlet, MatButtonModule, MatButtonToggleModule, MatIconModule, MatProgressSpinnerModule, MatTooltipModule],
  templateUrl: './hallenbelegung-v2.component.html',
  styleUrls: ['./hallenbelegung-v2.component.scss', './hallenbelegung-v2.mobile.scss'],
})
export class HallenbelegungV2Component implements OnInit {
  private readonly calendarService = inject(CalendarService);
  private readonly auth = inject(AuthService);
  private readonly dialog = inject(MatDialog);
  private readonly snackBar = inject(MatSnackBar);
  private readonly destroyRef = inject(DestroyRef);
  private readonly ansicht = inject(HallenAnsichtService);

  readonly ACHSE = ACHSE;

  readonly jetzt = signal(new Date());
  readonly isMobile = signal(false);
  readonly modus = signal<Modus>('woche');
  /** Erster Werkstatt-Tag der Timeline. */
  readonly startTag = signal(werkstattTag(new Date()));
  /** Mobil: geöffnete Tagesansicht. */
  readonly mobilTag = signal<Date | null>(null);
  readonly wochenendAnzahl = signal(WOCHENENDEN_UEBERSICHT);

  readonly configs = signal<CalendarConfig[]>([]);
  readonly timelineEvents = signal<CalendarEvent[]>([]);
  readonly uebersichtEvents = signal<CalendarEvent[]>([]);
  readonly loading = signal(true);

  private ladeZaehler = 0;

  // -------------------------------------------------------------------------
  // Plätze & Rechte
  // -------------------------------------------------------------------------

  readonly plaetze = computed<Platz[]>(() =>
    this.configs()
      .filter(c => c.isVisible)
      .sort((a, b) => a.sortOrder - b.sortOrder)
      .map(c => ({ id: c.googleCalendarId, name: c.name, farbe: c.color, textFarbe: textFarbeFuer(c.color) })),
  );

  readonly writableCalendarIds = computed(() => {
    const user = this.auth.currentUser();
    if (!user) return [];
    if (this.auth.isPrivileged()) return this.configs().map(c => c.googleCalendarId);
    const rollen = new Set(user.roles as string[]);
    return this.configs()
      .filter(c => c.writeRoles.some(r => rollen.has(r)))
      .map(c => c.googleCalendarId);
  });

  readonly canWrite = computed(() => this.plaetze().some(p => this.writableCalendarIds().includes(p.id)));

  // -------------------------------------------------------------------------
  // Timeline
  // -------------------------------------------------------------------------

  private readonly aktuellerTag = computed(() => werkstattTag(this.jetzt()));

  /** Läuft vor 03:00 noch der Werkstatt-Tag von gestern? */
  private readonly laeuftNoch = computed(() => !gleicherTag(this.aktuellerTag(), this.jetzt()));

  readonly istAktuell = computed(() => gleicherTag(this.startTag(), this.aktuellerTag()));

  /** 7 Tage ab Start (plus „Läuft noch"-Zeile vor 03:00) – Desktop-Modus „7 Tage" und mobile Liste. */
  private readonly wochenTage = computed(() => {
    const start = this.startTag();
    const anzahl = 7 + (this.istAktuell() && this.laeuftNoch() ? 1 : 0);
    return Array.from({ length: anzahl }, (_, i) => addTage(start, i));
  });

  private readonly modusTage = computed<Date[]>(() => {
    switch (this.modus()) {
      case 'tag': return [this.startTag()];
      case 'woche': return this.wochenTage();
      case 'wochenenden': {
        const sa = wochenendeAb(this.startTag());
        return Array.from({ length: WOCHENENDEN_IM_MODUS * 2 }, (_, i) => addTage(sa, Math.floor(i / 2) * 7 + (i % 2)));
      }
    }
  });

  readonly zeilen = computed(() => this.modusTage().map(t => this.zeileBauen(t)));
  readonly wochenZeilen = computed(() => this.wochenTage().map(t => this.zeileBauen(t)));
  readonly mobilZeile = computed(() => {
    const tag = this.mobilTag();
    return tag ? this.zeileBauen(tag) : null;
  });

  /** Termine des mobil geöffneten Tages, nach Platz gruppiert. */
  readonly mobilTermine = computed(() => {
    const zeile = this.mobilZeile();
    if (!zeile) return [];
    return zeile.spuren.map(s => ({ platz: s.platz, segmente: s.segmente }));
  });

  readonly kopfzeile = computed(() => {
    const tage = this.modusTage();
    const erster = tage[0];
    const letzter = tage[tage.length - 1];
    switch (this.modus()) {
      case 'tag': return `${tagRelativ(erster, this.jetzt())}${gleicherTag(erster, this.jetzt()) ? ' · ' + wochentag(erster) + ' ' + tagMonat(erster) : ''}`;
      case 'woche': {
        const sichtbarAb = this.istAktuell() && this.laeuftNoch() ? tage[1] : erster;
        return `${this.istAktuell() ? 'Ab heute · ' : ''}${zeitraum(sichtbarAb, letzter)}`;
      }
      case 'wochenenden': return `Wochenenden · ${zeitraum(erster, letzter)}`;
    }
  });

  readonly wochenKopfzeile = computed(() => {
    const tage = this.wochenTage();
    const ab = this.istAktuell() && this.laeuftNoch() ? tage[1] : tage[0];
    return `${this.istAktuell() ? 'Ab heute · ' : ''}${zeitraum(ab, tage[tage.length - 1])}`;
  });

  // -------------------------------------------------------------------------
  // „Als Nächstes frei" / „Jetzt in der Halle"
  // -------------------------------------------------------------------------

  private readonly uebersichtHorizont = computed(() => {
    const letzterSa = addTage(wochenendeAb(this.aktuellerTag()), (this.wochenendAnzahl() - 1) * 7);
    const wochenEnde = tagesGrenzen(addTage(this.aktuellerTag(), 7)).start;
    const wochenendEnde = tagesGrenzen(addTage(letzterSa, 1)).end;
    return wochenendEnde > wochenEnde ? wochenendEnde : wochenEnde;
  });

  readonly platzStatus = computed<PlatzStatus[]>(() => {
    const jetzt = this.jetzt();
    const horizont = this.uebersichtHorizont();
    return this.plaetze().map(platz => {
      const termine = this.termineMitIntervall(this.uebersichtEvents(), platz.id);
      const laufend = termine.find(t => t.iv.start <= jetzt && jetzt < t.iv.end);
      const naechster = termine.filter(t => t.iv.start > jetzt).sort((a, b) => +a.iv.start - +b.iv.start)[0];
      const fenster = naechstesFreiesFenster(termine.map(t => t.iv), jetzt, horizont);

      let haupt: string;
      if (!fenster) haupt = 'Vorerst belegt';
      else if (fenster.start <= jetzt) haupt = fenster.end ? `Jetzt · bis ${zeitpunktRelativ(fenster.end, jetzt)}` : 'Jetzt frei';
      else haupt = `${tagRelativ(fenster.start, jetzt)} ab ${uhrzeit(fenster.start)}`;

      let unter: string;
      if (laufend) unter = `Läuft noch: ${terminText(laufend.ev)}`;
      else if (naechster) unter = `Danach: ${terminText(naechster.ev)} · ${zeitpunktRelativ(naechster.iv.start, jetzt)}`;
      else unter = 'Keine weiteren Termine';

      let pill: PlatzStatus['pill'];
      if (laufend) pill = { text: 'Belegt', art: 'belegt' };
      else if (naechster && +naechster.iv.start - +jetzt < 24 * 60 * 60 * 1000) pill = { text: `Frei bis ${uhrzeit(naechster.iv.start)}`, art: 'frei' };
      else pill = { text: 'Frei', art: 'frei' };

      return { platz, haupt, unter, pill };
    });
  });

  // -------------------------------------------------------------------------
  // Wochenenden
  // -------------------------------------------------------------------------

  readonly wochenenden = computed<WochenendKachel[]>(() => {
    const erstesSa = wochenendeAb(this.aktuellerTag());
    return Array.from({ length: this.wochenendAnzahl() }, (_, i) => {
      const sa = addTage(erstesSa, i * 7);
      const saIv = tagesGrenzen(sa);
      const soIv = tagesGrenzen(addTage(sa, 1));
      const plaetze = this.plaetze().map(platz => {
        const ivs = this.termineMitIntervall(this.uebersichtEvents(), platz.id).map(t => t.iv);
        return { platz, sa: ivs.some(iv => ueberlappt(iv, saIv)), so: ivs.some(iv => ueberlappt(iv, soIv)) };
      });
      return {
        samstag: sa,
        label: zeitraum(sa, addTage(sa, 1)),
        ganzFrei: plaetze.every(p => !p.sa && !p.so),
        plaetze,
      };
    });
  });

  readonly naechstesFreiesWochenende = computed(() => this.wochenenden().find(w => w.ganzFrei) ?? null);
  readonly freieWochenenden = computed(() => this.wochenenden().filter(w => w.ganzFrei).slice(0, 2));

  // -------------------------------------------------------------------------
  // Lifecycle & Laden
  // -------------------------------------------------------------------------

  constructor() {
    this.breakpoints();
    const timer = setInterval(() => {
      const vorher = this.aktuellerTag();
      this.jetzt.set(new Date());
      // Über 03:00 hinweg: Übersicht für den neuen Werkstatt-Tag nachladen.
      if (!gleicherTag(vorher, this.aktuellerTag())) {
        if (gleicherTag(this.startTag(), vorher)) this.startTag.set(this.aktuellerTag());
        this.laden();
      }
    }, 60_000);
    this.destroyRef.onDestroy(() => clearInterval(timer));
  }

  private breakpoints(): void {
    inject(BreakpointObserver)
      .observe('(max-width: 839.98px)')
      .pipe(takeUntilDestroyed())
      .subscribe(r => this.isMobile.set(r.matches));
  }

  ngOnInit(): void {
    this.calendarService.getConfigs('Hallenbelegung').subscribe({
      next: configs => {
        this.configs.set(configs);
        this.laden();
      },
      error: () => {
        this.loading.set(false);
        this.snackBar.open('Kalender konnten nicht geladen werden.', 'OK', { duration: 4000 });
      },
    });
  }

  /** Lädt Timeline-Zeitraum und Übersicht (nächste Wochen) parallel. */
  laden(): void {
    const id = ++this.ladeZaehler;
    this.loading.set(true);
    const tage = [...this.modusTage(), ...this.wochenTage(), ...(this.mobilTag() ? [this.mobilTag()!] : [])];
    const von = tagesGrenzen(new Date(Math.min(...tage.map(Number)))).start;
    const bis = tagesGrenzen(new Date(Math.max(...tage.map(Number)))).end;

    forkJoin({
      timeline: this.calendarService.getEvents(von, bis, 'Hallenbelegung'),
      uebersicht: this.calendarService.getEvents(tagesGrenzen(this.aktuellerTag()).start, this.uebersichtHorizont(), 'Hallenbelegung'),
    }).subscribe({
      next: ({ timeline, uebersicht }) => {
        if (id !== this.ladeZaehler) return;
        this.timelineEvents.set(timeline);
        this.uebersichtEvents.set(uebersicht);
        this.loading.set(false);
      },
      error: () => {
        if (id !== this.ladeZaehler) return;
        this.loading.set(false);
        this.snackBar.open('Belegung konnte nicht geladen werden.', 'OK', { duration: 4000 });
      },
    });
  }

  // -------------------------------------------------------------------------
  // Navigation
  // -------------------------------------------------------------------------

  modusWechseln(modus: Modus): void {
    this.modus.set(modus);
    if (modus === 'wochenenden') this.startTag.set(this.aktuellerTag());
    this.laden();
  }

  zurueck(): void { this.blaettern(-1); }
  vor(): void { this.blaettern(1); }

  private blaettern(richtung: 1 | -1): void {
    const schritt = this.isMobile() ? 7 : { tag: 1, woche: 7, wochenenden: WOCHENENDEN_IM_MODUS * 7 }[this.modus()];
    this.startTag.set(addTage(this.startTag(), richtung * schritt));
    this.laden();
  }

  heute(): void {
    this.startTag.set(this.aktuellerTag());
    this.laden();
  }

  tagOeffnen(tag: Date): void {
    if (this.isMobile()) {
      this.mobilTag.set(tag);
      return;
    }
    this.modus.set('tag');
    this.startTag.set(tag);
    this.laden();
  }

  mobilTagSchliessen(): void {
    this.mobilTag.set(null);
  }

  mehrWochenenden(): void {
    this.wochenendAnzahl.update(n => n + WOCHENENDEN_UEBERSICHT);
    this.laden();
  }

  wochenendeAnzeigen(w: WochenendKachel): void {
    if (this.isMobile()) {
      this.mobilTag.set(w.samstag);
      return;
    }
    this.modus.set('wochenenden');
    this.startTag.set(w.samstag);
    this.laden();
  }

  alteAnsicht(): void {
    this.ansicht.setzen('legacy');
  }

  // -------------------------------------------------------------------------
  // Interaktion
  // -------------------------------------------------------------------------

  /** Klick auf eine Spur: in einer freien Lücke den Reservierungsdialog vorausgefüllt öffnen. */
  spurKlick(zeile: Zeile, platz: Platz, e: MouseEvent): void {
    if (!this.writableCalendarIds().includes(platz.id)) return;
    const rect = (e.currentTarget as HTMLElement).getBoundingClientRect();
    const t = new Date(+zeile.grenzen.start + ((e.clientX - rect.left) / rect.width) * (+zeile.grenzen.end - +zeile.grenzen.start));

    const belegung = this.termineMitIntervall(this.alleEvents(), platz.id).map(x => x.iv);
    const luecke = freieLueckeUm(belegung, t, zeile.grenzen);
    if (!luecke) return;

    const fruehestens = aufrunden(this.jetzt(), 15);
    let start = abrunden(t, 30);
    if (start < luecke.start) start = luecke.start;
    if (start < fruehestens) start = fruehestens;
    if (start >= luecke.end) return;
    const ende = new Date(Math.min(+luecke.end, +start + STANDARD_DAUER_MS));
    this.reservieren(platz.id, start, ende);
  }

  reservieren(calendarId?: string, start?: Date, ende?: Date): void {
    const writable = this.writableCalendarIds();
    if (writable.length === 0) return;
    const data: EventFormDialogData = {
      configs: this.configs().filter(c => c.isVisible),
      writableCalendarIds: writable,
      defaultStart: start,
      defaultEnd: ende,
      defaultCalendarId: calendarId,
      existingEvents: this.alleEvents(),
    };
    this.dialog.open(EventFormDialogComponent, { width: '480px', maxWidth: '95vw', data })
      .afterClosed().subscribe((r?: EventFormDialogResult) => {
        if (!r) return;
        this.calendarService.createEvent({
          calendarId: r.calendarId,
          title: r.title,
          start: serializeDate(r.start, r.isAllDay),
          end: serializeDate(r.end, r.isAllDay),
          isAllDay: r.isAllDay,
          description: r.description,
          location: r.location,
          url: r.url,
          fahrzeugId: r.fahrzeugId,
        }).subscribe({ next: () => this.laden(), error: () => this.fehler('Reservierung konnte nicht gespeichert werden.') });
      });
  }

  terminOeffnen(ev: CalendarEvent, e?: Event): void {
    e?.stopPropagation();
    const platz = this.plaetze().find(p => p.id === ev.calendarId);
    const bearbeitbar = this.writableCalendarIds().includes(ev.calendarId);
    const data: TerminDetailsData = {
      event: ev,
      platzName: platz?.name ?? ev.calendarName,
      farbe: platz?.farbe ?? ev.calendarColor,
      bearbeitbar,
    };
    this.dialog.open(TerminDetailsDialogComponent, { width: '420px', maxWidth: '95vw', data })
      .afterClosed().subscribe((aktion?: TerminDetailsAktion) => {
        if (aktion === 'bearbeiten') this.bearbeiten(ev);
        if (aktion === 'loeschen') this.loeschen(ev);
      });
  }

  private bearbeiten(ev: CalendarEvent): void {
    const data: EventFormDialogData = {
      event: ev,
      configs: this.configs().filter(c => c.isVisible),
      writableCalendarIds: this.writableCalendarIds(),
      existingEvents: this.alleEvents(),
    };
    this.dialog.open(EventFormDialogComponent, { width: '480px', maxWidth: '95vw', data })
      .afterClosed().subscribe((r?: EventFormDialogResult) => {
        if (!r) return;
        this.calendarService.updateEvent(ev.calendarId, ev.id, {
          title: r.title,
          start: serializeDate(r.start, r.isAllDay),
          end: serializeDate(r.end, r.isAllDay),
          isAllDay: r.isAllDay,
          description: r.description,
          location: r.location,
          url: r.url,
          fahrzeugId: r.fahrzeugId,
        }).subscribe({ next: () => this.laden(), error: () => this.fehler('Änderung konnte nicht gespeichert werden.') });
      });
  }

  private loeschen(ev: CalendarEvent): void {
    if (!confirm(`Termin „${ev.title}" wirklich löschen?`)) return;
    this.calendarService.deleteEvent(ev.calendarId, ev.id)
      .subscribe({ next: () => this.laden(), error: () => this.fehler('Termin konnte nicht gelöscht werden.') });
  }

  private fehler(text: string): void {
    this.snackBar.open(text, 'OK', { duration: 4000 });
  }

  // -------------------------------------------------------------------------
  // Helfer
  // -------------------------------------------------------------------------

  /** Timeline- und Übersichts-Termine ohne Duplikate (für Lücken- und Konfliktprüfung). */
  private alleEvents(): CalendarEvent[] {
    const map = new Map<string, CalendarEvent>();
    for (const ev of [...this.uebersichtEvents(), ...this.timelineEvents()]) map.set(`${ev.calendarId}|${ev.id}`, ev);
    return [...map.values()];
  }

  private termineMitIntervall(events: CalendarEvent[], calendarId: string): { ev: CalendarEvent; iv: Intervall }[] {
    return events
      .filter(ev => ev.calendarId === calendarId)
      .map(ev => ({ ev, iv: terminIntervall(ev) }))
      .filter((x): x is { ev: CalendarEvent; iv: Intervall } => x.iv !== null);
  }

  private zeileBauen(tag: Date): Zeile {
    const grenzen = tagesGrenzen(tag);
    const jetzt = this.jetzt();
    const events = this.alleEvents();
    const aktuell = this.aktuellerTag();

    let badge: Zeile['badge'] = null;
    if (gleicherTag(tag, jetzt)) badge = 'Heute';
    else if (gleicherTag(tag, aktuell)) badge = 'Läuft noch';

    const spuren = this.plaetze().map(platz => ({
      platz,
      segmente: this.termineMitIntervall(events, platz.id)
        .filter(t => ueberlappt(t.iv, grenzen))
        .sort((a, b) => +a.iv.start - +b.iv.start)
        .map(({ ev, iv }) => {
          const links = anteil(iv.start, grenzen) * 100;
          const rechts = anteil(iv.end, grenzen) * 100;
          const zeit = ev.isAllDay ? 'ganztägig' : `${uhrzeit(iv.start)}–${uhrzeit(iv.end)}`;
          return {
            event: ev,
            links,
            breite: Math.max(rechts - links, 0.6),
            zeit,
            label: `${platz.name}: ${terminText(ev)}, ${zeit}`,
            weiterLinks: iv.start < grenzen.start,
            weiterRechts: iv.end > grenzen.end,
          };
        }),
    }));

    const imTag = jetzt >= grenzen.start && jetzt < grenzen.end;
    return {
      tag,
      grenzen,
      wochentag: wochentag(tag),
      datum: tagMonat(tag),
      badge,
      wochenende: istWochenende(tag),
      spuren,
      jetzt: imTag ? anteil(jetzt, grenzen) * 100 : null,
      vergangen: anteil(jetzt, grenzen) * 100,
    };
  }

  achsenPosition(i: number): number {
    return (i / (ACHSE.length - 1)) * 100;
  }

  achsenLabel(h: number): string {
    return String(h).padStart(2, '0');
  }
}

function terminText(ev: CalendarEvent): string {
  return ev.creatorName ? `${ev.title} · ${ev.creatorName}` : ev.title;
}

/** Identisch zur alten Ansicht: ganztägig als lokale Zeit ohne Zeitzone, sonst ISO/UTC. */
function serializeDate(d: Date, isAllDay: boolean): string {
  if (!isAllDay) return d.toISOString();
  const p2 = (n: number) => String(n).padStart(2, '0');
  const p3 = (n: number) => String(n).padStart(3, '0');
  return `${d.getFullYear()}-${p2(d.getMonth() + 1)}-${p2(d.getDate())}T${p2(d.getHours())}:${p2(d.getMinutes())}:${p2(d.getSeconds())}.${p3(d.getMilliseconds())}`;
}
