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
    path: 'auth/register',
    loadComponent: () =>
      import('./features/auth/register/register.component').then(m => m.RegisterComponent),
  },
  {
    path: 'auth/email-pending',
    loadComponent: () =>
      import('./features/auth/email-pending/email-pending.component').then(m => m.EmailPendingComponent),
  },
  {
    path: 'auth/confirm-email',
    loadComponent: () =>
      import('./features/auth/confirm-email/confirm-email.component').then(m => m.ConfirmEmailComponent),
  },
  {
    path: 'auth/forgot-password',
    loadComponent: () =>
      import('./features/auth/forgot-password/forgot-password.component').then(m => m.ForgotPasswordComponent),
  },
  {
    path: 'auth/reset-password',
    loadComponent: () =>
      import('./features/auth/reset-password/reset-password.component').then(m => m.ResetPasswordComponent),
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
        path: 'hallenbuch',
        loadComponent: () =>
          import('./features/hallenbuch/hallenbuch-list.component').then(m => m.HallenbuchListComponent),
      },
      {
        path: 'verein',
        loadComponent: () =>
          import('./features/verein/verein-shell.component').then(m => m.VereinShellComponent),
        children: [
          { path: '', redirectTo: 'info', pathMatch: 'full' },
          {
            path: 'info',
            loadComponent: () =>
              import('./features/verein/verein-info/verein-info.component').then(m => m.VereinInfoComponent),
          },
          {
            path: 'dokumente',
            loadComponent: () =>
              import('./features/verein/verein-dokumente/verein-dokumente.component').then(m => m.VereinDokumenteComponent),
          },
          {
            path: 'zugaenge',
            loadComponent: () =>
              import('./features/verein/verein-zugaenge/verein-zugaenge.component').then(m => m.VereinZugaengeComponent),
          },
        ],
      },
      {
        path: 'projekte',
        loadComponent: () =>
          import('./features/verein/verein-projekte/verein-projekte.component').then(m => m.VereinProjekteComponent),
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
