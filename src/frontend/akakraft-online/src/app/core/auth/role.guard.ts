import { inject } from '@angular/core';
import { ActivatedRouteSnapshot, CanActivateFn, Router } from '@angular/router';
import { AuthService } from './auth.service';
import { Role } from '../../models/user.model';

export const roleGuard: CanActivateFn = (route: ActivatedRouteSnapshot) => {
  const auth = inject(AuthService);
  const router = inject(Router);
  const requiredRoles: Role[] = route.data['roles'] ?? [];

  if (auth.hasAnyRole(requiredRoles)) return true;

  return router.createUrlTree(['/dashboard']);
};
