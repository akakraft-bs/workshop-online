import { Component, inject, OnInit, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatDialogModule, MatDialogRef, MAT_DIALOG_DATA, MatDialog } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatTableModule } from '@angular/material/table';
import { MatTooltipModule } from '@angular/material/tooltip';
import { RouterLink } from '@angular/router';
import { ApiService } from '../../../core/api/api.service';

export interface AblageortOverview {
  id?: string;
  name: string;
  color?: string;
  itemCount: number;
}

interface AblageortDialogResult {
  id?: string;
  name: string;
  color?: string;
}

export interface AblageortEditDialogData {
  item: AblageortOverview | null;
}

interface ConflictInfo {
  id: string;
  name: string;
  color?: string;
}

@Component({
  selector: 'app-ablageort-edit-dialog',
  imports: [
    ReactiveFormsModule,
    MatDialogModule, MatFormFieldModule, MatInputModule,
    MatButtonModule, MatIconModule, MatProgressSpinnerModule,
  ],
  template: `
    <h2 mat-dialog-title>{{ data.item ? 'Ablageort bearbeiten' : 'Ablageort anlegen' }}</h2>
    <mat-dialog-content>
      @if (!conflictInfo()) {
        <form [formGroup]="form" class="form">
          <mat-form-field appearance="outline">
            <mat-label>Name</mat-label>
            <input matInput formControlName="name" placeholder="z. B. Regal A3">
            @if (form.controls.name.hasError('required')) {
              <mat-error>Pflichtfeld</mat-error>
            }
          </mat-form-field>

          <div class="color-row">
            <div class="color-header">
              <span class="color-label">Farbe (optional)</span>
              @if (useColor()) {
                <button type="button" mat-button (click)="clearColor()">
                  <mat-icon>clear</mat-icon>
                  Entfernen
                </button>
              }
            </div>
            @if (useColor()) {
              <div class="color-picker-row">
                <input type="color" formControlName="color" class="color-input">
                <span class="preview-tag">
                  <span class="tag-dot" [style.background]="form.controls.color.value || '#000'"></span>
                  {{ form.controls.name.value || 'Vorschau' }}
                </span>
              </div>
            } @else {
              <button type="button" mat-stroked-button (click)="enableColor()">
                <mat-icon>palette</mat-icon>
                Farbe zuweisen
              </button>
            }
          </div>
        </form>
      } @else {
        <div class="conflict-box">
          <mat-icon class="conflict-icon">merge</mat-icon>
          <div class="conflict-text">
            <strong>„{{ conflictInfo()!.name }}" existiert bereits.</strong>
            <p>
              Alle Einträge von „{{ data.item?.name }}" werden zu „{{ conflictInfo()!.name }}"
              verschoben. Die bisherige Farbe von „{{ conflictInfo()!.name }}" bleibt erhalten.
            </p>
            <div class="merge-preview">
              <span class="preview-tag">
                @if (conflictInfo()!.color) {
                  <span class="tag-dot" [style.background]="conflictInfo()!.color"></span>
                }
                {{ conflictInfo()!.name }}
              </span>
            </div>
          </div>
        </div>
      }
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      @if (!conflictInfo()) {
        <button mat-button mat-dialog-close>Abbrechen</button>
        <button mat-flat-button color="primary" (click)="save()" [disabled]="saving()">
          @if (saving()) { <mat-spinner diameter="18"></mat-spinner> }
          @else { Speichern }
        </button>
      } @else {
        <button mat-button (click)="conflictInfo.set(null)" [disabled]="saving()">Zurück</button>
        <button mat-flat-button color="warn" (click)="merge()" [disabled]="saving()">
          @if (saving()) { <mat-spinner diameter="18"></mat-spinner> }
          @else { Zusammenführen }
        </button>
      }
    </mat-dialog-actions>
  `,
  styles: [`
    .form { display: flex; flex-direction: column; gap: 20px; padding-top: 8px; min-width: 300px; }
    mat-form-field { width: 100%; }
    .color-label { font-size: 0.875rem; color: var(--mat-sys-on-surface-variant); }
    .color-header { display: flex; align-items: center; justify-content: space-between; margin-bottom: 8px; }
    .color-picker-row { display: flex; align-items: center; gap: 16px; }
    .color-input { width: 52px; height: 40px; border: 1px solid var(--mat-sys-outline); border-radius: 6px; padding: 3px; cursor: pointer; background: none; }
    .preview-tag { display: inline-flex; align-items: center; gap: 6px; padding: 4px 12px; border-radius: 12px; background: var(--mat-sys-surface-variant); font-size: 0.8125rem; }
    .tag-dot { width: 10px; height: 10px; border-radius: 50%; flex-shrink: 0; }
    .conflict-box { display: flex; gap: 12px; padding: 16px; border-radius: 8px; background: color-mix(in srgb, var(--mat-sys-error) 8%, transparent); border: 1px solid color-mix(in srgb, var(--mat-sys-error) 24%, transparent); min-width: 300px; }
    .conflict-icon { color: var(--mat-sys-error); flex-shrink: 0; margin-top: 2px; }
    .conflict-text { font-size: 0.875rem; p { margin: 6px 0 0; color: var(--mat-sys-on-surface-variant); line-height: 1.5; } }
    .merge-preview { margin-top: 12px; }
  `],
})
export class AblageortEditDialogComponent {
  private readonly api = inject(ApiService);
  private readonly snackBar = inject(MatSnackBar);
  private readonly dialogRef = inject(MatDialogRef<AblageortEditDialogComponent>);
  readonly data: AblageortEditDialogData = inject(MAT_DIALOG_DATA);
  private readonly fb = inject(FormBuilder);

