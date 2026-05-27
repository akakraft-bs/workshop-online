import { Component, inject } from '@angular/core';
import { MAT_DIALOG_DATA, MatDialogModule } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { CalendarEvent } from '../../../models/calendar.model';

@Component({
  selector: 'app-conflict-confirm-dialog',
  imports: [MatDialogModule, MatButtonModule],
  templateUrl: './conflict-confirm-dialog.component.html',
  styleUrl: './conflict-confirm-dialog.component.scss',
})
export class ConflictConfirmDialogComponent {
  readonly data = inject<CalendarEvent[]>(MAT_DIALOG_DATA);
}
