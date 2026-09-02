import { Component, computed, inject, OnDestroy, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatDialog } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { AuthService } from '../../core/auth/auth.service';
import { ParkplatzService } from '../../core/parkplatz/parkplatz.service';
import { ParkAccountStatus, ParkClaim, ParkHistorieEintrag, ParkOverview } from '../../models/parkplatz.model';
import { ParkplatzCheckinDialogComponent } from './parkplatz-checkin-dialog.component';
import { ParkplatzKennzeichenDialogComponent } from './parkplatz-kennzeichen-dialog.component';

const AVATAR_COLORS = ['#6366f1', '#ec4899', '#f59e0b', '#10b981', '#3b82f6', '#8b5cf6', '#ef4444', '#0ea5e9'];

type HistorieZeile =
  | { key: string; kind: 'belegung'; belegung: ParkHistorieEintrag }
  | {
      key: string; kind: 'kennzeichen';
      accountLabel: string; displayName: string; zeitpunkt: string;
      hinzugefuegt: string[]; entfernt: string[];
    };

@Component({
  selector: 'app-parkplatz-page',
  imports: [
    FormsModule, MatButtonModule, MatIconModule, MatProgressSpinnerModule, MatTooltipModule,
  ],
  templateUrl: './parkplatz-page.component.html',
  styleUrl: './parkplatz-page.component.scss',
})
export class ParkplatzPageComponent implements OnInit, OnDestroy {
  private readonly api = inject(ParkplatzService);
  private readonly auth = inject(AuthService);
  private readonly dialog = inject(MatDialog);
  private readonly snackBar = inject(MatSnackBar);

  readonly overview = signal<ParkOverview | null>(null);
  readonly loading = signal(true);
  readonly busyAccountId = signal<string | null>(null);
  readonly registering = signal<string | null>(null);
  readonly editClaimId = signal<string | null>(null);
  editVoraussichtlichBis = '';

  readonly historieOpen = signal(false);
  readonly historieLoading = signal(false);
  readonly historie = signal<ParkHistorieEintrag[]>([]);

  /** Aufeinanderfolgende Kennzeichen-Änderungen desselben Nutzers am selben Konto (≤ 2 Min.) zu einer Zeile zusammenfassen. */
  readonly historieZeilen = computed<HistorieZeile[]>(() => {
    const zeilen: HistorieZeile[] = [];
    for (const h of this.historie()) {
      if (h.typ === 'Belegung') {
        zeilen.push({ key: h.id, kind: 'belegung', belegung: h });
        continue;
      }
      const last = zeilen[zeilen.length - 1];
      const nah = last?.kind === 'kennzeichen'
        && last.accountLabel === h.accountLabel
        && last.displayName === h.displayName
        && Math.abs(new Date(last.zeitpunkt).getTime() - new Date(h.zeitpunkt).getTime()) <= 120_000;
      const gruppe = nah
        ? last as Extract<HistorieZeile, { kind: 'kennzeichen' }>
        : (() => {
            const z: HistorieZeile = {
              key: h.id, kind: 'kennzeichen', accountLabel: h.accountLabel,
              displayName: h.displayName, zeitpunkt: h.zeitpunkt, hinzugefuegt: [], entfernt: [],
            };
            zeilen.push(z);
            return z;
          })();
      (h.typ === 'KennzeichenHinzugefuegt' ? gruppe.hinzugefuegt : gruppe.entfernt).push(h.kennzeichen ?? '');
    }
    return zeilen;
  });

  private intervalId?: ReturnType<typeof setInterval>;
  private readonly now = signal(Date.now());

  readonly currentUserId = computed(() => this.auth.currentUser()?.id ?? null);
  readonly isPrivileged = computed(() => this.auth.isPrivileged());

  readonly meinClaim = computed<ParkClaim | null>(() => {
    const uid = this.currentUserId();
    for (const acc of this.overview()?.accounts ?? []) {
      if (acc.belegung && acc.belegung.userId === uid) return acc.belegung;
    }
    return null;
  });

