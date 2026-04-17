import { Component, inject, OnInit, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatOptionModule } from '@angular/material/core';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { MatSnackBar } from '@angular/material/snack-bar';
import { ApiService } from '../../../core/api/api.service';
import { User } from '../../../models/user.model';

@Component({
  selector: 'app-admin-push',
  imports: [
    ReactiveFormsModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatOptionModule,
    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule,
  ],
  templateUrl: './admin-push.component.html',
  styleUrl: './admin-push.component.scss',
})
export class AdminPushComponent implements OnInit {
  private readonly api = inject(ApiService);
  private readonly snackBar = inject(MatSnackBar);
  private readonly fb = inject(FormBuilder);

  readonly users = signal<User[]>([]);
  readonly loading = signal(true);
  readonly sending = signal(false);

  readonly form = this.fb.group({
    userId: [null as string | null],
    title: ['Test-Benachrichtigung', Validators.required],
    body: ['Dies ist eine Test-Benachrichtigung von AkaKraft.', Validators.required],
  });

  ngOnInit(): void {
    this.api.get<User[]>('/users').subscribe({
      next: users => {
        this.users.set(users);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }

  send(): void {
    if (this.form.invalid || this.sending()) return;
    this.sending.set(true);

    const { userId, title, body } = this.form.getRawValue();

    this.api.post<void>('/admin/push/test', {
      userId: userId || null,
      title,
      body,
    }).subscribe({
      next: () => {
        this.sending.set(false);
        const target = userId
          ? this.users().find(u => u.id === userId)?.name ?? 'Nutzer'
          : 'alle Nutzer';
        this.snackBar.open(`Benachrichtigung an ${target} gesendet.`, 'OK', { duration: 4000 });
      },
      error: () => {
        this.sending.set(false);
        this.snackBar.open('Fehler beim Senden.', 'OK', { duration: 3000 });
      },
    });
  }
}
