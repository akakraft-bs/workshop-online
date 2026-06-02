import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { FormsModule, ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatChipsModule } from '@angular/material/chips';
import { MatDialogModule, MatDialogRef, MAT_DIALOG_DATA, MatDialog } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatTableModule } from '@angular/material/table';
import { MatTooltipModule } from '@angular/material/tooltip';
import { ApiService } from '../../../core/api/api.service';
import {
  PartnerOverview, PartnerStatus,
  PARTNER_STATUS_LABELS,
} from '../../../models/crm.model';

const ALL_STATUSES: PartnerStatus[] = [
  'Potentiell', 'Angeschrieben', 'InVerhandlung', 'Aktiv', 'Abgelehnt', 'Inaktiv',
];

interface PartnerDialogData { partner: PartnerOverview | null; }

@Component({
  selector: 'app-partner-dialog',
  imports: [
    ReactiveFormsModule, MatDialogModule, MatFormFieldModule, MatInputModule,
    MatSelectModule, MatButtonModule, MatProgressSpinnerModule,
  ],
  template: `
    <h2 mat-dialog-title>{{ data.partner ? 'Partner bearbeiten' : 'Neuer Partner' }}</h2>
    <mat-dialog-content>
      <form [formGroup]="form" class="form">
        <mat-form-field appearance="outline">
          <mat-label>Name</mat-label>
          <input matInput formControlName="name">
        </mat-form-field>
        <mat-form-field appearance="outline">
          <mat-label>Kategorie</mat-label>
          <input matInput formControlName="kategorie" placeholder="z. B. Sponsor, Kooperationspartner">
        </mat-form-field>
        <mat-form-field appearance="outline">
          <mat-label>Status</mat-label>
          <mat-select formControlName="status">
            @for (s of statuses; track s) {
              <mat-option [value]="s">{{ statusLabel(s) }}</mat-option>
            }
          </mat-select>
        </mat-form-field>
        <mat-form-field appearance="outline">
          <mat-label>Website</mat-label>
          <input matInput formControlName="website" placeholder="https://...">
        </mat-form-field>
        <mat-form-field appearance="outline">
          <mat-label>Notizen</mat-label>
          <textarea matInput formControlName="notizen" rows="3"></textarea>
        </mat-form-field>
      </form>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button mat-dialog-close>Abbrechen</button>
      <button mat-flat-button color="primary" (click)="save()" [disabled]="saving()">
        @if (saving()) { <mat-spinner diameter="18"></mat-spinner> } @else { Speichern }
      </button>
    </mat-dialog-actions>
  `,
  styles: [`.form { display: flex; flex-direction: column; gap: 12px; padding-top: 8px; min-width: 360px; } mat-form-field { width: 100%; }`],
})
export class PartnerDialogComponent {
  private readonly api = inject(ApiService);
  private readonly snackBar = inject(MatSnackBar);
  private readonly dialogRef = inject(MatDialogRef<PartnerDialogComponent>);
  readonly data: PartnerDialogData = inject(MAT_DIALOG_DATA);
  private readonly fb = inject(FormBuilder);

  readonly saving = signal(false);
  readonly statuses = ALL_STATUSES;
  readonly statusLabel = (s: PartnerStatus) => PARTNER_STATUS_LABELS[s];

  readonly form = this.fb.nonNullable.group({
    name: [this.data.partner?.name ?? '', Validators.required],
    kategorie: [this.data.partner?.kategorie ?? ''],
    status: [this.data.partner?.status ?? 'Potentiell' as PartnerStatus],
    website: [this.data.partner?.website ?? ''],
    notizen: [''],
  });

  save(): void {
    if (this.form.invalid) { this.form.markAllAsTouched(); return; }
    this.saving.set(true);
    const { name, kategorie, status, website, notizen } = this.form.getRawValue();
    const body = {
      name: name.trim(),
      kategorie: kategorie.trim() || null,
      status,
      website: website.trim() || null,
      notizen: notizen.trim() || null,
    };
    const id = this.data.partner?.id;
    const req$ = id
      ? this.api.put<PartnerOverview>(`/crm/partner/${id}`, body)
      : this.api.post<PartnerOverview>('/crm/partner', body);

    req$.subscribe({
      next: result => this.dialogRef.close(result),
      error: () => {
        this.saving.set(false);
        this.snackBar.open('Fehler beim Speichern.', 'OK', { duration: 3000 });
      },
    });
  }
}

