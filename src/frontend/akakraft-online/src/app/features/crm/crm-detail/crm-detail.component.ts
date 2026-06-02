import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { FormsModule, ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatChipsModule } from '@angular/material/chips';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatDialogModule, MatDialogRef, MAT_DIALOG_DATA, MatDialog } from '@angular/material/dialog';
import { MatDividerModule } from '@angular/material/divider';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatNativeDateModule } from '@angular/material/core';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatTooltipModule } from '@angular/material/tooltip';
import { ApiService } from '../../../core/api/api.service';
import {
  AnsprechpartnerDto, KontakteintragDto, KontaktKanal, KontaktReaktion,
  PartnerDetail, PartnerOverview, PartnerStatus,
  KANAL_ICONS, KANAL_LABELS, PARTNER_STATUS_LABELS, REAKTION_LABELS,
} from '../../../models/crm.model';
import { PartnerDialogComponent } from '../crm-list/crm-list.component';

// ---- Ansprechpartner Dialog -------------------------------------------------

interface ApDialogData { ap: AnsprechpartnerDto | null; partnerId: string; }

@Component({
  selector: 'app-ap-dialog',
  imports: [ReactiveFormsModule, MatDialogModule, MatFormFieldModule, MatInputModule, MatButtonModule, MatProgressSpinnerModule],
  template: `
    <h2 mat-dialog-title>{{ data.ap ? 'Kontaktperson bearbeiten' : 'Kontaktperson anlegen' }}</h2>
    <mat-dialog-content>
      <form [formGroup]="form" class="form">
        <mat-form-field appearance="outline"><mat-label>Name</mat-label><input matInput formControlName="name"></mat-form-field>
        <mat-form-field appearance="outline"><mat-label>Position</mat-label><input matInput formControlName="position" placeholder="z. B. Marketingleitung"></mat-form-field>
        <mat-form-field appearance="outline"><mat-label>E-Mail</mat-label><input matInput formControlName="email" type="email"></mat-form-field>
        <mat-form-field appearance="outline"><mat-label>Telefon</mat-label><input matInput formControlName="telefon"></mat-form-field>
        <mat-form-field appearance="outline"><mat-label>Notizen</mat-label><textarea matInput formControlName="notizen" rows="2"></textarea></mat-form-field>
      </form>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button mat-dialog-close>Abbrechen</button>
      <button mat-flat-button color="primary" (click)="save()" [disabled]="saving()">
        @if (saving()) { <mat-spinner diameter="18"></mat-spinner> } @else { Speichern }
      </button>
    </mat-dialog-actions>
  `,
  styles: [`.form { display: flex; flex-direction: column; gap: 10px; padding-top: 8px; min-width: 340px; } mat-form-field { width: 100%; }`],
})
export class ApDialogComponent {
  private readonly api = inject(ApiService);
  private readonly snackBar = inject(MatSnackBar);
  private readonly dialogRef = inject(MatDialogRef<ApDialogComponent>);
  readonly data: ApDialogData = inject(MAT_DIALOG_DATA);
  private readonly fb = inject(FormBuilder);

  readonly saving = signal(false);
  readonly form = this.fb.nonNullable.group({
    name: [this.data.ap?.name ?? '', Validators.required],
    position: [this.data.ap?.position ?? ''],
    email: [this.data.ap?.email ?? ''],
    telefon: [this.data.ap?.telefon ?? ''],
    notizen: [this.data.ap?.notizen ?? ''],
  });

  save(): void {
    if (this.form.invalid) { this.form.markAllAsTouched(); return; }
    this.saving.set(true);
    const v = this.form.getRawValue();
    const body = { name: v.name.trim(), position: v.position.trim() || null, email: v.email.trim() || null, telefon: v.telefon.trim() || null, notizen: v.notizen.trim() || null };
    const req$ = this.data.ap
      ? this.api.put<AnsprechpartnerDto>(`/crm/partner/${this.data.partnerId}/ansprechpartner/${this.data.ap.id}`, body)
      : this.api.post<AnsprechpartnerDto>(`/crm/partner/${this.data.partnerId}/ansprechpartner`, body);
    req$.subscribe({
      next: r => this.dialogRef.close(r),
      error: () => { this.saving.set(false); this.snackBar.open('Fehler beim Speichern.', 'OK', { duration: 3000 }); },
    });
  }
}

