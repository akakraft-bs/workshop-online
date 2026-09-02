import { Component, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatDialogModule, MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { MatSnackBar } from '@angular/material/snack-bar';
import { FahrzeugService } from '../../core/fahrzeug/fahrzeug.service';
import { ParkplatzService } from '../../core/parkplatz/parkplatz.service';
import { Fahrzeug } from '../../models/fahrzeug.model';
import { PARKPORTAL_URL, ParkAccountStatus, ParkBerechtigung, ParkClaim } from '../../models/parkplatz.model';

export interface ParkCheckinDialogData {
  account: ParkAccountStatus;
  berechtigung: ParkBerechtigung;
}

const MANUELL = '__manuell__';

function toLocalInputValue(d: Date): string {
  const pad = (n: number) => n.toString().padStart(2, '0');
  return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}T${pad(d.getHours())}:${pad(d.getMinutes())}`;
}

@Component({
  selector: 'app-parkplatz-checkin-dialog',
  imports: [
    FormsModule, MatDialogModule, MatFormFieldModule, MatInputModule, MatSelectModule,
    MatCheckboxModule, MatButtonModule, MatProgressSpinnerModule, MatIconModule,
  ],
  templateUrl: './parkplatz-checkin-dialog.component.html',
  styleUrl: './parkplatz-checkin-dialog.component.scss',
})
export class ParkplatzCheckinDialogComponent implements OnInit {
  private readonly fahrzeugService = inject(FahrzeugService);
  private readonly parkplatzService = inject(ParkplatzService);
  private readonly snackBar = inject(MatSnackBar);
  private readonly dialogRef = inject(MatDialogRef<ParkplatzCheckinDialogComponent>);
  readonly data: ParkCheckinDialogData = inject(MAT_DIALOG_DATA);

  readonly MANUELL = MANUELL;
  readonly portalUrl = this.data.account.portalUrl || PARKPORTAL_URL;
  readonly fahrzeuge = signal<Fahrzeug[]>([]);
  readonly loadingFahrzeuge = signal(true);
  readonly saving = signal(false);

  fahrzeugId = '';
  manuellKennzeichen = '';
  manuellBezeichnung = '';
  einfahrt = toLocalInputValue(new Date());
  voraussichtlichBis = '';
  bestaetigt = false;
  bookingEventId = '';

  // Plain-Getter statt computed(): fahrzeugId & Co. sind ngModel-Felder (keine Signals),
  // ein computed würde nicht neu auswerten.
  istManuell(): boolean {
    return this.fahrzeugId === MANUELL;
  }

  disabledGrund(): string | null {
    if (this.saving() || this.loadingFahrzeuge()) return null;
    if (this.fahrzeugId === MANUELL) {
      if (this.manuellKennzeichen.trim().length === 0) return 'Bitte ein Kennzeichen eingeben.';
    } else if (this.fahrzeugId.length === 0) {
      return 'Bitte ein Fahrzeug wählen.';
    }
    if (this.data.berechtigung.erfordertBestaetigung && !this.bestaetigt) {
      return 'Bitte oben die Nutzungsberechtigung bestätigen.';
    }
    return null;
  }

  kannSpeichern(): boolean {
    return !this.saving() && this.disabledGrund() === null;
  }

  ngOnInit(): void {
    this.fahrzeugService.list().subscribe({
      next: list => {
        this.fahrzeuge.set(list);
        const vorschlag = this.data.berechtigung.vorgeschlagenesFahrzeugId;
        this.fahrzeugId = (vorschlag && list.some(f => f.id === vorschlag))
          ? vorschlag
          : (list.find(f => f.istStandard)?.id ?? list[0]?.id ?? MANUELL);
        this.loadingFahrzeuge.set(false);
      },
      error: () => { this.fahrzeugId = MANUELL; this.loadingFahrzeuge.set(false); },
    });
  }

  /** Wenn eine Bühne-Reservierung ausgewählt wird, deren hinterlegtes Fahrzeug vorauswählen. */
  onReservierungChange(): void {
    const r = this.data.berechtigung.waehlbareReservierungen.find(x => x.eventId === this.bookingEventId);
    if (r?.fahrzeugId && this.fahrzeuge().some(f => f.id === r.fahrzeugId)) {
      this.fahrzeugId = r.fahrzeugId;
    }
  }

  get fahrzeugAusReservierung(): boolean {
    const v = this.data.berechtigung.vorgeschlagenesFahrzeugId;
    return !!v && this.fahrzeugId === v;
  }

  private toIso(local: string): string | null {
    if (!local) return null;
    const d = new Date(local);
    return isNaN(d.getTime()) ? null : d.toISOString();
  }

  submit(): void {
    if (!this.kannSpeichern()) return;
    this.saving.set(true);

    const manuell = this.fahrzeugId === MANUELL;
    this.parkplatzService.checkin({
      parkAccountId: this.data.account.id,
      fahrzeugId: manuell ? null : this.fahrzeugId,
      kennzeichen: manuell ? this.manuellKennzeichen.trim().toUpperCase() : null,
      fahrzeugBezeichnung: manuell ? (this.manuellBezeichnung.trim() || null) : null,
      einfahrtAt: this.toIso(this.einfahrt),
      voraussichtlichBis: this.toIso(this.voraussichtlichBis),
      bestaetigungAkzeptiert: this.bestaetigt || !this.data.berechtigung.erfordertBestaetigung,
      bookingEventId: this.bookingEventId || null,
    }).subscribe({
      next: (claim: ParkClaim) => this.dialogRef.close(claim),
      error: err => {
        this.saving.set(false);
        this.snackBar.open(typeof err?.error === 'string' ? err.error : 'Check-in fehlgeschlagen.', 'OK', { duration: 4000 });
      },
    });
  }

  formatReservierung(start: string | null, titel: string): string {
    if (!start) return titel;
    const d = new Date(start);
    const pad = (n: number) => n.toString().padStart(2, '0');
    return `${pad(d.getDate())}.${pad(d.getMonth() + 1)}. ${pad(d.getHours())}:${pad(d.getMinutes())} – ${titel}`;
  }
}
