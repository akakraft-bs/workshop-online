import { Component, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatNativeDateModule } from '@angular/material/core';
import { ApiService } from '../../../core/api/api.service';
import { MatSnackBar } from '@angular/material/snack-bar';

function toDateStr(d: Date): string {
  const pad = (n: number) => String(n).padStart(2, '0');
  return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}`;
}

@Component({
  selector: 'app-hallenbuch-statistik-dialog',
  imports: [
    ReactiveFormsModule,
    MatDialogModule, MatFormFieldModule, MatInputModule,
    MatButtonModule, MatIconModule,
    MatDatepickerModule, MatNativeDateModule,
  ],
  templateUrl: './hallenbuch-statistik-dialog.component.html',
  styleUrl: './hallenbuch-statistik-dialog.component.scss',
})
export class HallenbuchStatistikDialogComponent {
  private readonly fb = inject(FormBuilder);
  private readonly api = inject(ApiService);
  private readonly snackBar = inject(MatSnackBar);
  readonly dialogRef = inject(MatDialogRef<HallenbuchStatistikDialogComponent>);

  readonly form = this.fb.group({
    from: [null as Date | null, Validators.required],
    to:   [null as Date | null, Validators.required],
  });

  download(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const { from, to } = this.form.getRawValue();
    const fromDate = from!;
    fromDate.setHours(0, 0, 0, 0);
    const toDate = to!;
    toDate.setHours(23, 59, 59, 999);

    const fromIso = fromDate.toISOString();
    const toIso   = toDate.toISOString();
    const fromStr = toDateStr(from!);
    const toStr   = toDateStr(to!);

    this.api.getBlob(`/hallenbuch/statistik?from=${encodeURIComponent(fromIso)}&to=${encodeURIComponent(toIso)}`)
      .subscribe({
        next: blob => {
          const url = URL.createObjectURL(blob);
          const a   = document.createElement('a');
          a.href    = url;
          a.download = `hallenbuch-statistik_${fromStr}_${toStr}.csv`;
          a.click();
          URL.revokeObjectURL(url);
          this.dialogRef.close();
        },
        error: () => this.snackBar.open('Download fehlgeschlagen.', 'OK', { duration: 3000 }),
      });
  }

  cancel(): void {
    this.dialogRef.close();
  }
}
