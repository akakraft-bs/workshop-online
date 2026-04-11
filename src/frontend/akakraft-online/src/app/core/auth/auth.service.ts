import { computed, inject, Injectable, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { catchError, EMPTY, firstValueFrom, tap } from 'rxjs';
import { Role, User, VORSTAND_ROLES } from '../../models/user.model';
import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly TOKEN_KEY = 'auth_token';
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);

  readonly currentUser = signal<User | null>(null);
  readonly isInitialized = signal(false);
  readonly isLoggedIn = computed(() => this.currentUser() !== null);
  readonly roles = computed(() => this.currentUser()?.roles ?? []);

  readonly isAdmin = computed(() => this.roles().includes(Role.Admin));
  readonly isVorstand = computed(() =>
    this.roles().some(r => VORSTAND_ROLES.includes(r))
  );
  readonly hasAccess = computed(() =>
    this.roles().some(r => r !== Role.None)
  );

  // Wird vom authGuard abgewartet bevor die Route aktiviert wird
  readonly initialized$: Promise<void>;

  constructor() {
    this.initialized$ = this.tryRestoreSession();
  }

  login(): void {
    window.location.href = `${environment.apiUrl}/auth/login/google`;
  }

  handleCallback(token: string): void {
    localStorage.setItem(this.TOKEN_KEY, token);
    this.http
      .get<User>(`${environment.apiUrl}/auth/me`)
      .pipe(
        tap(user => this.currentUser.set(user)),
        catchError(() => {
          this.logout();
          return EMPTY;
        })
      )
      .subscribe(() => this.router.navigate(['/dashboard']));
  }

  logout(): void {
    localStorage.removeItem(this.TOKEN_KEY);
    this.currentUser.set(null);
    this.router.navigate(['/login']);
  }

  getToken(): string | null {
    return localStorage.getItem(this.TOKEN_KEY);
  }

  hasRole(role: Role): boolean {
    return this.roles().includes(role);
  }

  hasAnyRole(roles: Role[]): boolean {
    return roles.some(r => this.roles().includes(r));
  }

  private async tryRestoreSession(): Promise<void> {
    const token = this.getToken();
    if (!token) {
      this.isInitialized.set(true);
      return;
    }

    await firstValueFrom(
      this.http.get<User>(`${environment.apiUrl}/auth/me`).pipe(
        tap(user => this.currentUser.set(user)),
        catchError(() => {
          localStorage.removeItem(this.TOKEN_KEY);
          return EMPTY;
        })
      )
    ).catch(() => {});

    this.isInitialized.set(true);
  }
}
