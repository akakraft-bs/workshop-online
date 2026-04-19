import { Component, inject, signal } from '@angular/core';
import { AbstractControl, FormBuilder, ReactiveFormsModule, ValidationErrors, ValidatorFn, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { AuthService } from '../../../core/auth/auth.service';

const passwordMatchValidator: ValidatorFn = (group: AbstractControl): ValidationErrors | null => {
  const pw = group.get('password')?.value;
  const pw2 = group.get('passwordConfirm')?.value;
  return pw && pw2 && pw !== pw2 ? { passwordMismatch: true } : null;
};

@Component({
  selector: 'app-register',
  imports: [
    ReactiveFormsModule, RouterLink,
    MatButtonModule, MatIconModule,
    MatFormFieldModule, MatInputModule,
    MatProgressSpinnerModule,
  ],
  templateUrl: './register.component.html',
  styleUrl: './register.component.scss',
})
export class RegisterComponent {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly fb = inject(FormBuilder);

  logoFailed = false;
  loading = signal(false);
  showPassword = signal(false);
  showPasswordConfirm = signal(false);
  error = signal<string | null>(null);

  readonly form = this.fb.group({
    displayName: ['', [Validators.required, Validators.minLength(2), Validators.maxLength(64)]],
    email:       ['', [Validators.required, Validators.email]],
    password:    ['', [Validators.required, Validators.minLength(8)]],
    passwordConfirm: ['', Validators.required],
  }, { validators: passwordMatchValidator });

  async submit(): Promise<void> {
    if (this.form.invalid) { this.form.markAllAsTouched(); return; }

    const { email, password, displayName } = this.form.getRawValue();
    this.loading.set(true);
    this.error.set(null);

    try {
      await this.auth.register(email!, password!, displayName!);
      this.router.navigate(['/auth/email-pending'], { queryParams: { email } });
    } catch (err: unknown) {
      const apiError = (err as { error?: { error?: string } })?.error?.error;
      this.error.set(apiError ?? 'Registrierung fehlgeschlagen. Bitte versuche es erneut.');
    } finally {
      this.loading.set(false);
    }
  }
}
