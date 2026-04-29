import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';
import { MatButtonToggleModule } from '@angular/material/button-toggle';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatDialog } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { ApiService } from '../../core/api/api.service';
import { AuthService } from '../../core/auth/auth.service';
import { Aufgabe, AufgabeStatus } from '../../models/aufgabe.model';
import { AufgabeDialogComponent } from './aufgabe-dialog/aufgabe-dialog.component';

type StatusFilter = 'offen' | 'erledigt' | 'alle';

@Component({
  selector: 'app-aufgaben-list',
  imports: [
    DatePipe,
    MatButtonModule, MatButtonToggleModule,
    MatIconModule, MatProgressSpinnerModule, MatTooltipModule,
  ],
  templateUrl: './aufgaben-list.component.html',
  styleUrl: './aufgaben-list.component.scss',
})
export class AufgabenListComponent implements OnInit {
  private readonly api      = inject(ApiService);
  private readonly auth     = inject(AuthService);
  private readonly dialog   = inject(MatDialog);
  private readonly snackBar = inject(MatSnackBar);

  readonly loading      = signal(true);
  readonly items        = signal<Aufgabe[]>([]);
  readonly statusFilter = signal<StatusFilter>('offen');
  readonly canEdit      = computed(() => this.auth.isAdmin() || this.auth.isVorstand());

  readonly offenCount = computed(() =>
    this.items().filter(a => a.status !== 'Erledigt').length
  );

  readonly filtered = computed(() => {
    const f = this.statusFilter();
    return this.items().filter(a =>
      f === 'offen'    ? a.status !== 'Erledigt' :
      f === 'erledigt' ? a.status === 'Erledigt' : true
    );
  });

  ngOnInit(): void { this.load(); }

  private load(): void {
    this.api.get<Aufgabe[]>('/aufgaben').subscribe({
      next: data => { this.items.set(data); this.loading.set(false); },
      error: () => {
        this.loading.set(false);
        this.snackBar.open('Aufgaben konnten nicht geladen werden.', 'OK', { duration: 3000 });
      },
    });
  }

  openCreate(): void {
    this.dialog.open(AufgabeDialogComponent, { data: null, width: '540px' })
      .afterClosed()
      .subscribe((created: Aufgabe | undefined) => {
        if (created) this.items.update(list => [created, ...list]);
      });
  }

  openEdit(aufgabe: Aufgabe): void {
    this.dialog.open(AufgabeDialogComponent, { data: aufgabe, width: '540px' })
      .afterClosed()
      .subscribe((updated: Aufgabe | undefined) => {
        if (updated) this.items.update(list => list.map(a => a.id === updated.id ? updated : a));
      });
  }

  delete(aufgabe: Aufgabe): void {
    this.api.delete(`/aufgaben/${aufgabe.id}`).subscribe({
      next: () => this.items.update(list => list.filter(a => a.id !== aufgabe.id)),
      error: () => this.snackBar.open('Löschen fehlgeschlagen.', 'OK', { duration: 3000 }),
    });
  }

  statusIcon(status: AufgabeStatus): string {
    return status === 'Neu' ? 'radio_button_unchecked' :
           status === 'Zugewiesen' ? 'person' : 'check_circle';
  }

  assignedLabel(a: Aufgabe): string | null {
    if (a.assignedDisplayName) return a.assignedDisplayName;
    if (a.assignedName)        return a.assignedName;
    return null;
  }
}
