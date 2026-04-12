import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatChipsModule } from '@angular/material/chips';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatDialog } from '@angular/material/dialog';
import { MatTooltipModule } from '@angular/material/tooltip';
import { ApiService } from '../../core/api/api.service';
import { AuthService } from '../../core/auth/auth.service';
import { Werkzeug } from '../../models/werkzeug.model';
import {
  WerkzeugFormDialogComponent,
  WerkzeugFormDialogData,
} from './werkzeug-form-dialog/werkzeug-form-dialog.component';

@Component({
  selector: 'app-werkzeug-list',
  imports: [
    FormsModule,
    MatFormFieldModule, MatInputModule, MatIconModule,
    MatButtonModule, MatCardModule, MatChipsModule,
    MatProgressSpinnerModule, MatTooltipModule,
  ],
  templateUrl: './werkzeug-list.component.html',
  styleUrl: './werkzeug-list.component.scss',
})
export class WerkzeugListComponent implements OnInit {
  private readonly api = inject(ApiService);
  private readonly auth = inject(AuthService);
  private readonly snackBar = inject(MatSnackBar);
  private readonly dialog = inject(MatDialog);

  readonly items = signal<Werkzeug[]>([]);
  readonly loading = signal(true);
  readonly searchQuery = signal('');
  readonly pendingDeleteId = signal<string | null>(null);

  readonly canManage = computed(() => this.auth.isAdmin() || this.auth.isVorstand());

  readonly filteredItems = computed(() => {
    const q = this.searchQuery().toLowerCase().trim();
    if (!q) return this.items();
    return this.items().filter(
      w =>
        w.name.toLowerCase().includes(q) ||
        w.description.toLowerCase().includes(q) ||
        w.category.toLowerCase().includes(q)
    );
  });

  ngOnInit(): void {
    this.load();
  }

  private load(): void {
    this.loading.set(true);
    this.api.get<Werkzeug[]>('/werkzeug').subscribe({
      next: data => {
        this.items.set(data);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.snackBar.open('Werkzeuge konnten nicht geladen werden.', 'OK', { duration: 3000 });
      },
    });
  }

  onSearch(event: Event): void {
    this.searchQuery.set((event.target as HTMLInputElement).value);
  }

  clearSearch(): void {
    this.searchQuery.set('');
  }

  borrow(item: Werkzeug): void {
    this.api.post<Werkzeug>(`/werkzeug/${item.id}/ausleihen`, {}).subscribe({
      next: updated => {
        this.items.update(list =>
          list.map(w => w.id === item.id ? updated : w)
        );
        this.snackBar.open(`${item.name} wurde ausgeliehen.`, 'OK', { duration: 3000 });
      },
      error: () =>
        this.snackBar.open('Ausleihe fehlgeschlagen.', 'OK', { duration: 3000 }),
    });
  }

  openCreateDialog(): void {
    const data: WerkzeugFormDialogData = { werkzeug: null };
    this.dialog
      .open(WerkzeugFormDialogComponent, { data, width: '480px' })
      .afterClosed()
      .subscribe((created: Werkzeug | undefined) => {
        if (created) {
          this.items.update(list => [created, ...list]);
          this.snackBar.open(`${created.name} wurde angelegt.`, 'OK', { duration: 3000 });
        }
      });
  }

  openEditDialog(item: Werkzeug): void {
    const data: WerkzeugFormDialogData = { werkzeug: item };
    this.dialog
      .open(WerkzeugFormDialogComponent, { data, width: '480px' })
      .afterClosed()
      .subscribe((updated: Werkzeug | undefined) => {
        if (updated) {
          this.items.update(list =>
            list.map(w => w.id === updated.id ? updated : w)
          );
          this.snackBar.open(`${updated.name} wurde gespeichert.`, 'OK', { duration: 3000 });
        }
      });
  }

  requestDelete(item: Werkzeug): void {
    this.pendingDeleteId.set(item.id);
  }

  cancelDelete(): void {
    this.pendingDeleteId.set(null);
  }

  confirmDelete(item: Werkzeug): void {
    this.pendingDeleteId.set(null);
    this.api.delete(`/werkzeug/${item.id}`).subscribe({
      next: () => {
        this.items.update(list => list.filter(w => w.id !== item.id));
        this.snackBar.open(`${item.name} wurde gelöscht.`, 'OK', { duration: 3000 });
      },
      error: () =>
        this.snackBar.open('Löschen fehlgeschlagen.', 'OK', { duration: 3000 }),
    });
  }
}
