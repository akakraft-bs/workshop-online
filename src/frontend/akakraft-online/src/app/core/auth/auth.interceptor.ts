import { inject } from '@angular/core';
import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { catchError, switchMap, throwError } from 'rxjs';
import { AuthService } from './auth.service';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const authService = inject(AuthService);

  const withAuth = (token: string | null) =>
    token ? req.clone({ headers: req.headers.set('Authorization', `Bearer ${token}`) }) : req;

  // Refresh-Anfragen selbst nicht abfangen (verhindert Endlosschleife)
  const isRefreshCall = req.url.includes('/auth/refresh') || req.url.includes('/auth/logout');

  return next(withAuth(authService.getToken())).pipe(
    catchError((err: HttpErrorResponse) => {
      if (err.status !== 401 || isRefreshCall) {
        return throwError(() => err);
      }

      // JWT abgelaufen → einmal per Cookie refreshen, dann Original wiederholen
      return authService.refresh().pipe(
        switchMap(newToken => next(withAuth(newToken))),
        catchError(() => throwError(() => err)),
      );
    })
  );
};
