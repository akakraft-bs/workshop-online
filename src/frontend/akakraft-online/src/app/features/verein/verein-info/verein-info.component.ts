import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatDialog } from '@angular/material/dialog';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatDividerModule } from '@angular/material/divider';
import { ApiService } from '../../../core/api/api.service';
import { AuthService } from '../../../core/auth/auth.service';
import { AmtsTraeger, Schluesselhinterlegung, VereinInfo } from '../../../models/verein.models';
import { SchluesselhinterlegungDialogComponent } from './schluesselhinterlegung-dialog.component';

@Component({
  selector: 'app-verein-info',
  imports: [
    MatButtonModule, MatIconModule, MatProgressSpinnerModule,
    MatTooltipModule, MatDividerModule,
  ],
  templateUrl: './verein-info.component.html',
  styleUrl: './verein-info.component.scss',
})
export class VereinInfoComponent implements OnInit {
  private readonly api      = inject(ApiService);
  private readonly auth     = inject(AuthService);
  private readonly snackBar = inject(MatSnackBar);
  private readonly dialog   = inject(MatDialog);

  readonly loading      = signal(true);
  readonly amtstraeger  = signal<AmtsTraeger[]>([]);
  readonly schluessel   = signal<Schluesselhinterlegung[]>([]);
  readonly canEdit      = computed(() => this.auth.isAdmin() || this.auth.isVorstand());

  readonly amtstraegerGroups = computed(() => {
    const groups = new Map<string, { label: string; entries: AmtsTraeger[] }>();
    for (const entry of this.amtstraeger()) {
      if (!groups.has(entry.role)) groups.set(entry.role, { label: entry.roleLabel, entries: [] });
      groups.get(entry.role)!.entries.push(entry);
    }
    return [...groups.values()];
  });

  ngOnInit(): void { this.load(); }

  private load(): void {
    this.api.get<VereinInfo>('/verein/info').subscribe({
      next: data => {
        this.amtstraeger.set(data.amtstraeger);
        this.schluessel.set(data.schluessel);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.snackBar.open('Daten konnten nicht geladen werden.', 'OK', { duration: 3000 });
      },
    });
  }

  openCreateSchluessel(): void {
    this.dialog
      .open(SchluesselhinterlegungDialogComponent, { data: null, width: '440px' })
      .afterClosed()
      .subscribe((created: Schluesselhinterlegung | undefined) => {
        if (created) this.schluessel.update(list => [...list, created]);
      });
  }

  openEditSchluessel(entry: Schluesselhinterlegung): void {
    this.dialog
      .open(SchluesselhinterlegungDialogComponent, { data: entry, width: '440px' })
      .afterClosed()
      .subscribe((updated: Schluesselhinterlegung | undefined) => {
        if (updated) this.schluessel.update(list => list.map(s => s.id === updated.id ? updated : s));
      });
  }

  deleteSchluessel(entry: Schluesselhinterlegung): void {
    this.api.delete(`/verein/info/schluessel/${entry.id}`).subscribe({
      next: () => this.schluessel.update(list => list.filter(s => s.id !== entry.id)),
      error: () => this.snackBar.open('Löschen fehlgeschlagen.', 'OK', { duration: 3000 }),
    });
  }
}
