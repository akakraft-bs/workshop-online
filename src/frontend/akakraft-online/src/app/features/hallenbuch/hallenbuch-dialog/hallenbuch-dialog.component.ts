import { Component, inject, OnInit } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatSelectModule } from '@angular/material/select';
import { MatDividerModule } from '@angular/material/divider';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { HallenbuchEintrag, GastschraubenArt, CreateHallenbuchEintragDto } from '../../../models/hallenbuch.model';
import { MangelKategorie } from '../../../models/mangel.model';

export interface HallenbuchDialogData {
  eintrag?: HallenbuchEintrag;
}

export interface HallenbuchDialogResult {
  hallenbuch: CreateHallenbuchEintragDto;
  mangel?: { title: string; description: string; kategorie: MangelKategorie };
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

@Component({
  selector: 'app-hallenbuch-dialog',
  imports: [
    ReactiveFormsModule,
    MatDialogModule, MatFormFieldModule, MatInputModule,
    MatButtonModule, MatIconModule, MatSlideToggleModule,
    MatSelectModule, MatDividerModule,
    MatDatepickerModule,
  ],
  templateUrl: './hallenbuch-dialog.component.html',
  styleUrl: './hallenbuch-dialog.component.scss',
})
export class HallenbuchDialogComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  readonly dialogRef = inject(MatDialogRef<HallenbuchDialogComponent>);
  readonly data: HallenbuchDialogData | null = inject(MAT_DIALOG_DATA, { optional: true });

  readonly isEdit = !!this.data?.eintrag;
  readonly eintrag = this.data?.eintrag;

  readonly kategorien: MangelKategorie[] = ['Halle', 'Werkzeug', 'Sonstiges'];

  readonly form = this.fb.group({
    startDate:            [null as Date | null, Validators.required],
    startTime:            ['', Validators.required],
    endDate:              [null as Date | null, Validators.required],
    endTime:              ['', Validators.required],
    description:          ['', [Validators.required, Validators.maxLength(256)]],
    hatGastgeschraubt:    [false],
    gastschraubenArt:     [null as GastschraubenArt | null],
    gastschraubenBezahlt: [false],
    hatFamiliegeschraubt: [false],
    mangelMelden:         [false],
    mangelTitel:          ['', Validators.maxLength(200)],
    mangelKategorie:      ['' as MangelKategorie | ''],
    mangelBeschreibung:   ['', Validators.maxLength(2000)],
  });

  get hatGast(): boolean {
    return !!this.form.get('hatGastgeschraubt')!.value;
  }

  get hatFamilie(): boolean {
    return !!this.form.get('hatFamiliegeschraubt')!.value;
  }

  get mangelMelden(): boolean {
    return !!this.form.get('mangelMelden')!.value;
  }

  get descriptionLength(): number {
    return this.form.get('description')!.value?.length ?? 0;
  }

  get mangelTitelLength(): number {
    return this.form.get('mangelTitel')!.value?.length ?? 0;
  }

  get mangelBeschreibungLength(): number {
    return this.form.get('mangelBeschreibung')!.value?.length ?? 0;
  }

  ngOnInit(): void {
    if (this.eintrag) {
      const start = new Date(this.eintrag.start);
      const end   = new Date(this.eintrag.end);
      this.form.patchValue({
        startDate: dateOnly(start),
        startTime: toTimeString(start),
        endDate:   dateOnly(end),
        endTime:   toTimeString(end),
        description:          this.eintrag.description,
        hatGastgeschraubt:    this.eintrag.hatGastgeschraubt,
        gastschraubenArt:     this.eintrag.gastschraubenArt,
        gastschraubenBezahlt: this.eintrag.gastschraubenBezahlt ?? false,
        hatFamiliegeschraubt: this.eintrag.hatFamiliegeschraubt,
      });
    } else {
      const now = new Date();
      now.setMinutes(0, 0, 0);
      this.form.patchValue({
        startDate: dateOnly(now),
        startTime: toTimeString(now),
        endDate:   dateOnly(now),
        endTime:   toTimeString(new Date(now.getTime() + 60 * 60 * 1000)),
      });
    }

    // Auto-adjust end if start moves past it
    this.form.get('startDate')!.valueChanges.subscribe(() => this.adjustEndIfNeeded());
    this.form.get('startTime')!.valueChanges.subscribe(() => this.adjustEndIfNeeded());

    // Gast und Familie schließen sich gegenseitig aus
    this.form.get('hatGastgeschraubt')!.valueChanges.subscribe(v => {
      if (v) this.form.get('hatFamiliegeschraubt')!.setValue(false, { emitEvent: false });
    });
    this.form.get('hatFamiliegeschraubt')!.valueChanges.subscribe(v => {
      if (v) this.form.get('hatGastgeschraubt')!.setValue(false, { emitEvent: false });
    });
  }

  private adjustEndIfNeeded(): void {
    const startDate = this.form.get('startDate')!.value;
    const startTime = this.form.get('startTime')!.value || '00:00';
    const endDate   = this.form.get('endDate')!.value;
    const endTime   = this.form.get('endTime')!.value   || '00:00';
    if (!startDate || !endDate) return;

    const start = combineDateTime(startDate, startTime);
    const end   = combineDateTime(endDate, endTime);
    if (end <= start) {
      const newEnd = new Date(start.getTime() + 60 * 60 * 1000);
      this.form.patchValue(
        { endDate: dateOnly(newEnd), endTime: toTimeString(newEnd) },
        { emitEvent: false }
      );
    }
  }

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const v = this.form.getRawValue();
    const start = combineDateTime(v.startDate!, v.startTime || '00:00');
    const end   = combineDateTime(v.endDate!,   v.endTime   || '00:00');

    if (end <= start) {
      this.form.get('endTime')!.setErrors({ afterStart: true });
      return;
    }

    if (v.mangelMelden) {
      let mangelValid = true;
      if (!v.mangelTitel?.trim()) {
        this.form.get('mangelTitel')!.setErrors({ required: true });
        this.form.get('mangelTitel')!.markAsTouched();
        mangelValid = false;
      }
      if (!v.mangelKategorie) {
        this.form.get('mangelKategorie')!.setErrors({ required: true });
        this.form.get('mangelKategorie')!.markAsTouched();
        mangelValid = false;
      }
      if (!v.mangelBeschreibung?.trim()) {
        this.form.get('mangelBeschreibung')!.setErrors({ required: true });
        this.form.get('mangelBeschreibung')!.markAsTouched();
        mangelValid = false;
      }
      if (!mangelValid) return;
    }

    const hallenbuch: CreateHallenbuchEintragDto = {
      start: start.toISOString(),
      end:   end.toISOString(),
      description: v.description!,
      hatGastgeschraubt: !!v.hatGastgeschraubt,
      gastschraubenArt:  v.hatGastgeschraubt ? v.gastschraubenArt : null,
      gastschraubenBezahlt: v.hatGastgeschraubt ? !!v.gastschraubenBezahlt : null,
      hatFamiliegeschraubt: !!v.hatFamiliegeschraubt,
    };

    const result: HallenbuchDialogResult = { hallenbuch };
    if (v.mangelMelden && v.mangelTitel && v.mangelKategorie && v.mangelBeschreibung) {
      result.mangel = {
        title: v.mangelTitel,
        kategorie: v.mangelKategorie as MangelKategorie,
        description: v.mangelBeschreibung,
      };
    }

    this.dialogRef.close(result);
  }

  cancel(): void {
    this.dialogRef.close();
  }
}
