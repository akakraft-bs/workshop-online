import { computed, inject, Injectable, signal } from '@angular/core';import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Router } from '@angular/router';
import { catchError, EMPTY, firstValueFrom, tap } from 'rxjs';
import { Role, User, VORSTAND_ROLES } from '../../models/user.model';
import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly TOKEN_KEY = 'auth_token';
  private readonly USER_KEY = 'auth_user';
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);

  readonly currentUser = signal<User | null>(null);
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
        tap(user => {
          this.currentUser.set(user);
          this.cacheUser(user);
        }),
        catchError(() => {
          this.clearSession();
          return EMPTY;
        })
      )
      .subscribe(() => {
        const target = this.hasAccess() ? '/dashboard' : '/pending';
        this.router.navigate([target]);
      });
  }

  logout(): void {
    this.clearSession();
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
      return;
    }

    // Sofort aus Cache wiederherstellen → kein Warten auf Backend nötig
    const cached = this.loadCachedUser();
    if (cached) {
      this.currentUser.set(cached);
    }

    // Backend-Abfrage zur Aktualisierung (im Hintergrund)
    await firstValueFrom(
      this.http.get<User>(`${environment.apiUrl}/auth/me`).pipe(
        tap(user => {
          this.currentUser.set(user);
          this.cacheUser(user);
        }),
        catchError((err: HttpErrorResponse) => {
          if (err.status === 401 || err.status === 403) {
            // Token abgelaufen oder ungültig → komplett abmelden
            this.clearSession();
          }
          // Bei Netzwerkfehlern: gecachten User behalten
          return EMPTY;
        })
      )
    ).catch(() => {});
  }

  private cacheUser(user: User): void {
    localStorage.setItem(this.USER_KEY, JSON.stringify(user));
  }

  private loadCachedUser(): User | null {
    try {
      const raw = localStorage.getItem(this.USER_KEY);
      return raw ? (JSON.parse(raw) as User) : null;
    } catch {
      return null;
    }
  }

  private clearSession(): void {
    localStorage.removeItem(this.TOKEN_KEY);
    localStorage.removeItem(this.USER_KEY);
    this.currentUser.set(null);
  }
}
