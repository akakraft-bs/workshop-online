import { Component, inject, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar } from '@angular/material/snack-bar';
import { ApiService } from '../../../core/api/api.service';

interface BackfillResult {
  processedWerkzeug: number;
  processedVerbrauchsmaterial: number;
  errors: number;
  total: number;
}

@Component({
  selector: 'app-admin-panel',
  imports: [MatCardModule, MatButtonModule, MatIconModule, MatProgressSpinnerModule],
  templateUrl: './admin-panel.component.html',
  styleUrl: './admin-panel.component.scss',
})
export class AdminPanelComponent {
  private readonly api = inject(ApiService);
  private readonly snackBar = inject(MatSnackBar);

  readonly backfillRunning = signal(false);
  readonly backfillResult = signal<BackfillResult | null>(null);

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
}
