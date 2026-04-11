import { Component, inject, OnInit, signal } from '@angular/core';
import { MatTableModule } from '@angular/material/table';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatChipsModule } from '@angular/material/chips';
import { MatMenuModule } from '@angular/material/menu';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar } from '@angular/material/snack-bar';
import { ApiService } from '../../../core/api/api.service';
import { Role, ROLE_LABELS, User } from '../../../models/user.model';

@Component({
  selector: 'app-user-list',
  imports: [
    MatTableModule, MatIconModule, MatButtonModule,
    MatChipsModule, MatMenuModule, MatProgressSpinnerModule,
  ],
  templateUrl: './user-list.component.html',
  styleUrl: './user-list.component.scss',
})
export class UserListComponent implements OnInit {
  private readonly api = inject(ApiService);
  private readonly snackBar = inject(MatSnackBar);

  readonly users = signal<User[]>([]);
  readonly loading = signal(true);
  readonly displayedColumns = ['name', 'email', 'roles', 'actions'];

  readonly allRoles = Object.values(Role);
  readonly roleLabels = ROLE_LABELS;

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

  assignRole(user: User, role: Role): void {
    this.api.post(`/users/${user.id}/roles/${role}`, {}).subscribe({
      next: (updated: unknown) => {
        this.users.update(list => list.map(u => u.id === user.id ? (updated as User) : u));
        this.snackBar.open(`Rolle "${this.roleLabels[role]}" vergeben.`, 'OK', { duration: 3000 });
      },
      error: () =>
        this.snackBar.open('Fehler beim Vergeben der Rolle.', 'OK', { duration: 3000 }),
    });
  }

  removeRole(user: User, role: Role): void {
    this.api.delete(`/users/${user.id}/roles/${role}`).subscribe({
      next: (updated: unknown) => {
        this.users.update(list => list.map(u => u.id === user.id ? (updated as User) : u));
        this.snackBar.open(`Rolle "${this.roleLabels[role]}" entfernt.`, 'OK', { duration: 3000 });
      },
      error: () =>
        this.snackBar.open('Fehler beim Entfernen der Rolle.', 'OK', { duration: 3000 }),
    });
  }

  getRoleLabel(role: Role): string {
    return this.roleLabels[role];
  }

  getAssignableRoles(user: User): Role[] {
    return this.allRoles.filter(r => !user.roles.includes(r));
  }
}
