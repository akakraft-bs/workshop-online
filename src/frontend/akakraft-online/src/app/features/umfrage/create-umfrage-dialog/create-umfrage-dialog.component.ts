import { Component, inject, OnInit } from '@angular/core';
import { FormArray, FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule, MatIconButton } from '@angular/material/button';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatNativeDateModule } from '@angular/material/core';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatTooltipModule } from '@angular/material/tooltip';
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
    MatSlideToggleModule,
    MatDatepickerModule,
    MatNativeDateModule,
    MatTooltipModule,
  ],
  templateUrl: './create-umfrage-dialog.component.html',
  styleUrl: './create-umfrage-dialog.component.scss',
})
export class CreateUmfrageDialogComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  readonly dialogRef = inject(MatDialogRef<CreateUmfrageDialogComponent>);
  readonly data: UmfrageDialogData | null = inject(MAT_DIALOG_DATA, { optional: true });

  readonly isEdit = !!this.data?.umfrage;
  readonly today = new Date();

  readonly form = this.fb.group({
    question: ['', [Validators.required, Validators.maxLength(500)]],
    isMultipleChoice: [false],
    resultsVisible: [true],
    revealAfterClose: [false],
    hasDeadline: [false],
    deadlineDate: [null as Date | null],
    deadlineTime: ['23:59'],
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
    const u = this.data?.umfrage;
    if (u) {
      this.form.patchValue({
        question: u.question,
        isMultipleChoice: u.isMultipleChoice,
        resultsVisible: u.resultsVisible,
        revealAfterClose: u.revealAfterClose,
        hasDeadline: !!u.deadline,
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

    if (this.isEdit) {
      const result: UpdateUmfrageDto = {
        question: v.question!,
        options: rawOptions.map(o => ({ id: o.id ?? null, text: o.text })),
        isMultipleChoice: v.isMultipleChoice!,
        resultsVisible: v.resultsVisible!,
        revealAfterClose: v.revealAfterClose!,
        deadline,
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
      };
      this.dialogRef.close(result);
    }
  }
}
