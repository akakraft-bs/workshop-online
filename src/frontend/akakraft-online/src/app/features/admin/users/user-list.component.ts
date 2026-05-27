import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { MatTableModule } from '@angular/material/table';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatChipsModule } from '@angular/material/chips';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatDialog } from '@angular/material/dialog';
import { MatBadgeModule } from '@angular/material/badge';
import { MatTooltipModule } from '@angular/material/tooltip';
import { ApiService } from '../../../core/api/api.service';
import { AuthService } from '../../../core/auth/auth.service';
import { Role, ROLE_LABELS, VORSTAND_ROLES, User } from '../../../models/user.model';
import {
  EditRolesDialogComponent,
  EditRolesDialogData,
} from './edit-roles-dialog/edit-roles-dialog.component';

@Component({
  selector: 'app-user-list',
  imports: [
    MatTableModule, MatIconModule, MatButtonModule, MatChipsModule,
    MatProgressSpinnerModule, MatBadgeModule, MatTooltipModule,
  ],
  templateUrl: './user-list.component.html',
  styleUrl: './user-list.component.scss',
})
export class UserListComponent implements OnInit {
  private readonly api = inject(ApiService);
  private readonly auth = inject(AuthService);
  private readonly snackBar = inject(MatSnackBar);
  private readonly dialog = inject(MatDialog);

  readonly users = signal<User[]>([]);
  readonly loading = signal(true);
  readonly roleLabels = ROLE_LABELS;
  readonly displayedColumns = ['name', 'email', 'roles', 'actions'];

  readonly sortedUsers = computed(() =>
    [...this.users()].sort((a, b) => {
      const nameA = (a.displayName || a.name).toLowerCase();
      const nameB = (b.displayName || b.name).toLowerCase();
      return nameA.localeCompare(nameB, 'de');
    })
  );

  readonly pendingUsers = computed(() =>
    this.sortedUsers().filter(u => u.roles.length === 0 || u.roles.every(r => r === Role.None))
  );

  readonly activeUsers = computed(() =>
    this.sortedUsers().filter(u => u.roles.some(r => r !== Role.None))
  );

  ngOnInit(): void {
    this.loadUsers();
  }

  loadUsers(): void {
    this.api.get<User[]>('/users').subscribe({
      next: data => {
        this.users.set(data);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.snackBar.open('Nutzer konnten nicht geladen werden.', 'OK', { duration: 3000 });
      },
    });
  }

  openEditDialog(user: User): void {
    const ref = this.dialog.open<EditRolesDialogComponent, EditRolesDialogData, User>(
      EditRolesDialogComponent,
      {
        data: { user },
        width: '400px',
        maxWidth: '95vw',
      }
    );

    ref.afterClosed().subscribe(updated => {
      if (updated) {
        this.users.update(list => list.map(u => u.id === updated.id ? updated : u));
      }
    });
  }

  deleteUser(user: User): void {
    const name = user.displayName || user.name;
    if (!confirm(`Nutzer „${name}" wirklich löschen? Diese Aktion kann nicht rückgängig gemacht werden.`)) return;

    this.api.delete(`/users/${user.id}`).subscribe({
      next: () => {
        this.users.update(list => list.filter(u => u.id !== user.id));
        this.snackBar.open(`${name} wurde gelöscht.`, 'OK', { duration: 3000 });
      },
      error: (err) => {
        const msg = err?.error?.error ?? 'Fehler beim Löschen des Nutzers.';
        this.snackBar.open(msg, 'OK', { duration: 5000 });
      },
    });
  }

  isSelf(user: User): boolean {
    return user.id === this.auth.currentUser()?.id;
  }

  getDisplayedRoles(user: User): Role[] {
    return user.roles
      .filter(r => r !== Role.None)
      .sort((a, b) => ROLE_SORT_ORDER.indexOf(a) - ROLE_SORT_ORDER.indexOf(b));
  }

  isPending(user: User): boolean {
    return user.roles.length === 0 || user.roles.every(r => r === Role.None);
  }
}

// Mitglied → Vorstand-Rollen → Moderator → Admin
const ROLE_SORT_ORDER: Role[] = [
  Role.Member,
  ...VORSTAND_ROLES,
  Role.Moderator,
  Role.Admin,
];
