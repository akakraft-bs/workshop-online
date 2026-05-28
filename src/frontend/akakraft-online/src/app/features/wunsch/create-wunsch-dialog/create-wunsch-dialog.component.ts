import { Component, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators, AbstractControl, ValidationErrors } from '@angular/forms';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { Wunsch } from '../../../models/wunsch.model';

export interface WunschDialogData {
  wunsch?: Wunsch;
}

@Component({
  selector: 'app-create-wunsch-dialog',
  imports: [
    ReactiveFormsModule,
    MatDialogModule, MatFormFieldModule, MatInputModule, MatButtonModule, MatIconModule,
  ],
  templateUrl: './create-wunsch-dialog.component.html',
  styleUrl: './create-wunsch-dialog.component.scss',
})
export class CreateWunschDialogComponent {
  private readonly fb = inject(FormBuilder);
  readonly dialogRef = inject(MatDialogRef<CreateWunschDialogComponent>);
  readonly data: WunschDialogData | null = inject(MAT_DIALOG_DATA, { optional: true });

  readonly isEdit = !!this.data?.wunsch;

  readonly form = this.fb.nonNullable.group({
    title:       [this.data?.wunsch?.title       ?? '', [Validators.required, Validators.maxLength(200)]],
    description: [this.data?.wunsch?.description ?? '', [Validators.required, Validators.maxLength(2000)]],
    link:        [this.data?.wunsch?.link        ?? ''],
    priceMin:    [this.data?.wunsch?.priceMin    ?? (null as number | null)],
    priceMax:    [this.data?.wunsch?.priceMax    ?? (null as number | null)],
  }, { validators: priceRangeValidator });

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    const { title, description, link, priceMin, priceMax } = this.form.getRawValue();
    this.dialogRef.close({
      title,
      description,
      link: link || null,
      priceMin: priceMin ?? null,
      priceMax: priceMax ?? null,
    });
  }

  cancel(): void {
    this.dialogRef.close();
  }
}

function priceRangeValidator(group: AbstractControl): ValidationErrors | null {
  const min = group.get('priceMin')?.value;
  const max = group.get('priceMax')?.value;
  if (min != null && max != null && max < min) {
    return { priceRange: true };
  }
  return null;
}
