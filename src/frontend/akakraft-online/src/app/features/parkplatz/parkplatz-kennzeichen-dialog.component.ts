import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatDialogModule, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatTooltipModule } from '@angular/material/tooltip';
import { ParkplatzService } from '../../core/parkplatz/parkplatz.service';
import { ParkAccountStatus, ParkKennzeichenAudit, ParkKennzeichenListe } from '../../models/parkplatz.model';

interface DialogData {
  account: ParkAccountStatus;
  /** Kennzeichen, das nach dem Öffnen automatisch hinzugefügt werden soll (wenn ein Platz frei ist). */
  vorschlag?: string;
}

@Component({
  selector: 'app-parkplatz-kennzeichen-dialog',
  imports: [
    FormsModule, MatDialogModule, MatFormFieldModule, MatInputModule, MatButtonModule,
    MatProgressSpinnerModule, MatIconModule, MatTooltipModule,
  ],
  templateUrl: './parkplatz-kennzeichen-dialog.component.html',
  styleUrl: './parkplatz-kennzeichen-dialog.component.scss',
})
export class ParkplatzKennzeichenDialogComponent implements OnInit {
  private readonly svc = inject(ParkplatzService);
  private readonly snackBar = inject(MatSnackBar);
  readonly data: DialogData = inject(MAT_DIALOG_DATA);

  readonly liste = signal<ParkKennzeichenListe | null>(null);
  readonly loading = signal(true);
  readonly busy = signal(false);
  neuKennzeichen = '';

  readonly historieOpen = signal(false);
  readonly historieLoading = signal(false);
  readonly historie = signal<ParkKennzeichenAudit[]>([]);

  private vorschlagGeprueft = false;

  readonly voll = computed(() => {
    const l = this.liste();
    return !!l && l.kennzeichen.length >= l.max;
  });

  ngOnInit(): void {
    this.load();
  }

  private load(): void {
    this.loading.set(true);
    this.svc.getKennzeichen(this.data.account.id).subscribe({
      next: l => { this.liste.set(l); this.loading.set(false); this.verarbeiteVorschlag(); },
      error: () => { this.loading.set(false); this.snackBar.open('Kennzeichen konnten nicht geladen werden.', 'OK', { duration: 3000 }); },
    });
  }

  /** Nach dem ersten Laden: vorgeschlagenes Kennzeichen ins Feld setzen und – wenn Platz frei – gleich hinzufügen. */
  private verarbeiteVorschlag(): void {
    if (this.vorschlagGeprueft) return;
    this.vorschlagGeprueft = true;

    const norm = (s: string) => s.replace(/\s+/g, '').toUpperCase();
    const v = norm(this.data.vorschlag ?? '');
    const l = this.liste();
    if (!v || !l || !l.zugangKonfiguriert || l.fehler) return;
    if (l.kennzeichen.some(c => norm(c) === v)) return; // schon registriert

    this.neuKennzeichen = v;
    if (!this.voll()) this.add();
  }

  add(): void {
    const code = this.neuKennzeichen.trim();
    if (!code || this.busy() || this.voll()) return;
    this.busy.set(true);
    this.svc.addKennzeichen(this.data.account.id, code).subscribe({
      next: l => {
        this.liste.set(l);
        this.neuKennzeichen = '';
        this.busy.set(false);
        this.refreshHistorieIfOpen();
      },
      error: err => {
        this.busy.set(false);
        this.snackBar.open(typeof err?.error === 'string' ? err.error : 'Hinzufügen fehlgeschlagen.', 'OK', { duration: 4000 });
      },
    });
  }

  remove(code: string): void {
    if (this.busy()) return;
    if (!confirm(`Kennzeichen ${code} aus ${this.data.account.label} entfernen?`)) return;
    this.busy.set(true);
    this.svc.removeKennzeichen(this.data.account.id, code).subscribe({
      next: l => { this.liste.set(l); this.busy.set(false); this.refreshHistorieIfOpen(); },
      error: err => {
        this.busy.set(false);
        this.snackBar.open(typeof err?.error === 'string' ? err.error : 'Entfernen fehlgeschlagen.', 'OK', { duration: 4000 });
      },
    });
  }

  toggleHistorie(): void {
    const open = !this.historieOpen();
    this.historieOpen.set(open);
    if (open && this.historie().length === 0) this.ladeHistorie();
  }

  private refreshHistorieIfOpen(): void {
    if (this.historieOpen()) this.ladeHistorie();
  }

  private ladeHistorie(): void {
    this.historieLoading.set(true);
    this.svc.getKennzeichenHistorie(this.data.account.id).subscribe({
      next: h => { this.historie.set(h); this.historieLoading.set(false); },
      error: () => this.historieLoading.set(false),
    });
  }

  uhrzeit(iso: string): string {
    const d = new Date(iso);
    const pad = (n: number) => n.toString().padStart(2, '0');
    return `${pad(d.getDate())}.${pad(d.getMonth() + 1)}.${d.getFullYear()} ${pad(d.getHours())}:${pad(d.getMinutes())}`;
  }
}
