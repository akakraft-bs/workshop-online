import { Component, inject, OnInit, signal } from '@angular/core';
import { AbstractControl, FormBuilder, ReactiveFormsModule, ValidationErrors, ValidatorFn, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { HttpErrorResponse } from '@angular/common/http';
import { catchError, EMPTY } from 'rxjs';
import { AuthService } from '../../../core/auth/auth.service';

const passwordMatchValidator: ValidatorFn = (group: AbstractControl): ValidationErrors | null => {
  const pw = group.get('newPassword')?.value;
  const pw2 = group.get('passwordConfirm')?.value;
  return pw && pw2 && pw !== pw2 ? { passwordMismatch: true } : null;
};

@Component({
  selector: 'app-reset-password',
  imports: [
    ReactiveFormsModule, RouterLink,
    MatButtonModule, MatIconModule,
    MatFormFieldModule, MatInputModule,
    MatProgressSpinnerModule,
  ],
  templateUrl: './reset-password.component.html',
  styleUrl: './reset-password.component.scss',
})
export class ResetPasswordComponent implements OnInit {
  private readonly auth = inject(AuthService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly fb = inject(FormBuilder);

  token = '';
  loading = signal(false);
  showPassword = signal(false);
  showConfirm = signal(false);
  error = signal<string | null>(null);
  success = signal(false);

  readonly form = this.fb.group({
    newPassword:     ['', [Validators.required, Validators.minLength(8)]],
    passwordConfirm: ['', Validators.required],
  }, { validators: passwordMatchValidator });

  ngOnInit(): void {
    this.token = this.route.snapshot.queryParamMap.get('token') ?? '';
    if (!this.token) this.error.set('Ungültiger Link.');
  }

  submit(): void {
    if (this.form.invalid || !this.token) { this.form.markAllAsTouched(); return; }
    const { newPassword } = this.form.getRawValue();

    this.loading.set(true);
    this.error.set(null);

    this.auth.resetPassword(this.token, newPassword!)
      .pipe(
        catchError((err: HttpErrorResponse) => {
          this.error.set(err.error?.error ?? 'Der Link ist ungültig oder abgelaufen.');
          this.loading.set(false);
          return EMPTY;
        })
      )
      .subscribe(() => {
        this.loading.set(false);
        this.success.set(true);
        setTimeout(() => this.router.navigate(['/login']), 2500);
      });
  }
}
