import { Component, inject, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar } from '@angular/material/snack-bar';
import { RouterLink } from '@angular/router';
import { ApiService } from '../../../core/api/api.service';

interface BackfillResult {
  processedWerkzeug: number;
  processedVerbrauchsmaterial: number;
  errors: number;
  total: number;
}

interface TrimResult {
  trimmedWerkzeug: number;
  trimmedVerbrauchsmaterial: number;
}

@Component({
  selector: 'app-admin-panel',
  imports: [MatCardModule, MatButtonModule, MatIconModule, MatProgressSpinnerModule, RouterLink],
  templateUrl: './admin-panel.component.html',
  styleUrl: './admin-panel.component.scss',
})
export class AdminPanelComponent {
  private readonly api = inject(ApiService);
  private readonly snackBar = inject(MatSnackBar);

  readonly backfillRunning = signal(false);
  readonly backfillResult = signal<BackfillResult | null>(null);

  readonly trimRunning = signal(false);

  runBackfill(): void {
    if (this.backfillRunning()) return;
    this.backfillRunning.set(true);
    this.backfillResult.set(null);

    this.api.post<BackfillResult>('/admin/backfill-thumbnails', {}).subscribe({
      next: result => {
        this.backfillResult.set(result);
        this.backfillRunning.set(false);
        this.snackBar.open(
          `Fertig: ${result.total} Thumbnail(s) generiert.`,
          'OK',
          { duration: 4000 }
        );
      },
      error: () => {
        this.backfillRunning.set(false);
        this.snackBar.open('Fehler beim Generieren der Thumbnails.', 'OK', { duration: 4000 });
      },
    });
  }

  runTrim(): void {
    if (this.trimRunning()) return;
    this.trimRunning.set(true);

    this.api.post<TrimResult>('/admin/trim-text-fields', {}).subscribe({
      next: result => {
        this.trimRunning.set(false);
        const total = result.trimmedWerkzeug + result.trimmedVerbrauchsmaterial;
        this.snackBar.open(
          total > 0
            ? `Fertig: ${total} Eintrag/Einträge bereinigt (${result.trimmedWerkzeug} Werkzeug, ${result.trimmedVerbrauchsmaterial} Verbrauchsmaterial).`
            : 'Keine Leerzeichen gefunden – alle Einträge sind bereits bereinigt.',
          'OK',
          { duration: 5000 }
        );
      },
      error: () => {
        this.trimRunning.set(false);
        this.snackBar.open('Fehler beim Bereinigen.', 'OK', { duration: 4000 });
      },
    });
  }
}