@Component({
  selector: 'app-crm-list',
  imports: [
    RouterLink, MatTableModule, MatButtonModule, MatIconModule, MatChipsModule,
    MatProgressSpinnerModule, MatTooltipModule, MatInputModule, FormsModule,
  ],
  templateUrl: './crm-list.component.html',
  styleUrl: './crm-list.component.scss',
})
export class CrmListComponent implements OnInit {
  private readonly api = inject(ApiService);
  private readonly dialog = inject(MatDialog);
  private readonly snackBar = inject(MatSnackBar);

  readonly loading = signal(true);
  readonly all = signal<PartnerOverview[]>([]);
  readonly search = signal('');
  readonly activeStatus = signal<PartnerStatus | null>(null);

  readonly statuses = ALL_STATUSES;
  readonly statusLabels = PARTNER_STATUS_LABELS;
  readonly displayedColumns = ['name', 'kategorie', 'status', 'letzterKontakt', 'anzahlKontakte', 'actions'];

  readonly filtered = computed(() => {
    const q = this.search().toLowerCase();
    const s = this.activeStatus();
    return this.all().filter(p =>
      (!s || p.status === s) &&
      (!q || p.name.toLowerCase().includes(q) || (p.kategorie?.toLowerCase().includes(q) ?? false))
    );
  });

  ngOnInit(): void { this.load(); }

  load(): void {
    this.loading.set(true);
    this.api.get<PartnerOverview[]>('/crm/partner').subscribe({
      next: data => { this.all.set(data); this.loading.set(false); },
      error: () => { this.loading.set(false); this.snackBar.open('Laden fehlgeschlagen.', 'OK', { duration: 3000 }); },
    });
  }

  openCreate(): void {
    this.dialog.open(PartnerDialogComponent, { width: '460px', data: { partner: null } })
      .afterClosed().subscribe((result: PartnerOverview | undefined) => {
        if (!result) return;
        this.all.update(list => [...list, result].sort((a, b) => a.name.localeCompare(b.name, 'de')));
        this.snackBar.open(`„${result.name}" angelegt.`, undefined, { duration: 3000 });
      });
  }

  openEdit(partner: PartnerOverview, event: Event): void {
    event.stopPropagation();
    this.dialog.open(PartnerDialogComponent, { width: '460px', data: { partner } })
      .afterClosed().subscribe((result: PartnerOverview | undefined) => {
        if (!result) return;
        this.all.update(list =>
          list.map(p => p.id === partner.id ? { ...p, ...result } : p)
              .sort((a, b) => a.name.localeCompare(b.name, 'de'))
        );
        this.snackBar.open(`„${result.name}" gespeichert.`, undefined, { duration: 3000 });
      });
  }

  delete(partner: PartnerOverview, event: Event): void {
    event.stopPropagation();
    if (!confirm(`„${partner.name}" wirklich löschen? Alle Kontakteinträge werden ebenfalls gelöscht.`)) return;
    this.api.delete<void>(`/crm/partner/${partner.id}`).subscribe({
      next: () => {
        this.all.update(list => list.filter(p => p.id !== partner.id));
        this.snackBar.open('Partner gelöscht.', undefined, { duration: 3000 });
      },
      error: () => this.snackBar.open('Löschen fehlgeschlagen.', 'OK', { duration: 3000 }),
    });
  }

  statusLabel(s: string): string {
    return PARTNER_STATUS_LABELS[s as PartnerStatus] ?? s;
  }

  formatDate(iso: string | null): string {
    if (!iso) return '—';
    const d = new Date(iso);
    return `${pad(d.getDate())}.${pad(d.getMonth() + 1)}.${d.getFullYear()}`;
  }

  toggleStatus(s: PartnerStatus): void {
    this.activeStatus.set(this.activeStatus() === s ? null : s);
  }
}

const pad = (n: number) => n.toString().padStart(2, '0');
