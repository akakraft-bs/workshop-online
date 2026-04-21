import { Component, inject, OnInit } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { CalendarConfig, CalendarEvent } from '../../../models/calendar.model';

export interface EventFormDialogData {
  event?: CalendarEvent;
  defaultStart?: Date;
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
    MatDatepickerModule,
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

        <mat-checkbox formControlName="isAllDay">Ganztägig</mat-checkbox>

        @if (!form.get('isAllDay')!.value) {
          <div class="date-time-row">
            <mat-form-field appearance="outline" class="date-field">
              <mat-label>Beginn</mat-label>
              <input matInput [matDatepicker]="startPicker" formControlName="startDate">
              <mat-datepicker-toggle matIconSuffix [for]="startPicker"></mat-datepicker-toggle>
              <mat-datepicker #startPicker></mat-datepicker>
            </mat-form-field>
            <mat-form-field appearance="outline" class="time-field">
              <mat-label>Uhrzeit</mat-label>
              <input matInput type="time" formControlName="startTime">
            </mat-form-field>
          </div>

          <div class="date-time-row">
            <mat-form-field appearance="outline" class="date-field">
              <mat-label>Ende</mat-label>
              <input matInput [matDatepicker]="endPicker" formControlName="endDate">
              <mat-datepicker-toggle matIconSuffix [for]="endPicker"></mat-datepicker-toggle>
              <mat-datepicker #endPicker></mat-datepicker>
            </mat-form-field>
            <mat-form-field appearance="outline" class="time-field">
              <mat-label>Uhrzeit</mat-label>
              <input matInput type="time" formControlName="endTime">
            </mat-form-field>
          </div>
        } @else {
          <mat-form-field appearance="outline">
            <mat-label>Datum (Beginn)</mat-label>
            <input matInput [matDatepicker]="allDayStartPicker" formControlName="startDate">
            <mat-datepicker-toggle matIconSuffix [for]="allDayStartPicker"></mat-datepicker-toggle>
            <mat-datepicker #allDayStartPicker></mat-datepicker>
          </mat-form-field>

          <mat-form-field appearance="outline">
            <mat-label>Datum (Ende)</mat-label>
            <input matInput [matDatepicker]="allDayEndPicker" formControlName="endDate">
            <mat-datepicker-toggle matIconSuffix [for]="allDayEndPicker"></mat-datepicker-toggle>
            <mat-datepicker #allDayEndPicker></mat-datepicker>
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
    .date-time-row {
      display: flex;
      gap: 8px;
      .date-field { flex: 1 1 60%; }
      .time-field { flex: 1 1 40%; }
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
    startDate: [null as Date | null, Validators.required],
    startTime: [''],
    endDate: [null as Date | null, Validators.required],
    endTime: [''],
    description: [''],
    location: [''],
  });

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
        startDate: dateOnly(start),
        startTime: toTimeString(start),
        endDate: dateOnly(end),
        endTime: toTimeString(end),
        description: ev.description ?? '',
        location: ev.location ?? '',
      });
    } else {
      const start = this.data.defaultStart ?? roundToNextHour(new Date());
      const end = new Date(start.getTime() + 60 * 60 * 1000);
      this.form.patchValue({
        startDate: dateOnly(start),
        startTime: toTimeString(start),
        endDate: dateOnly(end),
        endTime: toTimeString(end),
      });
    }

    // Auto-adjust end when start changes
    this.form.get('startDate')!.valueChanges.subscribe(() => this.adjustEndIfNeeded());
    this.form.get('startTime')!.valueChanges.subscribe(() => this.adjustEndIfNeeded());
    this.form.get('isAllDay')!.valueChanges.subscribe(() => this.adjustEndIfNeeded());
  }

  private adjustEndIfNeeded(): void {
    const isAllDay = !!this.form.get('isAllDay')!.value;
    const startDate = this.form.get('startDate')!.value as Date | null;
    if (!startDate) return;

    if (isAllDay) {
      const endDate = this.form.get('endDate')!.value as Date | null;
      if (!endDate || endDate < startDate) {
        this.form.get('endDate')!.setValue(new Date(startDate), { emitEvent: false });
      }
    } else {
      const startTime = this.form.get('startTime')!.value || '00:00';
      const endDate = this.form.get('endDate')!.value as Date | null;
      const endTime = this.form.get('endTime')!.value || '00:00';
      const start = combineDateTime(startDate, startTime);
      const end = endDate ? combineDateTime(endDate, endTime) : null;
      if (!end || end <= start) {
        const newEnd = new Date(start.getTime() + 60 * 60 * 1000);
        this.form.patchValue({
          endDate: dateOnly(newEnd),
          endTime: toTimeString(newEnd),
        }, { emitEvent: false });
      }
    }
  }

  submit(): void {
    if (this.form.invalid) return;

    const v = this.form.getRawValue();
    const isAllDay = !!v.isAllDay;

    let start: Date;
    let end: Date;

    if (isAllDay) {
      start = new Date(v.startDate!);
      start.setHours(0, 0, 0, 0);
      end = new Date(v.endDate!);
      end.setHours(0, 0, 0, 0);
    } else {
      start = combineDateTime(v.startDate!, v.startTime || '00:00');
      end = combineDateTime(v.endDate!, v.endTime || '00:00');
    }

    const result: EventFormDialogResult = {
      calendarId: v.calendarId!,
      title: v.title!,
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

function dateOnly(d: Date): Date {
  return new Date(d.getFullYear(), d.getMonth(), d.getDate());
}

function toTimeString(d: Date): string {
  const pad = (n: number) => n.toString().padStart(2, '0');
  return `${pad(d.getHours())}:${pad(d.getMinutes())}`;
}

function combineDateTime(date: Date, timeStr: string): Date {
  const [hours, minutes] = timeStr.split(':').map(Number);
  const result = new Date(date);
  result.setHours(hours, minutes, 0, 0);
  return result;
}
