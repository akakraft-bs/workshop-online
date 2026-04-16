import { Component, inject, OnInit, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { CalendarConfig, CalendarEvent } from '../../../models/calendar.model';

export interface EventFormDialogData {
  event?: CalendarEvent;
  defaultStart?: Date;
  /** Wird beim Speichern automatisch vor den Titel gestellt, z. B. "Max - " */
  titlePrefix?: string;
  configs: CalendarConfig[];
  writableCalendarIds: string[];
}

export interface EventFormDialogResult {
  calendarId: string;
  title: string;
  start: Date;
  end: Date;
  isAllDay: boolean;
  description?: string;
  location?: string;
}

@Component({
  selector: 'app-event-form-dialog',
  imports: [
    ReactiveFormsModule,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatSelectModule,
    MatCheckboxModule,
  ],
  template: `
    <h2 mat-dialog-title>{{ data.event ? 'Termin bearbeiten' : 'Neuer Termin' }}</h2>
    <mat-dialog-content>
      <form [formGroup]="form" class="event-form">
        <mat-form-field appearance="outline">
          <mat-label>Kalender</mat-label>
          <mat-select formControlName="calendarId">
            @for (cfg of writableConfigs; track cfg.googleCalendarId) {
              <mat-option [value]="cfg.googleCalendarId">
                <span class="cal-dot" [style.background]="cfg.color"></span>
                {{ cfg.name }}
              </mat-option>
            }
          </mat-select>
        </mat-form-field>

        <mat-form-field appearance="outline">
          <mat-label>Titel</mat-label>
          <input matInput formControlName="title" placeholder="Titel eingeben">
        </mat-form-field>

        @if (data.titlePrefix && !data.event) {
          <p class="title-preview">
            <span class="preview-label">Gespeicherter Titel:</span>
            <span class="preview-prefix">{{ data.titlePrefix }}</span><span class="preview-input">{{ titlePreview() || '…' }}</span>
          </p>
        }

        <mat-checkbox formControlName="isAllDay">Ganztägig</mat-checkbox>

        @if (!form.get('isAllDay')!.value) {
          <mat-form-field appearance="outline">
            <mat-label>Beginn</mat-label>
            <input matInput type="datetime-local" formControlName="start">
          </mat-form-field>

          <mat-form-field appearance="outline">
            <mat-label>Ende</mat-label>
            <input matInput type="datetime-local" formControlName="end">
          </mat-form-field>
        } @else {
          <mat-form-field appearance="outline">
            <mat-label>Datum (Beginn)</mat-label>
            <input matInput type="date" formControlName="startDate">
          </mat-form-field>

          <mat-form-field appearance="outline">
            <mat-label>Datum (Ende)</mat-label>
            <input matInput type="date" formControlName="endDate">
          </mat-form-field>
        }

        <mat-form-field appearance="outline">
          <mat-label>Beschreibung (optional)</mat-label>
          <textarea matInput formControlName="description" rows="3"></textarea>
        </mat-form-field>

        <mat-form-field appearance="outline">
          <mat-label>Ort (optional)</mat-label>
          <input matInput formControlName="location">
        </mat-form-field>
      </form>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button mat-dialog-close>Abbrechen</button>
      <button mat-flat-button color="primary" (click)="submit()" [disabled]="form.invalid">
        {{ data.event ? 'Speichern' : 'Erstellen' }}
      </button>
    </mat-dialog-actions>
  `,
  styles: [`
    .event-form {
      display: flex;
      flex-direction: column;
      gap: 4px;
      min-width: 340px;
    }
    mat-form-field { width: 100%; }
    mat-checkbox { margin-bottom: 8px; }
    .cal-dot {
      display: inline-block;
      width: 10px;
      height: 10px;
      border-radius: 50%;
      margin-right: 6px;
    }
    .title-preview {
      font-size: 13px;
      color: var(--mat-sys-on-surface-variant, #666);
      margin: -2px 0 8px;
      display: flex;
      align-items: baseline;
      gap: 6px;
      flex-wrap: wrap;
    }
    .preview-label {
      white-space: nowrap;
    }
    .preview-prefix {
      color: var(--mat-sys-primary, #1976d2);
      font-weight: 500;
    }
    .preview-input {
      font-style: italic;
    }
  `],
})
export class EventFormDialogComponent implements OnInit {
  readonly data = inject<EventFormDialogData>(MAT_DIALOG_DATA);
  private readonly dialogRef = inject(MatDialogRef<EventFormDialogComponent>);
  private readonly fb = inject(FormBuilder);

  readonly writableConfigs = this.data.configs.filter(c =>
    this.data.writableCalendarIds.includes(c.googleCalendarId)
  );

  readonly form = this.fb.group({
    calendarId: [this.writableConfigs[0]?.googleCalendarId ?? '', Validators.required],
    title: ['', Validators.required],
    isAllDay: [false],
    start: [''],
    end: [''],
    startDate: [''],
    endDate: [''],
    description: [''],
    location: [''],
  });

  /** Reaktive Vorschau des Titels im Eingabefeld (für das Preview-Label) */
  titlePreview = signal('');

  ngOnInit(): void {
    const ev = this.data.event;
    if (ev) {
      this.form.patchValue({ calendarId: ev.calendarId });
      this.form.get('calendarId')!.disable();
    }

    if (ev) {
      const start = ev.start ? new Date(ev.start) : new Date();
      const end = ev.end ? new Date(ev.end) : new Date(start.getTime() + 60 * 60 * 1000);
      this.form.patchValue({
        title: ev.title,
        isAllDay: ev.isAllDay,
        start: toLocalDateTimeInput(start),
        end: toLocalDateTimeInput(end),
        startDate: toLocalDateInput(start),
        endDate: toLocalDateInput(end),
        description: ev.description ?? '',
        location: ev.location ?? '',
      });
    } else {
      const start = this.data.defaultStart ?? roundToNextHour(new Date());
      const end = new Date(start.getTime() + 60 * 60 * 1000);
      this.form.patchValue({
        start: toLocalDateTimeInput(start),
        end: toLocalDateTimeInput(end),
        startDate: toLocalDateInput(start),
        endDate: toLocalDateInput(start),
      });
    }

    // Preview live aktualisieren
    this.form.get('title')!.valueChanges.subscribe(v => this.titlePreview.set(v ?? ''));
  }

  submit(): void {
    if (this.form.invalid) return;

    const v = this.form.getRawValue();
    const isAllDay = !!v.isAllDay;

    let start: Date;
    let end: Date;

    if (isAllDay) {
      start = new Date(v.startDate + 'T00:00:00');
      end = new Date(v.endDate + 'T00:00:00');
    } else {
      start = new Date(v.start!);
      end = new Date(v.end!);
    }

    // Prefix nur beim Erstellen anhängen (nicht beim Bearbeiten)
    const prefix = !this.data.event && this.data.titlePrefix ? this.data.titlePrefix : '';
    const title = prefix + v.title!;

    const result: EventFormDialogResult = {
      calendarId: v.calendarId!,
      title,
      start,
      end,
      isAllDay,
      description: v.description || undefined,
      location: v.location || undefined,
    };

    this.dialogRef.close(result);
  }
}

function roundToNextHour(d: Date): Date {
  const result = new Date(d);
  result.setMinutes(0, 0, 0);
  result.setHours(result.getHours() + 1);
  return result;
}

function toLocalDateTimeInput(d: Date): string {
  const pad = (n: number) => n.toString().padStart(2, '0');
  return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}T${pad(d.getHours())}:${pad(d.getMinutes())}`;
}

function toLocalDateInput(d: Date): string {
  const pad = (n: number) => n.toString().padStart(2, '0');
  return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}`;
}
