import { Component, inject, OnInit, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatDialogModule, MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatRadioModule } from '@angular/material/radio';
import { MatSelectModule } from '@angular/material/select';
import { MatSnackBar } from '@angular/material/snack-bar';
import { of, switchMap, Observable } from 'rxjs';
import { ApiService } from '../../../core/api/api.service';
import { Aufgabe, AssignableUser } from '../../../models/aufgabe.model';

type AssignMode = 'none' | 'member' | 'extern';

@Component({
  selector: 'app-aufgabe-dialog',
  imports: [
    ReactiveFormsModule,
    MatDialogModule, MatFormFieldModule, MatInputModule,
    MatSelectModule, MatRadioModule, MatCheckboxModule,
    MatButtonModule, MatIconModule, MatProgressSpinnerModule,
  ],
  templateUrl: './aufgabe-dialog.component.html',
  styleUrl: './aufgabe-dialog.component.scss',
})
export class AufgabeDialogComponent implements OnInit {
  private readonly fb       = inject(FormBuilder);
  private readonly api      = inject(ApiService);
  private readonly snackBar = inject(MatSnackBar);
  readonly dialogRef        = inject(MatDialogRef<AufgabeDialogComponent>);
  readonly data: Aufgabe | null = inject(MAT_DIALOG_DATA);

  readonly saving       = signal(false);
  readonly users        = signal<AssignableUser[]>([]);
  readonly selectedFile = signal<File | null>(null);
  readonly previewUrl   = signal<string | null>(null);
  readonly keepFotoUrl  = signal<string | null>(this.data?.fotoUrl ?? null);

  readonly assignMode = signal<AssignMode>(
    this.data?.assignedUserId ? 'member' :
    this.data?.assignedName   ? 'extern' : 'none'
  );

  readonly form = this.fb.nonNullable.group({
    titel:          [this.data?.titel        ?? '', [Validators.required, Validators.maxLength(200)]],
    beschreibung:   [this.data?.beschreibung ?? '', Validators.required],
    assignedUserId: [this.data?.assignedUserId ?? null as string | null],
    assignedName:   [this.data?.assignedName   ?? ''],
    erledigt:       [this.data?.status === 'Erledigt'],
  });

  ngOnInit(): void {
    this.api.get<AssignableUser[]>('/users/assignable').subscribe({
      next: list => this.users.set(list),
    });
  }

  setAssignMode(mode: AssignMode): void {
    this.assignMode.set(mode);
    if (mode !== 'member') this.form.patchValue({ assignedUserId: null });
    if (mode !== 'extern') this.form.patchValue({ assignedName: '' });
  }

  onFileSelected(event: Event): void {
    const file = (event.target as HTMLInputElement).files?.[0];
    if (!file) return;
    if (!['image/jpeg', 'image/png', 'image/webp', 'image/gif'].includes(file.type)) {
      this.snackBar.open('Erlaubt: JPEG, PNG, WebP, GIF.', 'OK', { duration: 4000 });
      return;
    }
    if (file.size > 5 * 1024 * 1024) {
      this.snackBar.open('Maximal 5 MB erlaubt.', 'OK', { duration: 4000 });
      return;
    }
    this.selectedFile.set(file);
    this.previewUrl.set(URL.createObjectURL(file));
    this.keepFotoUrl.set(null);
  }

  clearImage(): void {
    this.selectedFile.set(null);
    this.previewUrl.set(null);
    this.keepFotoUrl.set(null);
  }

  save(): void {
    if (this.form.invalid) { this.form.markAllAsTouched(); return; }
    this.saving.set(true);

    const { titel, beschreibung, assignedUserId, assignedName, erledigt } = this.form.getRawValue();
    const mode = this.assignMode();
    const file = this.selectedFile();

    const upload$: Observable<{ url: string } | null> = file
      ? (() => {
          const fd = new FormData();
          fd.append('file', file);
          return this.api.postFormData<{ url: string }>('/uploads/aufgabe', fd);
        })()
      : of(null);

    upload$.pipe(
      switchMap((res: { url: string } | null) => {
        const fotoUrl = res?.url ?? this.keepFotoUrl() ?? null;
        const body = {
          titel,
          beschreibung,
          fotoUrl,
          assignedUserId: mode === 'member' ? (assignedUserId || null) : null,
          assignedName:   mode === 'extern' ? (assignedName.trim() || null) : null,
          erledigt,
        };
        return this.data
          ? this.api.put<Aufgabe>(`/aufgaben/${this.data.id}`, body)
          : this.api.post<Aufgabe>('/aufgaben', body);
      })
    ).subscribe({
      next: result => this.dialogRef.close(result),
      error: () => {
        this.snackBar.open('Speichern fehlgeschlagen.', 'OK', { duration: 3000 });
        this.saving.set(false);
      },
    });
  }
}
