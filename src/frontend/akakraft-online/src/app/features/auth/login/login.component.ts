import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { catchError, EMPTY } from 'rxjs';
import { AuthService } from '../../../core/auth/auth.service';

@Component({
  selector: 'app-login',
  imports: [
    ReactiveFormsModule, RouterLink,
    MatButtonModule, MatIconModule,
    MatFormFieldModule, MatInputModule,
    MatProgressSpinnerModule,
  ],
  templateUrl: './login.component.html',
  styleUrl: './login.component.scss',
})
export class LoginComponent {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly fb = inject(FormBuilder);

  logoFailed = false;
  mode = signal<'google' | 'email'>('google');
  loading = signal(false);
  showPassword = signal(false);
  error = signal<string | null>(null);

  readonly form = this.fb.group({
    email:    ['', [Validators.required, Validators.email]],
    password: ['', Validators.required],
  });

  loginGoogle(): void {
    this.auth.login();
  }

  switchMode(m: 'google' | 'email'): void {
    this.mode.set(m);
    this.error.set(null);
    this.form.reset();
  }

  submitEmail(): void {
    if (this.form.invalid) { this.form.markAllAsTouched(); return; }
    const { email, password } = this.form.getRawValue();

    this.loading.set(true);
    this.error.set(null);

    this.auth.loginWithEmail(email!, password!)
      .pipe(
        catchError((err: HttpErrorResponse) => {
          this.loading.set(false);
          if (err.status === 403 && err.error?.error === 'email_not_confirmed') {
            this.router.navigate(['/auth/email-pending'], { queryParams: { email: email } });
          } else {
            this.error.set('E-Mail oder Passwort ist falsch.');
          }
          return EMPTY;
        })
      )
      .subscribe(() => {
        this.auth.refreshCurrentUser();
        this.loading.set(false);
        // refreshCurrentUser navigiert nicht – wir warten auf Signal-Update
        this.auth.currentUser; // trigger read
        setTimeout(() => {
          this.router.navigate([this.auth.hasAccess() ? '/dashboard' : '/pending']);
        }, 100);
      });
  }
}
