import { Component, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatButtonToggleModule } from '@angular/material/button-toggle';
import { MatIconModule } from '@angular/material/icon';
import { Wunsch, WunschStatus } from '../../../models/wunsch.model';

export interface CloseWunschDialogData {
  wunsch: Wunsch;
}

@Component({
  selector: 'app-close-wunsch-dialog',
  imports: [
    ReactiveFormsModule,
    MatDialogModule, MatFormFieldModule, MatInputModule,
    MatButtonModule, MatButtonToggleModule, MatIconModule,
  ],
  templateUrl: './close-wunsch-dialog.component.html',
  styleUrl: './close-wunsch-dialog.component.scss',
})
export class CloseWunschDialogComponent {
  private readonly fb = inject(FormBuilder);
  readonly dialogRef = inject(MatDialogRef<CloseWunschDialogComponent>);
  readonly data = inject<CloseWunschDialogData>(MAT_DIALOG_DATA);

  readonly form = this.fb.nonNullable.group({
    status: ['Angeschafft' as WunschStatus],
    closeNote: [''],
  });

  submit(): void {
    const { status, closeNote } = this.form.getRawValue();
    this.dialogRef.close({ status, closeNote: closeNote || null });
  }

  cancel(): void {
    this.dialogRef.close();
  }
}