  readonly saving      = signal(false);
  readonly useColor    = signal(!!this.data.item?.color);
  readonly conflictInfo = signal<ConflictInfo | null>(null);

  readonly form = this.fb.nonNullable.group({
    name:  [this.data.item?.name ?? '', Validators.required],
    color: [this.data.item?.color ?? '#4caf50'],
  });

  enableColor(): void { this.useColor.set(true); }
  clearColor(): void  { this.useColor.set(false); }

  save(): void {
    if (this.form.invalid) { this.form.markAllAsTouched(); return; }
    this.saving.set(true);
    const { name, color } = this.form.getRawValue();
    const id       = this.data.item?.id;
    const newName  = name.trim();
    const colorVal = this.useColor() ? color : null;

    let request$;
    if (id) {
      // Location has an Ablageort record → PUT by ID
      request$ = this.api.put<AblageortDialogResult>(`/admin/ablageorte/${id}`, { name: newName, color: colorVal });
    } else if (this.data.item) {
      // Name-only location (no color/ID) → dedicated rename endpoint
      request$ = this.api.post<AblageortDialogResult>('/admin/ablageorte/rename-from-name', {
        currentName: this.data.item.name,
        newName,
        color: colorVal,
      });
    } else {
      // Create new Ablageort
      request$ = this.api.post<AblageortDialogResult>('/admin/ablageorte', { name: newName, color: colorVal });
    }

    request$.subscribe({
      next: result => this.dialogRef.close(result),
      error: (err) => {
        this.saving.set(false);
        if (err.status === 409 && err.error?.conflictsWithId) {
          this.conflictInfo.set({
            id:    err.error.conflictsWithId,
            name:  err.error.conflictsWithName,
            color: err.error.conflictsWithColor ?? undefined,
          });
        } else {
          this.snackBar.open('Fehler beim Speichern.', 'OK', { duration: 3000 });
        }
      },
    });
  }

