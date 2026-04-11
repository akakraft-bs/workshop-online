import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatTableModule } from '@angular/material/table';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatChipsModule } from '@angular/material/chips';
import { MatSnackBar } from '@angular/material/snack-bar';
import { ApiService } from '../../core/api/api.service';
import { Verbrauchsmaterial } from '../../models/verbrauchsmaterial.model';

@Component({
  selector: 'app-verbrauchsmaterial-list',
  imports: [
    FormsModule,
    MatFormFieldModule, MatInputModule, MatIconModule,
    MatButtonModule, MatTableModule, MatProgressSpinnerModule, MatChipsModule,
  ],
  templateUrl: './verbrauchsmaterial-list.component.html',
  styleUrl: './verbrauchsmaterial-list.component.scss',
})
export class VerbrauchsmaterialListComponent implements OnInit {
  private readonly api = inject(ApiService);
  private readonly snackBar = inject(MatSnackBar);

  readonly items = signal<Verbrauchsmaterial[]>([]);
  readonly loading = signal(true);
  readonly searchQuery = signal('');

  readonly displayedColumns = ['name', 'category', 'quantity', 'status'];

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
