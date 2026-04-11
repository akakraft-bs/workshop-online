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
import { ApiService } from '../../core/api/api.service';
import { Werkzeug } from '../../models/werkzeug.model';

@Component({
  selector: 'app-werkzeug-list',
  imports: [
    FormsModule,
    MatFormFieldModule, MatInputModule, MatIconModule,
    MatButtonModule, MatCardModule, MatChipsModule, MatProgressSpinnerModule,
  ],
  templateUrl: './werkzeug-list.component.html',
  styleUrl: './werkzeug-list.component.scss',
})
export class WerkzeugListComponent implements OnInit {
  private readonly api = inject(ApiService);
  private readonly snackBar = inject(MatSnackBar);

  readonly items = signal<Werkzeug[]>([]);
  readonly loading = signal(true);
  readonly searchQuery = signal('');

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
    this.api.post(`/werkzeug/${item.id}/ausleihen`, {}).subscribe({
      next: () => {
        this.items.update(list =>
          list.map(w => w.id === item.id ? { ...w, isAvailable: false } : w)
        );
        this.snackBar.open(`${item.name} wurde ausgeliehen.`, 'OK', { duration: 3000 });
      },
      error: () =>
        this.snackBar.open('Ausleihe fehlgeschlagen.', 'OK', { duration: 3000 }),
    });
  }
}
