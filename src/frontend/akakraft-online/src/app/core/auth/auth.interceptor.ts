import { inject } from '@angular/core';
import { HttpInterceptorFn } from '@angular/common/http';
import { AuthService } from './auth.service';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const token = inject(AuthService).getToken();

  if (token) {
    return next(
      req.clone({ headers: req.headers.set('Authorization', `Bearer ${token}`) })
    );
  }

  return next(req);
};
