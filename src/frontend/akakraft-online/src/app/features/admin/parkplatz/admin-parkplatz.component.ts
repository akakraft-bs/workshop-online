import { Component, inject, OnInit, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { switchMap } from 'rxjs';
import { ParkplatzService } from '../../../core/parkplatz/parkplatz.service';
import { ParkAccountAdmin } from '../../../models/parkplatz.model';

@Component({
  selector: 'app-admin-parkplatz',
  imports: [
    ReactiveFormsModule, MatCardModule, MatButtonModule, MatFormFieldModule,
    MatInputModule, MatIconModule, MatProgressSpinnerModule, MatSnackBarModule,
  ],
  templateUrl: './admin-parkplatz.component.html',
  styleUrl: './admin-parkplatz.component.scss',
})
export class AdminParkplatzComponent implements OnInit {
  private readonly svc = inject(ParkplatzService);
  private readonly snackBar = inject(MatSnackBar);
  private readonly fb = inject(FormBuilder);

  readonly loading = signal(true);
  readonly saving = signal<string | null>(null);
  readonly accounts = signal<ParkAccountAdmin[]>([]);

  readonly forms: Record<string, ReturnType<typeof this.buildForm>> = {};

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.svc.getAdminAccounts().subscribe({
      next: list => {
        this.accounts.set(list);
        for (const a of list) this.forms[a.id] = this.buildForm(a);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.snackBar.open('Parkkonten konnten nicht geladen werden.', 'OK', { duration: 3000 });
      },
    });
  }

  save(account: ParkAccountAdmin): void {
    const form = this.forms[account.id];
    if (!form || form.invalid) return;
    const v = form.getRawValue();
    this.saving.set(account.id);

    const password = (v.portalPassword ?? '').length > 0 ? v.portalPassword! : null;

    this.svc.updateAccount(account.id, {
      label: (v.label ?? '').trim(),
      portalUrl: (v.portalUrl ?? '').trim() || null,
      notiz: (v.notiz ?? '').trim() || null,
    }).pipe(
      switchMap(() => this.svc.setZugang(account.id, (v.portalUsername ?? '').trim(), password)),
    ).subscribe({
      next: () => {
        this.saving.set(null);
        form.get('portalPassword')!.reset('');
        this.snackBar.open('Gespeichert.', 'OK', { duration: 3000 });
        this.load();
      },
      error: () => {
        this.saving.set(null);
        this.snackBar.open('Fehler beim Speichern.', 'OK', { duration: 4000 });
      },
    });
  }

  private buildForm(a: ParkAccountAdmin) {
    return this.fb.group({
      label: [a.label, [Validators.required, Validators.maxLength(80)]],
      portalUrl: [a.portalUrl ?? '', [Validators.maxLength(512)]],
      notiz: [a.notiz ?? '', [Validators.maxLength(1000)]],
      portalUsername: [a.portalUsername ?? '', [Validators.maxLength(128)]],
      portalPassword: ['', [Validators.maxLength(256)]],
    });
  }
}
