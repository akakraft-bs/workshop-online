import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatTableModule } from '@angular/material/table';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatChipsModule } from '@angular/material/chips';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatDialog } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { ApiService } from '../../core/api/api.service';
import { AuthService } from '../../core/auth/auth.service';
import { Verbrauchsmaterial } from '../../models/verbrauchsmaterial.model';
import { VerbrauchsmaterialFormDialogComponent, VerbrauchsmaterialFormDialogData } from './verbrauchsmaterial-form-dialog/verbrauchsmaterial-form-dialog.component';

@Component({
  selector: 'app-verbrauchsmaterial-list',
  imports: [
    FormsModule,
    MatFormFieldModule, MatInputModule, MatIconModule,
    MatButtonModule, MatTableModule, MatProgressSpinnerModule,
    MatChipsModule, MatTooltipModule,
  ],
  templateUrl: './verbrauchsmaterial-list.component.html',
  styleUrl: './verbrauchsmaterial-list.component.scss',
})
export class VerbrauchsmaterialListComponent implements OnInit {
  private readonly api = inject(ApiService);
  private readonly auth = inject(AuthService);
  private readonly dialog = inject(MatDialog);
  private readonly snackBar = inject(MatSnackBar);

  readonly items = signal<Verbrauchsmaterial[]>([]);
  readonly loading = signal(true);
  readonly searchQuery = signal('');

  readonly canManage = computed(() => this.auth.isVorstand() || this.auth.isAdmin());

  readonly displayedColumns = computed(() =>
    this.canManage()
      ? ['image', 'name', 'category', 'quantity', 'status', 'actions']
      : ['image', 'name', 'category', 'quantity', 'status']
  );

  readonly filteredItems = computed(() => {
    const q = this.searchQuery().toLowerCase().trim();
    if (!q) return this.items();
    return this.items().filter(
      v =>
        v.name.toLowerCase().includes(q) ||
        v.category.toLowerCase().includes(q)
    );
  });

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.api.get<Verbrauchsmaterial[]>('/verbrauchsmaterial').subscribe({
      next: data => {
        this.items.set(data);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.snackBar.open('Verbrauchsmaterialien konnten nicht geladen werden.', 'OK', { duration: 3000 });
      },
    });
  }

  openAddDialog(): void {
    this.openDialog(null);
  }

  openEditDialog(item: Verbrauchsmaterial): void {
    this.openDialog(item);
  }

  private openDialog(item: Verbrauchsmaterial | null): void {
    const ref = this.dialog.open<VerbrauchsmaterialFormDialogComponent, VerbrauchsmaterialFormDialogData, Verbrauchsmaterial>(
      VerbrauchsmaterialFormDialogComponent,
      { width: '560px', maxWidth: '95vw', data: { item } }
    );

    ref.afterClosed().subscribe(result => {
      if (!result) return;
      if (item) {
        this.items.update(list => list.map(v => v.id === result.id ? result : v));
        this.snackBar.open(`„${result.name}" gespeichert.`, undefined, { duration: 3000 });
      } else {
        this.items.update(list => [...list, result].sort((a, b) =>
          a.category.localeCompare(b.category) || a.name.localeCompare(b.name)
        ));
        this.snackBar.open(`„${result.name}" hinzugefügt.`, undefined, { duration: 3000 });
      }
    });
  }

  delete(item: Verbrauchsmaterial): void {
    if (!confirm(`„${item.name}" wirklich löschen?`)) return;

    this.api.delete<void>(`/verbrauchsmaterial/${item.id}`).subscribe({
      next: () => {
        this.items.update(list => list.filter(v => v.id !== item.id));
        this.snackBar.open(`„${item.name}" gelöscht.`, undefined, { duration: 3000 });
      },
      error: () => {
        this.snackBar.open('Löschen fehlgeschlagen.', 'OK', { duration: 3000 });
      },
    });
  }

  onSearch(event: Event): void {
    this.searchQuery.set((event.target as HTMLInputElement).value);
  }

  clearSearch(): void {
    this.searchQuery.set('');
  }

  isLowStock(item: Verbrauchsmaterial): boolean {
    return item.minQuantity != null && item.quantity <= item.minQuantity;
  }
}
