import { Component, computed, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';
import { MatChipsModule } from '@angular/material/chips';
import { MatDialog, MatDialogModule, MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatDividerModule } from '@angular/material/divider';
import { MatTooltipModule } from '@angular/material/tooltip';
import { ApiService } from '../../../core/api/api.service';
import { AuthService } from '../../../core/auth/auth.service';
import { Werkzeug } from '../../../models/werkzeug.model';
import { AusleihenDialogComponent, AusleihenDialogData } from '../ausleihen-dialog/ausleihen-dialog.component';
import { WerkzeugFormDialogComponent, WerkzeugFormDialogData } from '../werkzeug-form-dialog/werkzeug-form-dialog.component';

export type WerkzeugDetailResult =
  | { action: 'updated'; item: Werkzeug }
  | { action: 'deleted'; id: string }
  | undefined;

@Component({
  selector: 'app-werkzeug-detail-dialog',
  imports: [
    DatePipe,
    MatDialogModule, MatButtonModule, MatIconModule, MatTooltipModule,
    MatChipsModule, MatProgressSpinnerModule, MatDividerModule,
  ],
  templateUrl: './werkzeug-detail-dialog.component.html',
  styleUrl: './werkzeug-detail-dialog.component.scss',
})
export class WerkzeugDetailDialogComponent {
  private readonly api    = inject(ApiService);
  private readonly auth   = inject(AuthService);
  private readonly dialog = inject(MatDialog);
  private readonly snackBar = inject(MatSnackBar);
  readonly dialogRef = inject(MatDialogRef<WerkzeugDetailDialogComponent>);

  readonly item             = signal<Werkzeug>(inject(MAT_DIALOG_DATA));
  readonly returning        = signal(false);
  readonly showDeleteConfirm = signal(false);
  readonly deleting         = signal(false);

  readonly canManage    = computed(() => this.auth.isAdmin() || this.auth.isVorstand());
  readonly currentUserId = computed(() => this.auth.currentUser()?.id ?? null);
  readonly isMyBorrow   = computed(() =>
    !!this.currentUserId() && this.item().borrowedByUserId === this.currentUserId()
  );
  readonly canReturn = computed(() =>
    !this.item().isAvailable && (this.isMyBorrow() || this.canManage())
  );
  readonly isOverdue = computed(() => {
    const ret = this.item().expectedReturnAt;
    return ret ? new Date(ret) < new Date() : false;
  });

  borrow(): void {
    const data: AusleihenDialogData = { werkzeug: this.item() };
    this.dialog
      .open(AusleihenDialogComponent, { data, width: '400px' })
      .afterClosed()
      .subscribe((updated: Werkzeug | undefined) => {
        if (updated) this.dialogRef.close({ action: 'updated', item: updated });
      });
  }

  returnItem(): void {
    this.returning.set(true);
    this.api.post<Werkzeug>(`/werkzeug/${this.item().id}/zurueckgeben`, {}).subscribe({
      next: updated => {
        this.snackBar.open(`${this.item().name} zurückgegeben.`, 'OK', { duration: 3000 });
        this.dialogRef.close({ action: 'updated', item: updated });
      },
      error: () => {
        this.snackBar.open('Rückgabe fehlgeschlagen.', 'OK', { duration: 3000 });
        this.returning.set(false);
      },
    });
  }

  edit(): void {
    const data: WerkzeugFormDialogData = { werkzeug: this.item() };
    this.dialog
      .open(WerkzeugFormDialogComponent, { data, width: '480px' })
      .afterClosed()
      .subscribe((updated: Werkzeug | undefined) => {
        if (updated) this.dialogRef.close({ action: 'updated', item: updated });
      });
  }

  delete(): void {
    this.deleting.set(true);
    this.api.delete(`/werkzeug/${this.item().id}`).subscribe({
      next: () => {
        this.snackBar.open(`${this.item().name} gelöscht.`, 'OK', { duration: 3000 });
        this.dialogRef.close({ action: 'deleted', id: this.item().id });
      },
      error: () => {
        this.snackBar.open('Löschen fehlgeschlagen.', 'OK', { duration: 3000 });
        this.deleting.set(false);
        this.showDeleteConfirm.set(false);
      },
    });
  }
}
