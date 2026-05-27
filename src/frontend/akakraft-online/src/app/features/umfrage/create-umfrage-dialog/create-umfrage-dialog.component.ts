import { Component, inject, OnInit, signal } from '@angular/core';
import { FormArray, FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule, MatIconButton } from '@angular/material/button';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatTooltipModule } from '@angular/material/tooltip';
import { CalendarService } from '../../../core/calendar/calendar.service';
import { CalendarEvent } from '../../../models/calendar.model';
import { Umfrage, CreateUmfrageDto, UpdateUmfrageDto } from '../../../models/umfrage.model';

export interface UmfrageDialogData {
  umfrage?: Umfrage;
}

@Component({
  selector: 'app-create-umfrage-dialog',
  imports: [
    ReactiveFormsModule,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatIconModule,
    MatCheckboxModule,
    MatSelectModule,
    MatSlideToggleModule,
    MatDatepickerModule,
    MatTooltipModule,
  ],
  templateUrl: './create-umfrage-dialog.component.html',
  styleUrl: './create-umfrage-dialog.component.scss',
})
export class CreateUmfrageDialogComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly calendarService = inject(CalendarService);
  readonly dialogRef = inject(MatDialogRef<CreateUmfrageDialogComponent>);
  readonly data: UmfrageDialogData | null = inject(MAT_DIALOG_DATA, { optional: true });

  readonly isEdit = !!this.data?.umfrage;
  readonly today = new Date();

  readonly veranstaltungen = signal<CalendarEvent[]>([]);
  readonly eventsLoading = signal(false);

  readonly form = this.fb.group({
    question: ['', [Validators.required, Validators.maxLength(500)]],
    isMultipleChoice: [false],
    resultsVisible: [true],
    revealAfterClose: [false],
    hasDeadline: [false],
    deadlineDate: [null as Date | null],
    deadlineTime: ['23:59'],
    linkedEventId: [null as string | null],
    options: this.fb.array<FormGroup>([]),
  });

  get optionControls() {
    return (this.form.get('options') as FormArray).controls as FormGroup[];
  }

  get resultsVisibleValue() {
    return this.form.get('resultsVisible')!.value;
  }

  get hasDeadlineValue() {
    return this.form.get('hasDeadline')!.value;
  }

  ngOnInit(): void {
    this.loadVeranstaltungen();

    const u = this.data?.umfrage;
    if (u) {
      this.form.patchValue({
        question: u.question,
        isMultipleChoice: u.isMultipleChoice,
        resultsVisible: u.resultsVisible,
        revealAfterClose: u.revealAfterClose,
        hasDeadline: !!u.deadline,
        linkedEventId: u.linkedEventId ?? null,
      });

      if (u.deadline) {
        const d = new Date(u.deadline);
        this.form.patchValue({
          deadlineDate: d,
          deadlineTime: `${String(d.getHours()).padStart(2, '0')}:${String(d.getMinutes()).padStart(2, '0')}`,
        });
      }

      for (const opt of u.options) {
        this.optionsArray.push(this.fb.group({
          id: [opt.id],
          text: [opt.text, [Validators.required, Validators.maxLength(500)]],
        }));
      }
    } else {
      // Default: two empty options
      this.addOption();
      this.addOption();
    }
  }

  private loadVeranstaltungen(): void {
    this.eventsLoading.set(true);
    const now = new Date();
    const from = new Date(now.getFullYear(), now.getMonth() - 1, 1);
    const to = new Date(now.getFullYear() + 2, now.getMonth(), 1);
    this.calendarService.getEvents(from, to, 'Veranstaltungen').subscribe({
      next: events => {
        const sorted = events.slice().sort((a, b) =>
          new Date(a.start ?? 0).getTime() - new Date(b.start ?? 0).getTime()
        );
        this.veranstaltungen.set(sorted);
        this.eventsLoading.set(false);
      },
      error: () => this.eventsLoading.set(false),
    });
  }

  formatEventLabel(event: CalendarEvent): string {
    if (!event.start) return event.title;
    const d = new Date(event.start);
    const pad = (n: number) => String(n).padStart(2, '0');
    return `${pad(d.getDate())}.${pad(d.getMonth() + 1)}. – ${event.title}`;
  }

  private get optionsArray(): FormArray {
    return this.form.get('options') as FormArray;
  }

  addOption(): void {
    this.optionsArray.push(this.fb.group({
      id: [null as string | null],
      text: ['', [Validators.required, Validators.maxLength(500)]],
    }));
  }

  removeOption(index: number): void {
    if (this.optionsArray.length > 2) {
      this.optionsArray.removeAt(index);
    }
  }

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const v = this.form.getRawValue();

    let deadline: string | null = null;
    if (v.hasDeadline && v.deadlineDate) {
      const d = new Date(v.deadlineDate);
      const [hours, minutes] = (v.deadlineTime || '23:59').split(':').map(Number);
      d.setHours(hours, minutes, 0, 0);
      deadline = d.toISOString();
    }

    // getRawValue() for a FormArray<FormGroup> yields { [key: string]: any }[]
    const rawOptions = v.options as Array<{ id: string | null; text: string }>;

    const linkedEvent = this.veranstaltungen().find(e => e.id === v.linkedEventId) ?? null;
    const linkedEventId = linkedEvent?.id ?? null;
    const linkedCalendarId = linkedEvent?.calendarId ?? null;
    const linkedEventTitle = linkedEvent?.title ?? null;
    const linkedEventStart = linkedEvent?.start ?? null;

    if (this.isEdit) {
      const result: UpdateUmfrageDto = {
        question: v.question!,
        options: rawOptions.map(o => ({ id: o.id ?? null, text: o.text })),
        isMultipleChoice: v.isMultipleChoice!,
        resultsVisible: v.resultsVisible!,
        revealAfterClose: v.revealAfterClose!,
        deadline,
        linkedEventId,
        linkedCalendarId,
        linkedEventTitle,
        linkedEventStart,
      };
      this.dialogRef.close(result);
    } else {
      const result: CreateUmfrageDto = {
        question: v.question!,
        options: rawOptions.map(o => o.text),
        isMultipleChoice: v.isMultipleChoice!,
        resultsVisible: v.resultsVisible!,
        revealAfterClose: v.revealAfterClose!,
        deadline,
        linkedEventId,
        linkedCalendarId,
        linkedEventTitle,
        linkedEventStart,
      };
      this.dialogRef.close(result);
    }
  }
}
