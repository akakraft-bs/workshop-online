import { Component, inject, OnInit, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MAT_DIALOG_DATA, MatDialog, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { CalendarConfig, CalendarEvent } from '../../../models/calendar.model';
import { ConflictConfirmDialogComponent } from './conflict-confirm-dialog.component';

export interface EventFormDialogData {
  event?: CalendarEvent;
  defaultStart?: Date;
  configs: CalendarConfig[];
  writableCalendarIds: string[];
  /** Existing events used to detect time conflicts (optional). */
  existingEvents?: CalendarEvent[];
  /** Show the optional URL field (e.g. for Veranstaltungen, not Hallenbelegung). */
  showUrlField?: boolean;
}

export interface EventFormDialogResult {
  calendarId: string;
  title: string;
  start: Date;
  end: Date;
  isAllDay: boolean;
  description?: string;
  location?: string;
  url?: string;
}

@Component({
  selector: 'app-event-form-dialog',
  imports: [
    ReactiveFormsModule,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatIconModule,
    MatSelectModule,
    MatCheckboxModule,
    MatDatepickerModule,
  ],
  templateUrl: './event-form-dialog.component.html',
  styleUrl: './event-form-dialog.component.scss',
})
export class EventFormDialogComponent implements OnInit {
  readonly data = inject<EventFormDialogData>(MAT_DIALOG_DATA);
  private readonly dialogRef = inject(MatDialogRef<EventFormDialogComponent>);
  private readonly dialog = inject(MatDialog);
  private readonly fb = inject(FormBuilder);

  readonly writableConfigs = this.data.configs.filter(c =>
    this.data.writableCalendarIds.includes(c.googleCalendarId)
  );

  readonly conflicts = signal<CalendarEvent[]>([]);

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
    url: [''],
  });

  ngOnInit(): void {
    const ev = this.data.event;
    if (ev) {
      this.form.patchValue({ calendarId: ev.calendarId });
      this.form.get('calendarId')!.disable();

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
        url: ev.url ?? '',
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

    this.form.get('startDate')!.valueChanges.subscribe(() => this.adjustEndIfNeeded());
    this.form.get('startTime')!.valueChanges.subscribe(() => this.adjustEndIfNeeded());
    this.form.get('isAllDay')!.valueChanges.subscribe(() => this.adjustEndIfNeeded());

    this.form.valueChanges.subscribe(() => this.updateConflicts());
    this.updateConflicts();
  }

  private updateConflicts(): void {
    const existing = this.data.existingEvents;
    if (!existing || existing.length === 0) { this.conflicts.set([]); return; }

    const v = this.form.getRawValue();
    if (!v.startDate || !v.endDate) { this.conflicts.set([]); return; }

    const isAllDay = !!v.isAllDay;
    let newStart: Date, newEnd: Date;

    if (isAllDay) {
      newStart = new Date(v.startDate); newStart.setHours(0, 0, 0, 0);
      newEnd = new Date(v.endDate); newEnd.setHours(23, 59, 59, 999);
    } else {
      newStart = combineDateTime(v.startDate, v.startTime || '00:00');
      newEnd = combineDateTime(v.endDate, v.endTime || '00:00');
    }

    if (newStart >= newEnd) { this.conflicts.set([]); return; }

    const calendarId = v.calendarId;
    const editId = this.data.event?.id;

    this.conflicts.set(existing.filter(ev => {
      if (ev.calendarId !== calendarId) return false;
      if (editId && ev.id === editId) return false;
      if (!ev.start) return false;
      const evStart = new Date(ev.start);
      const evEnd = ev.end ? new Date(ev.end) : new Date(evStart.getTime() + 60_000);
      return newStart < evEnd && evStart < newEnd;
    }));
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

    if (this.conflicts().length > 0) {
      this.dialog.open(ConflictConfirmDialogComponent, {
        width: '380px',
        data: this.conflicts(),
      }).afterClosed().subscribe((confirmed: boolean) => {
        if (confirmed) this.doClose();
      });
      return;
    }

    this.doClose();
  }

  private doClose(): void {
    const v = this.form.getRawValue();
    const isAllDay = !!v.isAllDay;

    let start: Date;
    let end: Date;

    if (isAllDay) {
      start = new Date(v.startDate!); start.setHours(0, 0, 0, 0);
      end = new Date(v.endDate!); end.setHours(23, 59, 59, 999);
    } else {
      start = combineDateTime(v.startDate!, v.startTime || '00:00');
      end = combineDateTime(v.endDate!, v.endTime || '00:00');
    }

    this.dialogRef.close({
      calendarId: v.calendarId!,
      title: v.title!,
      start,
      end,
      isAllDay,
      description: v.description || undefined,
      location: v.location || undefined,
      url: v.url || undefined,
    } satisfies EventFormDialogResult);
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
