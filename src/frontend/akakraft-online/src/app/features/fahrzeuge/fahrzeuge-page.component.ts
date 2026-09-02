import { Component, inject, OnInit, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatDialog, MatDialogModule, MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatTooltipModule } from '@angular/material/tooltip';
import { FahrzeugService } from '../../core/fahrzeug/fahrzeug.service';
import { Fahrzeug } from '../../models/fahrzeug.model';

interface FahrzeugDialogData {
  fahrzeug: Fahrzeug | null;
}

@Component({
  selector: 'app-fahrzeug-dialog',
  imports: [
    ReactiveFormsModule, MatDialogModule, MatFormFieldModule, MatInputModule,
    MatCheckboxModule, MatButtonModule, MatProgressSpinnerModule, MatIconModule,
  ],
  template: `
    <h2 mat-dialog-title>{{ data.fahrzeug ? 'Fahrzeug bearbeiten' : 'Fahrzeug hinzufügen' }}</h2>
    <mat-dialog-content>
      <form [formGroup]="form" class="fz-form">
        <mat-form-field appearance="outline">
          <mat-label>Marke</mat-label>
          <input matInput formControlName="marke" placeholder="z. B. VW" maxlength="80">
        </mat-form-field>
        <mat-form-field appearance="outline">
          <mat-label>Modell (optional)</mat-label>
          <input matInput formControlName="modell" placeholder="z. B. Golf" maxlength="80">
        </mat-form-field>
        <mat-form-field appearance="outline">
          <mat-label>Kennzeichen</mat-label>
          <input matInput formControlName="kennzeichen" placeholder="BS-XX 123" maxlength="16"
                 style="text-transform: uppercase">
        </mat-form-field>
        <mat-checkbox formControlName="istStandard">Als Standardfahrzeug verwenden</mat-checkbox>
      </form>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button mat-dialog-close>Abbrechen</button>
      <button mat-flat-button color="primary" (click)="save()" [disabled]="form.invalid || saving()">
        @if (saving()) { <mat-spinner diameter="18" /> } @else { Speichern }
      </button>
    </mat-dialog-actions>
  `,
  styles: [`.fz-form { display: flex; flex-direction: column; gap: 8px; padding-top: 8px; min-width: 300px; } mat-form-field { width: 100%; }`],
})
export class FahrzeugDialogComponent {
  private readonly fb = inject(FormBuilder);
  private readonly svc = inject(FahrzeugService);
  private readonly snackBar = inject(MatSnackBar);
  private readonly dialogRef = inject(MatDialogRef<FahrzeugDialogComponent>);
  readonly data: FahrzeugDialogData = inject(MAT_DIALOG_DATA);

  readonly saving = signal(false);

  readonly form = this.fb.group({
    marke: [this.data.fahrzeug?.marke ?? '', [Validators.required, Validators.maxLength(80)]],
    modell: [this.data.fahrzeug?.modell ?? '', [Validators.maxLength(80)]],
    kennzeichen: [this.data.fahrzeug?.kennzeichen ?? '', [Validators.required, Validators.maxLength(16)]],
    istStandard: [this.data.fahrzeug?.istStandard ?? false],
  });

  save(): void {
    if (this.form.invalid || this.saving()) return;
    this.saving.set(true);
    const v = this.form.getRawValue();
    const dto = {
      marke: (v.marke ?? '').trim(),
      modell: (v.modell ?? '').trim() || null,
      kennzeichen: (v.kennzeichen ?? '').trim().toUpperCase(),
      istStandard: !!v.istStandard,
    };
    const req$ = this.data.fahrzeug
      ? this.svc.update(this.data.fahrzeug.id, dto)
      : this.svc.create(dto);
    req$.subscribe({
      next: result => this.dialogRef.close(result),
      error: err => {
        this.saving.set(false);
        this.snackBar.open(typeof err?.error === 'string' ? err.error : 'Fehler beim Speichern.', 'OK', { duration: 4000 });
      },
    });
  }
}

@Component({
  selector: 'app-fahrzeuge-page',
  imports: [
    MatButtonModule, MatIconModule, MatProgressSpinnerModule, MatTooltipModule,
  ],
  templateUrl: './fahrzeuge-page.component.html',
  styleUrl: './fahrzeuge-page.component.scss',
})
export class FahrzeugePageComponent implements OnInit {
  private readonly svc = inject(FahrzeugService);
  private readonly dialog = inject(MatDialog);
  private readonly snackBar = inject(MatSnackBar);

  readonly fahrzeuge = signal<Fahrzeug[]>([]);
  readonly loading = signal(true);

  ngOnInit(): void {
    this.load();
  }

  private load(): void {
    this.loading.set(true);
    this.svc.list().subscribe({
      next: list => { this.fahrzeuge.set(list); this.loading.set(false); },
      error: () => { this.loading.set(false); this.snackBar.open('Fahrzeuge konnten nicht geladen werden.', 'OK', { duration: 3000 }); },
    });
  }

  openDialog(fahrzeug: Fahrzeug | null): void {
    this.dialog.open(FahrzeugDialogComponent, { data: { fahrzeug }, width: '380px', maxWidth: '95vw' })
      .afterClosed().subscribe((result: Fahrzeug | undefined) => {
        if (result) this.load();
      });
  }

  remove(fahrzeug: Fahrzeug): void {
    if (!confirm(`${fahrzeug.marke} ${fahrzeug.kennzeichen} löschen?`)) return;
    this.svc.remove(fahrzeug.id).subscribe({
      next: () => { this.fahrzeuge.update(l => l.filter(f => f.id !== fahrzeug.id)); this.snackBar.open('Fahrzeug gelöscht.', undefined, { duration: 2500 }); },
      error: () => this.snackBar.open('Fehler beim Löschen.', 'OK', { duration: 3000 }),
    });
  }
}
