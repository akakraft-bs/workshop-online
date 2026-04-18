import { Routes } from '@angular/router';
import { authGuard } from './core/auth/auth.guard';
import { roleGuard } from './core/auth/role.guard';
import { Role } from './models/user.model';

export const routes: Routes = [
  {
    path: 'login',
    loadComponent: () =>
      import('./features/auth/login/login.component').then(m => m.LoginComponent),
  },
  {
    path: 'auth/callback',
    loadComponent: () =>
      import('./features/auth/callback/callback.component').then(m => m.CallbackComponent),
  },
  {
    path: 'pending',
    loadComponent: () =>
      import('./features/auth/pending/pending.component').then(m => m.PendingComponent),
  },
  {
    path: '',
    loadComponent: () =>
      import('./shared/layout/main-layout.component').then(m => m.MainLayoutComponent),
    canActivate: [authGuard],
    children: [
      { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
      {
        path: 'dashboard',
        loadComponent: () =>
          import('./features/dashboard/dashboard.component').then(m => m.DashboardComponent),
      },
      {
        path: 'werkzeug',
        loadComponent: () =>
          import('./features/werkzeug/werkzeug-list.component').then(m => m.WerkzeugListComponent),
      },
      {
        path: 'verbrauchsmaterial',
        loadComponent: () =>
          import('./features/verbrauchsmaterial/verbrauchsmaterial-list.component').then(
            m => m.VerbrauchsmaterialListComponent
          ),
      },
      {
        path: 'mangel',
        loadComponent: () =>
          import('./features/mangel/mangel-list.component').then(m => m.MangelListComponent),
      },
      {
        path: 'wunsch',
        loadComponent: () =>
          import('./features/wunsch/wunsch-list.component').then(m => m.WunschListComponent),
      },
      {
        path: 'umfrage',
        loadComponent: () =>
          import('./features/umfrage/umfrage-list.component').then(m => m.UmfrageListComponent),
      },
      {
        path: 'admin/users',
        loadComponent: () =>
          import('./features/admin/users/user-list.component').then(m => m.UserListComponent),
        canActivate: [roleGuard],
        data: { roles: [Role.Admin] },
      },
      {
        path: 'admin/feedback',
        loadComponent: () =>
          import('./features/admin/feedback/feedback-list.component').then(m => m.AdminFeedbackListComponent),
        canActivate: [roleGuard],
        data: { roles: [Role.Admin] },
      },
      {
        path: 'admin/push',
        loadComponent: () =>
          import('./features/admin/push/admin-push.component').then(m => m.AdminPushComponent),
        canActivate: [roleGuard],
        data: { roles: [Role.Admin] },
      },
      {
        path: 'kalender',
        loadComponent: () =>
          import('./features/kalender/kalender-page.component').then(m => m.KalenderPageComponent),
      },
      {
        path: 'veranstaltungen',
        loadComponent: () =>
          import('./features/veranstaltungen/veranstaltungen-page.component').then(m => m.VeranstaltungenPageComponent),
      },
      {
        path: 'admin/kalender',
        loadComponent: () =>
          import('./features/admin/kalender/admin-kalender.component').then(m => m.AdminKalenderComponent),
        canActivate: [roleGuard],
        data: { roles: [Role.Admin] },
      },
    ],
  },
  { path: '**', redirectTo: '' },
];
