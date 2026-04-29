import { Component, inject, signal } from '@angular/core';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatDialogModule, MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatNativeDateModule } from '@angular/material/core';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar } from '@angular/material/snack-bar';
import { Observable, switchMap, of } from 'rxjs';
import { ApiService } from '../../../core/api/api.service';
import { ProjektDto, ProjektStatus } from '../../../models/verein.models';

@Component({
  selector: 'app-projekt-dialog',
  imports: [
    ReactiveFormsModule, MatDialogModule, MatButtonModule,
    MatFormFieldModule, MatInputModule, MatSelectModule,
    MatDatepickerModule, MatNativeDateModule,
    MatIconModule, MatProgressSpinnerModule,
  ],
  templateUrl: './projekt-dialog.component.html',
  styleUrl: './projekt-dialog.component.scss',
})
export class ProjektDialogComponent {
  private readonly api      = inject(ApiService);
  private readonly fb       = inject(FormBuilder);
  private readonly snackBar = inject(MatSnackBar);
  readonly dialogRef        = inject(MatDialogRef<ProjektDialogComponent>);
  readonly data: ProjektDto | null = inject(MAT_DIALOG_DATA);

  readonly isEdit        = this.data !== null;
  readonly saving        = signal(false);
  readonly selectedFile  = signal<File | null>(null);
  readonly planUrl       = signal<string | null>(this.data?.projektplanUrl ?? null);

  readonly statusOptions: ProjektStatus[] = ['Geplant', 'Gestartet', 'Abgeschlossen'];

  readonly form = this.fb.nonNullable.group({
    name:            [this.data?.name            ?? '', Validators.required],
    description:     [this.data?.description     ?? ''],
    plannedStartDate:[this.data ? new Date(this.data.plannedStartDate) : null as unknown as Date, Validators.required],
    durationWeeks:   [this.data?.durationWeeks   ?? 1, [Validators.required, Validators.min(1)]],
    actualStartDate: [this.data?.actualStartDate ? new Date(this.data.actualStartDate) : null as unknown as Date],
    status:          [this.data?.status          ?? 'Geplant' as ProjektStatus, Validators.required],
  });

  onFileSelected(event: Event): void {
    const file = (event.target as HTMLInputElement).files?.[0];
    if (file) this.selectedFile.set(file);
  }

  clearPlan(): void {
    this.selectedFile.set(null);
    this.planUrl.set(null);
  }

  save(): void {
    if (this.form.invalid) { this.form.markAllAsTouched(); return; }
    this.saving.set(true);
    const v = this.form.getRawValue();
    const file = this.selectedFile();

    const upload$: Observable<{ url: string } | null> = file
      ? (() => {
          const fd = new FormData();
          fd.append('file', file);
          return this.api.postFormData<{ url: string }>('/uploads/projektplan', fd);
        })()
      : of(null);

    upload$.pipe(
      switchMap((res: { url: string } | null) => {
        const projektplanUrl = res?.url ?? this.planUrl();
        const body = {
          name:             v.name,
          description:      v.description || null,
          plannedStartDate: v.plannedStartDate.toISOString(),
          durationWeeks:    v.durationWeeks,
          actualStartDate:  v.actualStartDate ? v.actualStartDate.toISOString() : null,
          status:           v.status,
          projektplanUrl,
        };
        return this.isEdit
          ? this.api.put<ProjektDto>(`/verein/projekte/${this.data!.id}`, body)
          : this.api.post<ProjektDto>('/verein/projekte', body);
      })
    ).subscribe({
      next: (result: ProjektDto) => this.dialogRef.close(result),
      error: () => { this.snackBar.open('Speichern fehlgeschlagen.', 'OK', { duration: 3000 }); this.saving.set(false); },
    });
  }
}
