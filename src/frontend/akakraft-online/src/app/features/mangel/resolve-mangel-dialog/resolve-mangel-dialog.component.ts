import { Component, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatButtonToggleModule } from '@angular/material/button-toggle';
import { Mangel, MangelStatus } from '../../../models/mangel.model';

export interface ResolveMangelDialogData {
  mangel: Mangel;
}

interface StatusOption {
  value: MangelStatus;
  label: string;
  icon: string;
}

@Component({
  selector: 'app-resolve-mangel-dialog',
  imports: [
    ReactiveFormsModule,
    MatDialogModule, MatFormFieldModule, MatInputModule,
    MatButtonModule, MatButtonToggleModule,
  ],
  templateUrl: './resolve-mangel-dialog.component.html',
  styleUrl: './resolve-mangel-dialog.component.scss',
})
export class ResolveMangelDialogComponent {
  private readonly fb = inject(FormBuilder);
  readonly dialogRef = inject(MatDialogRef<ResolveMangelDialogComponent>);
  readonly data = inject<ResolveMangelDialogData>(MAT_DIALOG_DATA);

  readonly statusOptions: StatusOption[] = [
    { value: 'Kenntnisgenommen', label: 'Kenntnisgenommen', icon: 'visibility' },
    { value: 'Behoben', label: 'Behoben', icon: 'check_circle' },
    { value: 'Abgelehnt', label: 'Abgelehnt', icon: 'cancel' },
  ];

  readonly form = this.fb.nonNullable.group({
    status: ['Kenntnisgenommen' as MangelStatus],
    note: [''],
  });

  submit(): void {
    const { status, note } = this.form.getRawValue();
    this.dialogRef.close({ status, note: note || null });
  }

  cancel(): void {
    this.dialogRef.close();
  }
}
