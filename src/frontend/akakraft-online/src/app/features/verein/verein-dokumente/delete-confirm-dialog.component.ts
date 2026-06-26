import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatDialogModule, MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';

export interface DeleteConfirmDialogData {
  fileName: string;
}

@Component({
  selector: 'app-delete-confirm-dialog',
  imports: [
    FormsModule,
    MatDialogModule, MatButtonModule, MatFormFieldModule, MatInputModule,
  ],
  template: `
    <h2 mat-dialog-title>Dokument löschen</h2>
    <mat-dialog-content>
      <p class="doc-name">„{{ data.fileName }}"</p>
      <p class="hint">
        Diese Aktion kann nicht rückgängig gemacht werden.<br>
        Gib <strong>löschen</strong> ein, um fortzufahren.
      </p>
      <mat-form-field appearance="outline" class="input-field">
        <mat-label>Bestätigung</mat-label>
        <input matInput [(ngModel)]="input" autocomplete="off" spellcheck="false">
      </mat-form-field>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button (click)="cancel()">Abbrechen</button>
      <button mat-raised-button color="warn"
              [disabled]="input().toLowerCase() !== 'löschen'"
              (click)="confirm()">
        Löschen
      </button>
    </mat-dialog-actions>
  `,
  styles: [`
    .doc-name {
      font-weight: 600;
      word-break: break-all;
      margin-bottom: 8px;
    }
    .hint {
      font-size: 14px;
      color: var(--mat-sys-on-surface-variant);
      margin-bottom: 16px;
      line-height: 1.5;
    }
    .input-field { width: 100%; }
  `],
})
export class DeleteConfirmDialogComponent {
  readonly data: DeleteConfirmDialogData = inject(MAT_DIALOG_DATA);
  private readonly dialogRef = inject(MatDialogRef<DeleteConfirmDialogComponent>);

  readonly input = signal('');

  confirm(): void { this.dialogRef.close(true); }
  cancel(): void  { this.dialogRef.close(false); }
}
