import { computed, inject, Injectable, signal } from '@angular/core';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Router } from '@angular/router';
import { catchError, EMPTY, firstValueFrom, Observable, tap } from 'rxjs';
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
  readonly isVorstand = computed(() => this.roles().some(r => VORSTAND_ROLES.includes(r)));
  readonly hasAccess = computed(() => this.roles().some(r => r !== Role.None));

  readonly initialized$: Promise<void>;

  constructor() {
    this.initialized$ = this.tryRestoreSession();
  }

  login(): void {
    window.location.href = `${environment.apiUrl}/auth/login/google`;
  }

  handleCallback(token: string): void {
    localStorage.setItem(this.TOKEN_KEY, token);
    this.http.get<User>(`${environment.apiUrl}/auth/me`, { withCredentials: true })
      .pipe(
        tap(user => { this.currentUser.set(user); this.cacheUser(user); }),
        catchError(() => { this.clearSession(); return EMPTY; })
      )
      .subscribe(() => this.router.navigate([this.hasAccess() ? '/dashboard' : '/pending']));
  }

  logout(): void {
    this.http.post(`${environment.apiUrl}/auth/logout`, {}, { withCredentials: true })
      .pipe(catchError(() => EMPTY))
      .subscribe(() => { this.clearSession(); this.router.navigate(['/login']); });
  }

  /** Versucht per Refresh-Token (httpOnly-Cookie) ein neues JWT zu holen. */
  refresh(): Observable<string> {
    return new Observable(observer => {
      this.http.post<{ token: string }>(
        `${environment.apiUrl}/auth/refresh`, {}, { withCredentials: true }
      ).pipe(
        tap(res => localStorage.setItem(this.TOKEN_KEY, res.token)),
        catchError((err: HttpErrorResponse) => {
          this.clearSession();
          this.router.navigate(['/login']);
          observer.error(err);
          return EMPTY;
        })
      ).subscribe({
        next: res => { observer.next(res.token); observer.complete(); },
      });
    });
  }

  getToken(): string | null {
    return localStorage.getItem(this.TOKEN_KEY);
  }

  hasRole(role: Role): boolean { return this.roles().includes(role); }
  hasAnyRole(roles: Role[]): boolean { return roles.some(r => this.roles().includes(r)); }

  clearSession(): void {
    localStorage.removeItem(this.TOKEN_KEY);
    localStorage.removeItem(this.USER_KEY);
    this.currentUser.set(null);
  }

  private async tryRestoreSession(): Promise<void> {
    const cached = this.loadCachedUser();
    if (cached) this.currentUser.set(cached);

    // Erst mit vorhandenem JWT versuchen, dann per Refresh-Cookie
    const token = this.getToken();
    const meUrl = `${environment.apiUrl}/auth/me`;

    await firstValueFrom(
      this.http.get<User>(meUrl, { withCredentials: true }).pipe(
        tap(user => { this.currentUser.set(user); this.cacheUser(user); }),
        catchError((err: HttpErrorResponse) => {
          if (err.status === 401) {
            // JWT abgelaufen → Refresh-Cookie versuchen
            return this.http.post<{ token: string }>(
              `${environment.apiUrl}/auth/refresh`, {}, { withCredentials: true }
            ).pipe(
              tap(res => localStorage.setItem(this.TOKEN_KEY, res.token)),
              // Nach erfolgreichem Refresh Nutzerdaten laden
              catchError(() => { this.clearSession(); return EMPTY; })
            );
          }
          if (!token) { /* noch nicht eingeloggt – kein Fehler */ }
          return EMPTY;
        })
      )
    ).catch(() => {});

    // Falls Refresh ein neues Token gesetzt hat, Nutzerdaten laden
    const newToken = this.getToken();
    if (newToken && !this.currentUser()) {
      await firstValueFrom(
        this.http.get<User>(meUrl, { withCredentials: true }).pipe(
          tap(user => { this.currentUser.set(user); this.cacheUser(user); }),
          catchError(() => { this.clearSession(); return EMPTY; })
        )
      ).catch(() => {});
    }
  }

  private cacheUser(user: User): void {
    localStorage.setItem(this.USER_KEY, JSON.stringify(user));
  }

  private loadCachedUser(): User | null {
    try {
      const raw = localStorage.getItem(this.USER_KEY);
      return raw ? (JSON.parse(raw) as User) : null;
    } catch { return null; }
  }
}