  ngOnInit(): void {
    this.load();
    this.intervalId = setInterval(() => { this.now.set(Date.now()); this.load(true); }, 60_000);
  }

  ngOnDestroy(): void {
    clearInterval(this.intervalId);
  }

  private load(silent = false): void {
    if (!silent) this.loading.set(true);
    this.api.getOverview().subscribe({
      next: data => { this.overview.set(data); this.loading.set(false); },
      error: () => {
        this.loading.set(false);
        if (!silent) this.snackBar.open('Parkplatz-Übersicht konnte nicht geladen werden.', 'OK', { duration: 3000 });
      },
    });
  }

  openCheckin(account: ParkAccountStatus): void {
    const ov = this.overview();
    if (!ov) return;
    this.dialog.open(ParkplatzCheckinDialogComponent, {
      width: '460px',
      maxWidth: '95vw',
      data: { account, berechtigung: ov.berechtigung },
    }).afterClosed().subscribe((claim: ParkClaim | undefined) => {
      if (claim) {
        this.snackBar.open(`${account.label} übernommen. Jetzt Kennzeichen im Uni-Portal eintragen.`, 'OK', { duration: 5000 });
        this.load(true);
        if (this.historieOpen()) this.ladeHistorie();
      }
    });
  }

  freigeben(claim: ParkClaim, account: ParkAccountStatus): void {
    if (!confirm(`${account.label} freigeben? Bitte nur, wenn dein Fahrzeug den Campus verlassen hat.`)) return;
    this.busyAccountId.set(account.id);
    this.api.freigeben(claim.id).subscribe({
      next: () => {
        this.busyAccountId.set(null);
        this.load(true);
        if (this.historieOpen()) this.ladeHistorie();
      },
      error: () => { this.busyAccountId.set(null); this.snackBar.open('Freigabe fehlgeschlagen.', 'OK', { duration: 3000 }); },
    });
  }

  toggleHistorie(): void {
    const open = !this.historieOpen();
    this.historieOpen.set(open);
    if (open && this.historie().length === 0) this.ladeHistorie();
  }

  ladeHistorie(): void {
    this.historieLoading.set(true);
    this.api.getHistorie(100).subscribe({
      next: list => { this.historie.set(list); this.historieLoading.set(false); },
      error: () => {
        this.historieLoading.set(false);
        this.snackBar.open('Historie konnte nicht geladen werden.', 'OK', { duration: 3000 });
      },
    });
  }

  openKennzeichen(account: ParkAccountStatus, vorschlag?: string): void {
    this.dialog.open(ParkplatzKennzeichenDialogComponent, {
      width: '460px', maxWidth: '95vw', data: { account, vorschlag },
    }).afterClosed().subscribe(() => this.load(true));
  }

  /** "Jetzt registrieren": bei freiem Platz direkt hinzufügen, sonst Dialog zum Aufräumen. */
  registriereKennzeichen(account: ParkAccountStatus, claim: ParkClaim): void {
    if (account.kennzeichen.length >= 5) {
      this.openKennzeichen(account, claim.kennzeichen);
      return;
    }
    this.registering.set(account.id);
    this.api.addKennzeichen(account.id, claim.kennzeichen).subscribe({
      next: () => {
        this.registering.set(null);
        this.snackBar.open(`${claim.kennzeichen} auf ${account.label} registriert.`, undefined, { duration: 3000 });
        this.load(true);
      },
      error: err => {
        this.registering.set(null);
        this.snackBar.open(
          typeof err?.error === 'string' ? err.error : 'Registrieren fehlgeschlagen.', 'OK', { duration: 4000 });
      },
    });
  }

  /** true, wenn das Kennzeichen der Belegung (noch) nicht im Portal des Kontos registriert ist. */
  fehltKennzeichen(account: ParkAccountStatus, claim: ParkClaim): boolean {
    if (!account.zugangKonfiguriert || account.kennzeichenFehler) return false;
    const norm = (s: string) => s.replace(/\s+/g, '').toUpperCase();
    return !account.kennzeichen.some(c => norm(c) === norm(claim.kennzeichen));
  }

