import { Component, computed, effect, ElementRef, inject, OnInit, signal, untracked, ViewChild } from '@angular/core';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatButtonToggleModule } from '@angular/material/button-toggle';
import { MatCardModule } from '@angular/material/card';
import { MatChipsModule } from '@angular/material/chips';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTableModule } from '@angular/material/table';
import { MatSelectModule } from '@angular/material/select';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatDialog } from '@angular/material/dialog';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { DatePipe } from '@angular/common';

type SortOrder = 'name' | 'createdAt' | 'category' | 'storageLocation';
import { ApiService } from '../../core/api/api.service';
import { AuthService } from '../../core/auth/auth.service';
import { Werkzeug } from '../../models/werkzeug.model';
import { WerkzeugFormDialogComponent, WerkzeugFormDialogData } from './werkzeug-form-dialog/werkzeug-form-dialog.component';
import { WerkzeugDetailDialogComponent, WerkzeugDetailResult } from './werkzeug-detail-dialog/werkzeug-detail-dialog.component';

@Component({
  selector: 'app-werkzeug-list',
  imports: [
    MatFormFieldModule, MatInputModule, MatIconModule,
    MatButtonModule, MatButtonToggleModule, MatCardModule, MatChipsModule,
    MatProgressSpinnerModule, MatTableModule, MatSelectModule, MatTooltipModule,
    MatPaginatorModule, DatePipe,
  ],
  templateUrl: './werkzeug-list.component.html',
  styleUrl: './werkzeug-list.component.scss',
})
export class WerkzeugListComponent implements OnInit {
  private readonly api      = inject(ApiService);
  private readonly auth     = inject(AuthService);
  private readonly snackBar = inject(MatSnackBar);
  private readonly dialog   = inject(MatDialog);

  readonly items            = signal<Werkzeug[]>([]);
  readonly storageLocations = signal<{ name: string; color?: string }[]>([]);
  readonly storageLocationColorMap = computed(() =>
    new Map(this.storageLocations().filter(l => l.color).map(l => [l.name, l.color!]))
  );
  readonly loading          = signal(true);
  readonly searchQuery      = signal('');
  readonly showOnlyBorrowed = signal(false);
  readonly selectedCategory = signal<string | null>(null);

  @ViewChild('mobileSearchInput') mobileSearchInput?: ElementRef<HTMLInputElement>;

  private readonly VIEW_MODE_KEY  = 'werkzeug-view-mode';
  private readonly SORT_ORDER_KEY = 'werkzeug-sort-order';
  private readonly PAGE_SIZE_KEY  = 'werkzeug-page-size';
  readonly mobilePanel = signal<'search' | 'sort' | null>(null);
  readonly viewMode  = signal<'card' | 'table'>(
    (localStorage.getItem(this.VIEW_MODE_KEY) as 'card' | 'table') ?? 'card'
  );
  readonly sortOrder = signal<SortOrder>(
    (localStorage.getItem(this.SORT_ORDER_KEY) as SortOrder) ?? 'name'
  );
  readonly tableColumns  = ['name', 'category', 'storageLocation', 'status', 'borrowedBy', 'expectedReturn'];

  readonly canManage    = computed(() => this.auth.isPrivileged());
  readonly currentUserId = computed(() => this.auth.currentUser()?.id ?? null);

  readonly categories = computed(() =>
    [...new Set(this.items().map(w => w.category))].sort()
  );

  readonly filteredItems = computed(() => {
    const q            = this.searchQuery().toLowerCase().trim();
    const onlyBorrowed = this.showOnlyBorrowed();
    const cat          = this.selectedCategory();
    const sort         = this.sortOrder();

    return this.items()
      .filter(w => {
        if (onlyBorrowed && w.isAvailable) return false;
        if (cat && w.category !== cat) return false;
        if (!q) return true;
        return (
          w.name.toLowerCase().includes(q) ||
          w.description.toLowerCase().includes(q) ||
          w.category.toLowerCase().includes(q)
        );
      })
      .sort((a, b) => {
        switch (sort) {
          case 'createdAt':     return new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime();
          case 'category':      return a.category.localeCompare(b.category, 'de') || a.name.localeCompare(b.name, 'de');
          case 'storageLocation': {
            const la = a.storageLocation ?? '';
            const lb = b.storageLocation ?? '';
            return la.localeCompare(lb, 'de') || a.name.localeCompare(b.name, 'de');
          }
          default:              return a.name.localeCompare(b.name, 'de');
        }
      });
  });

  readonly borrowedCount = computed(() => this.items().filter(w => !w.isAvailable).length);

  readonly pageIndex = signal(0);
  readonly pageSize  = signal<number>(Number(localStorage.getItem(this.PAGE_SIZE_KEY)) || 25);

  readonly pagedItems = computed(() => {
    const start = this.pageIndex() * this.pageSize();
    return this.filteredItems().slice(start, start + this.pageSize());
  });

  constructor() {
    effect(() => {
      this.searchQuery();
      this.selectedCategory();
      this.showOnlyBorrowed();
      this.sortOrder();
      untracked(() => this.pageIndex.set(0));
    });
  }

  onPage(e: PageEvent): void {
    this.pageSize.set(e.pageSize);
    this.pageIndex.set(e.pageIndex);
    localStorage.setItem(this.PAGE_SIZE_KEY, String(e.pageSize));
  }

  ngOnInit(): void { this.load(); }

  private load(): void {
    this.loading.set(true);
    this.api.get<Werkzeug[]>('/werkzeug').subscribe({
      next: data => { this.items.set(data); this.loading.set(false); },
      error: () => {
        this.loading.set(false);
        this.snackBar.open('Werkzeuge konnten nicht geladen werden.', 'OK', { duration: 3000 });
      },
    });
    this.api.get<{ name: string; color?: string }[]>('/storage-locations').subscribe({
      next: locs => this.storageLocations.set(locs),
    });
  }

  onSearch(event: Event): void {
    this.searchQuery.set((event.target as HTMLInputElement).value);
  }

  clearSearch(): void { this.searchQuery.set(''); }

  toggleBorrowedFilter(): void { this.showOnlyBorrowed.update(v => !v); }

  setViewMode(mode: 'card' | 'table'): void {
    this.viewMode.set(mode);
    localStorage.setItem(this.VIEW_MODE_KEY, mode);
  }

  setSortOrder(order: SortOrder): void {
    this.sortOrder.set(order);
    localStorage.setItem(this.SORT_ORDER_KEY, order);
    this.mobilePanel.set(null);
  }

  toggleMobilePanel(panel: 'search' | 'sort'): void {
    const next = this.mobilePanel() === panel ? null : panel;
    this.mobilePanel.set(next);
    if (next === 'search') {
      setTimeout(() => this.mobileSearchInput?.nativeElement.focus(), 50);
    }
  }

  closeMobilePanel(): void {
    this.mobilePanel.set(null);
  }

  isOverdue(item: Werkzeug): boolean {
    return !!item.expectedReturnAt && new Date(item.expectedReturnAt) < new Date();
  }

  openDetailDialog(item: Werkzeug): void {
    this.dialog
      .open(WerkzeugDetailDialogComponent, {
        data: item,
        width: '560px',
        maxWidth: '95vw',
        panelClass: 'werkzeug-detail-panel',
      })
      .afterClosed()
      .subscribe((result: WerkzeugDetailResult) => {
        if (!result) return;
        if (result.action === 'updated') {
          this.items.update(list => list.map(w => w.id === result.item.id ? result.item : w));
        } else if (result.action === 'deleted') {
          this.items.update(list => list.filter(w => w.id !== result.id));
        }
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
}
