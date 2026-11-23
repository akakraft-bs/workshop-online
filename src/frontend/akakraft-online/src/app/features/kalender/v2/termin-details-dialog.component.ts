import { Component, inject } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialogModule } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { CalendarEvent } from '../../../models/calendar.model';
import { gleicherTag, tagRelativ, terminIntervall, uhrzeit } from './werkstatt-zeit';

export interface TerminDetailsData {
  event: CalendarEvent;
  platzName: string;
  farbe: string;
  bearbeitbar: boolean;
}

export type TerminDetailsAktion = 'bearbeiten' | 'loeschen';

@Component({
  selector: 'app-termin-details-dialog',
  imports: [MatDialogModule, MatButtonModule, MatIconModule],
  template: `
    <h2 mat-dialog-title class="td-title">{{ data.event.title }}</h2>
    <mat-dialog-content>
      <dl class="td-list">
        <div class="td-row">
          <dt><mat-icon aria-hidden="true">place</mat-icon><span class="cdk-visually-hidden">Platz</span></dt>
          <dd><span class="td-dot" [style.background]="data.farbe"></span>{{ data.platzName }}</dd>
        </div>
        <div class="td-row">
          <dt><mat-icon aria-hidden="true">schedule</mat-icon><span class="cdk-visually-hidden">Zeit</span></dt>
          <dd>{{ zeit }}</dd>
        </div>
        @if (data.event.creatorName) {
          <div class="td-row">
            <dt><mat-icon aria-hidden="true">person</mat-icon><span class="cdk-visually-hidden">Person</span></dt>
            <dd>{{ data.event.creatorName }}</dd>
          </div>
        }
        @if (data.event.fahrzeugLabel) {
          <div class="td-row">
            <dt><mat-icon aria-hidden="true">directions_car</mat-icon><span class="cdk-visually-hidden">Fahrzeug</span></dt>
            <dd>{{ data.event.fahrzeugLabel }}</dd>
          </div>
        }
        @if (data.event.location) {
          <div class="td-row">
            <dt><mat-icon aria-hidden="true">location_on</mat-icon><span class="cdk-visually-hidden">Ort</span></dt>
            <dd>{{ data.event.location }}</dd>
          </div>
        }
        @if (data.event.description) {
          <div class="td-row">
            <dt><mat-icon aria-hidden="true">notes</mat-icon><span class="cdk-visually-hidden">Beschreibung</span></dt>
            <dd class="td-desc">{{ data.event.description }}</dd>
          </div>
        }
      </dl>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      @if (data.bearbeitbar) {
        <button mat-button class="td-delete" [mat-dialog-close]="'loeschen'">Löschen</button>
        <button mat-button [mat-dialog-close]="'bearbeiten'">Bearbeiten</button>
      }
      <button mat-flat-button mat-dialog-close>Schließen</button>
    </mat-dialog-actions>
  `,
  styles: [`
    .td-title { overflow-wrap: anywhere; }
    .td-list { margin: 0; display: flex; flex-direction: column; gap: 10px; }
    .td-row { display: flex; align-items: flex-start; gap: 12px; }
    dt { display: flex; color: var(--mat-sys-on-surface-variant); }
    dt mat-icon { font-size: 20px; width: 20px; height: 20px; }
    dd { margin: 0; display: flex; align-items: center; gap: 8px; overflow-wrap: anywhere; }
    .td-desc { white-space: pre-line; }
    .td-dot { width: 10px; height: 10px; border-radius: 50%; flex-shrink: 0; }
    .td-delete { color: var(--mat-sys-error); }
  `],
})
export class TerminDetailsDialogComponent {
  readonly data = inject<TerminDetailsData>(MAT_DIALOG_DATA);
  readonly zeit = formatZeit(this.data.event);
}

function formatZeit(ev: CalendarEvent): string {
  const iv = terminIntervall(ev);
  if (!iv) return '–';
  const jetzt = new Date();
  if (ev.isAllDay) return `${tagRelativ(new Date(ev.start!), jetzt)} · ganztägig`;
  if (gleicherTag(iv.start, iv.end)) return `${tagRelativ(iv.start, jetzt)}, ${uhrzeit(iv.start)}–${uhrzeit(iv.end)}`;
  return `${tagRelativ(iv.start, jetzt)} ${uhrzeit(iv.start)} – ${tagRelativ(iv.end, jetzt)} ${uhrzeit(iv.end)}`;
}
