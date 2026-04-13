import { Component, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatDialogRef, MatDialogModule } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatSnackBar } from '@angular/material/snack-bar';
import { Router } from '@angular/router';
import { ApiService } from '../../../core/api/api.service';
import { Feedback } from '../../../models/feedback.model';

@Component({
  selector: 'app-feedback-dialog',
  imports: [
    ReactiveFormsModule,
    MatDialogModule, MatFormFieldModule, MatInputModule,
    MatButtonModule, MatIconModule,
  ],
  templateUrl: './feedback-dialog.component.html',
  styleUrl: './feedback-dialog.component.scss',
})
export class FeedbackDialogComponent {
  private readonly fb = inject(FormBuilder);
  private readonly api = inject(ApiService);
  private readonly router = inject(Router);
  private readonly snackBar = inject(MatSnackBar);
  private readonly dialogRef = inject(MatDialogRef<FeedbackDialogComponent>);

  readonly form = this.fb.group({
    text: ['', [Validators.required, Validators.maxLength(256)]],
  });

  readonly sending = false;

  get remaining(): number {
    return 256 - (this.form.get('text')?.value?.length ?? 0);
  }

  submit(): void {
    if (this.form.invalid) return;

    const payload = {
      text: this.form.value.text!,
      pageUrl: this.router.url,
    };

    this.api.post<Feedback>('/feedback', payload).subscribe({
      next: () => {
        this.snackBar.open('Feedback gesendet – danke!', undefined, { duration: 3000 });
        this.dialogRef.close(true);
      },
      error: () => {
        this.snackBar.open('Fehler beim Senden. Bitte erneut versuchen.', 'OK', { duration: 4000 });
      },
    });
  }
}