  merge(): void {
    const conflict = this.conflictInfo();
    const sourceId = this.data.item?.id;
    const sourceName = this.data.item?.name;

    if (!conflict) return;

    this.saving.set(true);
    const request$ = sourceId
      ? this.api.post<AblageortDialogResult>(`/admin/ablageorte/${sourceId}/merge-into/${conflict.id}`, {})
      : this.api.post<AblageortDialogResult>(`/admin/ablageorte/${conflict.id}/merge-from-name`, { sourceName });

    request$.subscribe({
      next: result => this.dialogRef.close({ ...result, _merged: true }),
      error: () => {
        this.snackBar.open('Fehler beim Zusammenführen.', 'OK', { duration: 3000 });
        this.saving.set(false);
      },
    });
  }
}

@Component({
  selector: 'app-ablageort-verwaltung',
  imports: [
    MatTableModule, MatButtonModule, MatIconModule,
    MatProgressSpinnerModule, MatTooltipModule, MatCardModule, RouterLink,
  ],
  templateUrl: './ablageort-verwaltung.component.html',
  styleUrl: './ablageort-verwaltung.component.scss',
})
export class AblageortVerwaltungComponent implements OnInit {
  private readonly api      = inject(ApiService);
  private readonly snackBar = inject(MatSnackBar);
  private readonly dialog   = inject(MatDialog);

  readonly items            = signal<AblageortOverview[]>([]);
  readonly loading          = signal(true);
  readonly displayedColumns = ['color', 'name', 'itemCount', 'actions'];

  ngOnInit(): void { this.load(); }

  load(): void {
    this.loading.set(true);
    this.api.get<AblageortOverview[]>('/admin/ablageorte').subscribe({
      next: data => { this.items.set(data); this.loading.set(false); },
      error: () => {
        this.loading.set(false);
        this.snackBar.open('Laden fehlgeschlagen.', 'OK', { duration: 3000 });
      },
    });
  }

  openCreate(): void {
    this.dialog.open(AblageortEditDialogComponent, {
      width: '400px',
      data: { item: null } satisfies AblageortEditDialogData,
    }).afterClosed().subscribe((result: AblageortDialogResult | undefined) => {
      if (!result) return;
      const newItem: AblageortOverview = { id: result.id, name: result.name, color: result.color, itemCount: 0 };
      this.items.update(list => [...list, newItem].sort((a, b) => a.name.localeCompare(b.name, 'de')));
      this.snackBar.open(`„${result.name}" angelegt.`, undefined, { duration: 3000 });
    });
  }

  openEdit(item: AblageortOverview): void {
    this.dialog.open(AblageortEditDialogComponent, {
      width: '400px',
      data: { item } satisfies AblageortEditDialogData,
    }).afterClosed().subscribe((result: (AblageortDialogResult & { _merged?: boolean }) | undefined) => {
      if (!result) return;
      if (result._merged) {
        this.load();
        this.snackBar.open(`Ablageorte zusammengeführt zu „${result.name}".`, undefined, { duration: 3000 });
      } else {
        this.items.update(list =>
          list.map(i => i.name === item.name
            ? { ...i, id: result.id, name: result.name, color: result.color }
            : i
          ).sort((a, b) => a.name.localeCompare(b.name, 'de'))
        );
        this.snackBar.open(`„${result.name}" gespeichert.`, undefined, { duration: 3000 });
      }
    });
  }

  delete(item: AblageortOverview): void {
    if (!item.id) return;
    if (!confirm(`Farbkonfiguration für „${item.name}" löschen?\nDie zugehörigen Einträge bleiben erhalten.`)) return;

    this.api.delete<void>(`/admin/ablageorte/${item.id}`).subscribe({
      next: () => {
        this.items.update(list =>
          list
            .map(i => i.name === item.name ? { ...i, id: undefined, color: undefined } : i)
            .filter(i => i.id !== undefined || i.itemCount > 0)
        );
        this.snackBar.open('Konfiguration gelöscht.', undefined, { duration: 3000 });
      },
      error: () => this.snackBar.open('Löschen fehlgeschlagen.', 'OK', { duration: 3000 }),
    });
  }
}
