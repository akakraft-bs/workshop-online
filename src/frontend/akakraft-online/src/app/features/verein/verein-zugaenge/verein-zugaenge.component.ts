import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatDialog } from '@angular/material/dialog';
import { MatTooltipModule } from '@angular/material/tooltip';
import { ApiService } from '../../../core/api/api.service';
import { AuthService } from '../../../core/auth/auth.service';
import { VereinZugang } from '../../../models/verein.models';
import { ZugangDialogComponent } from './zugang-dialog.component';

@Component({
  selector: 'app-verein-zugaenge',
  imports: [MatButtonModule, MatIconModule, MatProgressSpinnerModule, MatTooltipModule],
  templateUrl: './verein-zugaenge.component.html',
  styleUrl: './verein-zugaenge.component.scss',
})
export class VereinZugaengeComponent implements OnInit {
  private readonly api      = inject(ApiService);
  private readonly auth     = inject(AuthService);
  private readonly snackBar = inject(MatSnackBar);
  private readonly dialog   = inject(MatDialog);

  readonly loading  = signal(true);
  readonly zugaenge = signal<VereinZugang[]>([]);
  readonly canEdit  = computed(() => this.auth.isAdmin() || this.auth.isVorstand());

  ngOnInit(): void { this.load(); }

  private load(): void {
    this.api.get<VereinZugang[]>('/verein/zugaenge').subscribe({
      next: data => { this.zugaenge.set(data); this.loading.set(false); },
      error: () => {
        this.loading.set(false);
        this.snackBar.open('Daten konnten nicht geladen werden.', 'OK', { duration: 3000 });
      },
    });
  }

  openCreate(): void {
    this.dialog
      .open(ZugangDialogComponent, { data: null, width: '520px' })
      .afterClosed()
      .subscribe((created: VereinZugang | undefined) => {
        if (created) this.zugaenge.update(list => [...list, created].sort((a, b) => a.anbieter.localeCompare(b.anbieter)));
      });
  }

  openEdit(entry: VereinZugang): void {
    this.dialog
      .open(ZugangDialogComponent, { data: entry, width: '520px' })
      .afterClosed()
      .subscribe((updated: VereinZugang | undefined) => {
        if (updated) this.zugaenge.update(list => list.map(z => z.id === updated.id ? updated : z));
      });
  }

  delete(entry: VereinZugang): void {
    this.api.delete(`/verein/zugaenge/${entry.id}`).subscribe({
      next: () => this.zugaenge.update(list => list.filter(z => z.id !== entry.id)),
      error: () => this.snackBar.open('Löschen fehlgeschlagen.', 'OK', { duration: 3000 }),
    });
  }
}