  problemMelden(account: ParkAccountStatus): void {
    if (!confirm(`Problem mit ${account.label} an Hallenwart/Vorstand melden?`)) return;
    this.api.problemMelden(account.id).subscribe({
      next: () => this.snackBar.open('Meldung wurde weitergegeben. Danke!', undefined, { duration: 3000 }),
      error: () => this.snackBar.open('Meldung fehlgeschlagen.', 'OK', { duration: 3000 }),
    });
  }

  startEdit(claim: ParkClaim): void {
    this.editClaimId.set(claim.id);
    this.editVoraussichtlichBis = claim.voraussichtlichBis
      ? this.toLocalInput(new Date(claim.voraussichtlichBis))
      : '';
  }

  cancelEdit(): void {
    this.editClaimId.set(null);
  }

  saveEdit(claim: ParkClaim): void {
    const iso = this.editVoraussichtlichBis ? new Date(this.editVoraussichtlichBis).toISOString() : null;
    this.api.updateClaim(claim.id, iso).subscribe({
      next: () => { this.editClaimId.set(null); this.load(true); },
      error: () => this.snackBar.open('Speichern fehlgeschlagen.', 'OK', { duration: 3000 }),
    });
  }

  // ---- Anzeige-Helfer ----

  restzeit(claim: ParkClaim): string {
    const ms = new Date(claim.autoExpiresAt).getTime() - this.now();
    if (ms <= 0) return 'abgelaufen';
    const h = Math.floor(ms / 3_600_000);
    const m = Math.floor((ms % 3_600_000) / 60_000);
    return h > 0 ? `noch ${h} Std. ${m} Min.` : `noch ${m} Min.`;
  }

  laeuftBald(claim: ParkClaim): boolean {
    return new Date(claim.autoExpiresAt).getTime() - this.now() <= 2 * 3_600_000;
  }

  seit(dateStr: string): string {
    const mins = Math.floor((this.now() - new Date(dateStr).getTime()) / 60_000);
    if (mins < 1) return 'gerade eben';
    if (mins < 60) return `seit ${mins} Min.`;
    const h = Math.floor(mins / 60);
    return `seit ${h} Std. ${mins % 60} Min.`;
  }

  uhrzeit(dateStr: string): string {
    const d = new Date(dateStr);
    const pad = (n: number) => n.toString().padStart(2, '0');
    return `${pad(d.getDate())}.${pad(d.getMonth() + 1)}. ${pad(d.getHours())}:${pad(d.getMinutes())}`;
  }

  /** z. B. "Max hat Kennzeichen AB-CD1234 hinzugefügt und DC-BA4321 entfernt" */
  satzKennzeichen(z: Extract<HistorieZeile, { kind: 'kennzeichen' }>): string {
    const teile: string[] = [];
    if (z.hinzugefuegt.length) teile.push(`Kennzeichen ${z.hinzugefuegt.join(', ')} hinzugefügt`);
    if (z.entfernt.length) teile.push(`${z.hinzugefuegt.length ? '' : 'Kennzeichen '}${z.entfernt.join(', ')} entfernt`);
    return `${z.displayName} hat ${teile.join(' und ')}`;
  }

  initials(name: string): string {
    return name.split(' ').map(n => n[0] ?? '').join('').toUpperCase().slice(0, 2);
  }

  avatarColor(name: string): string {
    let h = 0;
    for (let i = 0; i < name.length; i++) h = name.charCodeAt(i) + ((h << 5) - h);
    return AVATAR_COLORS[Math.abs(h) % AVATAR_COLORS.length];
  }

  private toLocalInput(d: Date): string {
    const pad = (n: number) => n.toString().padStart(2, '0');
    return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}T${pad(d.getHours())}:${pad(d.getMinutes())}`;
  }
}