// ---- Kontakteintrag Dialog --------------------------------------------------

const KANALE: KontaktKanal[] = ['Email', 'Telefon', 'Meeting', 'Brief', 'Sonstiges'];
const REAKTIONEN: KontaktReaktion[] = ['Positiv', 'Neutral', 'Negativ', 'KeineAntwort'];

interface KontaktDialogData {
  eintrag: KontakteintragDto | null;
  partnerId: string;
  ansprechpartner: AnsprechpartnerDto[];
}

@Component({
  selector: 'app-kontakt-dialog',
  imports: [
    ReactiveFormsModule, MatDialogModule, MatFormFieldModule, MatInputModule,
    MatSelectModule, MatButtonModule, MatProgressSpinnerModule,
    MatDatepickerModule, MatNativeDateModule,
  ],
  template: `
    <h2 mat-dialog-title>{{ data.eintrag ? 'Kontakteintrag bearbeiten' : 'Kontakteintrag hinzufügen' }}</h2>
    <mat-dialog-content>
      <form [formGroup]="form" class="form">
        <div class="row">
          <mat-form-field appearance="outline">
            <mat-label>Datum</mat-label>
            <input matInput [matDatepicker]="dp" formControlName="datum">
            <mat-datepicker-toggle matIconSuffix [for]="dp"></mat-datepicker-toggle>
            <mat-datepicker #dp></mat-datepicker>
          </mat-form-field>
          <mat-form-field appearance="outline">
            <mat-label>Kanal</mat-label>
            <mat-select formControlName="kanal">
              @for (k of kanale; track k) { <mat-option [value]="k">{{ kanalLabels[k] }}</mat-option> }
            </mat-select>
          </mat-form-field>
        </div>
        <mat-form-field appearance="outline">
          <mat-label>Reaktion</mat-label>
          <mat-select formControlName="reaktion">
            @for (r of reaktionen; track r) { <mat-option [value]="r">{{ reaktionLabels[r] }}</mat-option> }
          </mat-select>
        </mat-form-field>
        @if (data.ansprechpartner.length > 0) {
          <mat-form-field appearance="outline">
            <mat-label>Ansprechpartner (optional)</mat-label>
            <mat-select formControlName="ansprechpartnerId">
              <mat-option [value]="null">— Keiner —</mat-option>
              @for (ap of data.ansprechpartner; track ap.id) {
                <mat-option [value]="ap.id">{{ ap.name }}@if (ap.position) { · {{ ap.position }} }</mat-option>
              }
            </mat-select>
          </mat-form-field>
        }
        <mat-form-field appearance="outline">
          <mat-label>Zusammenfassung</mat-label>
          <textarea matInput formControlName="zusammenfassung" rows="3" placeholder="Was wurde besprochen?"></textarea>
        </mat-form-field>
        <mat-form-field appearance="outline">
          <mat-label>Nächste Schritte</mat-label>
          <textarea matInput formControlName="naechsteSchritte" rows="2" placeholder="Was ist als nächstes geplant?"></textarea>
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
  styles: [`.form { display: flex; flex-direction: column; gap: 10px; padding-top: 8px; min-width: 380px; } mat-form-field { width: 100%; } .row { display: grid; grid-template-columns: 1fr 1fr; gap: 12px; }`],
})
export class KontaktDialogComponent {
  private readonly api = inject(ApiService);
  private readonly snackBar = inject(MatSnackBar);
  private readonly dialogRef = inject(MatDialogRef<KontaktDialogComponent>);
  readonly data: KontaktDialogData = inject(MAT_DIALOG_DATA);
  private readonly fb = inject(FormBuilder);

  readonly saving = signal(false);
  readonly kanale = KANALE;
  readonly reaktionen = REAKTIONEN;
  readonly kanalLabels = KANAL_LABELS;
  readonly reaktionLabels = REAKTION_LABELS;

  readonly form = this.fb.nonNullable.group({
    datum: [this.data.eintrag ? new Date(this.data.eintrag.datum) : new Date(), Validators.required],
    kanal: [this.data.eintrag?.kanal ?? 'Email' as KontaktKanal, Validators.required],
    reaktion: [this.data.eintrag?.reaktion ?? 'Neutral' as KontaktReaktion, Validators.required],
    ansprechpartnerId: [this.data.eintrag?.ansprechpartnerId ?? null as string | null],
    zusammenfassung: [this.data.eintrag?.zusammenfassung ?? '', Validators.required],
    naechsteSchritte: [this.data.eintrag?.naechsteSchritte ?? ''],
  });

  save(): void {
    if (this.form.invalid) { this.form.markAllAsTouched(); return; }
    this.saving.set(true);
    const v = this.form.getRawValue();
    const body = {
      ansprechpartnerId: v.ansprechpartnerId || null,
      datum: (v.datum as Date).toISOString(),
      kanal: v.kanal,
      reaktion: v.reaktion,
      zusammenfassung: v.zusammenfassung.trim(),
      naechsteSchritte: v.naechsteSchritte.trim() || null,
    };
    const base = `/crm/partner/${this.data.partnerId}/kontakt`;
    const req$ = this.data.eintrag
      ? this.api.put<KontakteintragDto>(`${base}/${this.data.eintrag.id}`, body)
      : this.api.post<KontakteintragDto>(base, body);
    req$.subscribe({
      next: r => this.dialogRef.close(r),
      error: () => { this.saving.set(false); this.snackBar.open('Fehler beim Speichern.', 'OK', { duration: 3000 }); },
    });
  }
}

// ---- Detail Component -------------------------------------------------------

@Component({
  selector: 'app-crm-detail',
  imports: [
    RouterLink, MatButtonModule, MatIconModule, MatCardModule,
    MatProgressSpinnerModule, MatTooltipModule, MatChipsModule, MatDividerModule,
  ],
  templateUrl: './crm-detail.component.html',
  styleUrl: './crm-detail.component.scss',
})
export class CrmDetailComponent implements OnInit {
  private readonly api = inject(ApiService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly dialog = inject(MatDialog);
  private readonly snackBar = inject(MatSnackBar);

  readonly loading = signal(true);
  readonly partner = signal<PartnerDetail | null>(null);

  readonly statusLabels = PARTNER_STATUS_LABELS;
  readonly kanalIcons = KANAL_ICONS;
  readonly kanalLabels = KANAL_LABELS;
  readonly reaktionLabels = REAKTION_LABELS;

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id')!;
    this.api.get<PartnerDetail>(`/crm/partner/${id}`).subscribe({
      next: p => { this.partner.set(p); this.loading.set(false); },
      error: () => { this.loading.set(false); this.snackBar.open('Laden fehlgeschlagen.', 'OK', { duration: 3000 }); },
    });
  }

  openEditPartner(): void {
    const p = this.partner();
    if (!p) return;
    const overview: PartnerOverview = {
      id: p.id, name: p.name, kategorie: p.kategorie, status: p.status,
      website: p.website, anzahlKontakte: 0, letzterKontakt: null,
    };
    this.dialog.open(PartnerDialogComponent, { width: '460px', data: { partner: overview } })
      .afterClosed().subscribe((result: PartnerOverview | undefined) => {
        if (!result) return;
        this.partner.update(prev => prev ? { ...prev, ...result } : prev);
        this.snackBar.open('Gespeichert.', undefined, { duration: 2000 });
      });
  }

  openAddAp(): void {
    const p = this.partner();
    if (!p) return;
    this.dialog.open(ApDialogComponent, { width: '420px', data: { ap: null, partnerId: p.id } })
      .afterClosed().subscribe((result: AnsprechpartnerDto | undefined) => {
        if (!result) return;
        this.partner.update(prev => prev ? { ...prev, ansprechpartner: [...prev.ansprechpartner, result] } : prev);
      });
  }

  openEditAp(ap: AnsprechpartnerDto): void {
    const p = this.partner();
    if (!p) return;
    this.dialog.open(ApDialogComponent, { width: '420px', data: { ap, partnerId: p.id } })
      .afterClosed().subscribe((result: AnsprechpartnerDto | undefined) => {
        if (!result) return;
        this.partner.update(prev => prev ? {
          ...prev,
          ansprechpartner: prev.ansprechpartner.map(a => a.id === ap.id ? result : a),
        } : prev);
      });
  }

  deleteAp(ap: AnsprechpartnerDto): void {
    const p = this.partner();
    if (!p || !confirm(`„${ap.name}" löschen?`)) return;
    this.api.delete<void>(`/crm/partner/${p.id}/ansprechpartner/${ap.id}`).subscribe({
      next: () => this.partner.update(prev => prev ? {
        ...prev,
        ansprechpartner: prev.ansprechpartner.filter(a => a.id !== ap.id),
        kontakteintraege: prev.kontakteintraege.map(k =>
          k.ansprechpartnerId === ap.id ? { ...k, ansprechpartnerId: null, ansprechpartnerName: null } : k
        ),
      } : prev),
      error: () => this.snackBar.open('Löschen fehlgeschlagen.', 'OK', { duration: 3000 }),
    });
  }

  openAddKontakt(): void {
    const p = this.partner();
    if (!p) return;
    this.dialog.open(KontaktDialogComponent, {
      width: '480px',
      data: { eintrag: null, partnerId: p.id, ansprechpartner: p.ansprechpartner },
    }).afterClosed().subscribe((result: KontakteintragDto | undefined) => {
      if (!result) return;
      this.partner.update(prev => prev ? {
        ...prev,
        kontakteintraege: [result, ...prev.kontakteintraege],
      } : prev);
    });
  }

  openEditKontakt(eintrag: KontakteintragDto): void {
    const p = this.partner();
    if (!p) return;
    this.dialog.open(KontaktDialogComponent, {
      width: '480px',
      data: { eintrag, partnerId: p.id, ansprechpartner: p.ansprechpartner },
    }).afterClosed().subscribe((result: KontakteintragDto | undefined) => {
      if (!result) return;
      this.partner.update(prev => prev ? {
        ...prev,
        kontakteintraege: prev.kontakteintraege.map(k => k.id === eintrag.id ? result : k),
      } : prev);
    });
  }

  deleteKontakt(eintrag: KontakteintragDto): void {
    const p = this.partner();
    if (!p || !confirm('Kontakteintrag löschen?')) return;
    this.api.delete<void>(`/crm/partner/${p.id}/kontakt/${eintrag.id}`).subscribe({
      next: () => this.partner.update(prev => prev ? {
        ...prev,
        kontakteintraege: prev.kontakteintraege.filter(k => k.id !== eintrag.id),
      } : prev),
      error: () => this.snackBar.open('Löschen fehlgeschlagen.', 'OK', { duration: 3000 }),
    });
  }

  deletePartner(): void {
    const p = this.partner();
    if (!p || !confirm(`„${p.name}" wirklich löschen?`)) return;
    this.api.delete<void>(`/crm/partner/${p.id}`).subscribe({
      next: () => this.router.navigate(['/crm']),
      error: () => this.snackBar.open('Löschen fehlgeschlagen.', 'OK', { duration: 3000 }),
    });
  }

  formatDate(iso: string): string {
    const d = new Date(iso);
    return `${pad(d.getDate())}.${pad(d.getMonth() + 1)}.${d.getFullYear()}`;
  }

  statusClass(s: PartnerStatus): string {
    return `status-${s.toLowerCase()}`;
  }
}

const pad = (n: number) => n.toString().padStart(2, '0');
