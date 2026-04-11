import { Component, inject, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatDividerModule } from '@angular/material/divider';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { Role, ROLE_LABELS, User, VORSTAND_ROLES } from '../../../../models/user.model';
import { ApiService } from '../../../../core/api/api.service';
import { MatSnackBar } from '@angular/material/snack-bar';

export interface EditRolesDialogData {
  user: User;
}

interface RoleGroup {
  label: string;
  roles: Role[];
}

const ROLE_GROUPS: RoleGroup[] = [
  {
    label: 'Mitgliedschaft',
    roles: [Role.Member],
  },
  {
    label: 'Vorstand',
    roles: VORSTAND_ROLES,
  },
  {
    label: 'Administration',
    roles: [Role.Admin],
  },
];

@Component({
  selector: 'app-edit-roles-dialog',
  imports: [
    MatDialogModule, MatButtonModule, MatCheckboxModule,
    MatDividerModule, MatIconModule, MatProgressSpinnerModule,
  ],
  templateUrl: './edit-roles-dialog.component.html',
  styleUrl: './edit-roles-dialog.component.scss',
})
export class EditRolesDialogComponent {
  private readonly api = inject(ApiService);
  private readonly snackBar = inject(MatSnackBar);
  private readonly dialogRef = inject(MatDialogRef<EditRolesDialogComponent>);
  readonly data: EditRolesDialogData = inject(MAT_DIALOG_DATA);

  readonly saving = signal(false);
  readonly roleLabels = ROLE_LABELS;
  readonly roleGroups = ROLE_GROUPS;

  // Arbeits-Set – welche Rollen aktuell ausgewählt sind
  readonly selectedRoles = signal<Set<Role>>(
    new Set(this.data.user.roles.filter((r): r is Role => r !== Role.None))
  );

  isSelected(role: Role): boolean {
    return this.selectedRoles().has(role);
  }

  toggle(role: Role): void {
    this.selectedRoles.update(set => {
      const next = new Set(set);
      next.has(role) ? next.delete(role) : next.add(role);
      return next;
    });
  }

  async save(): Promise<void> {
    this.saving.set(true);
    const user = this.data.user;
    const current = new Set<Role>(user.roles.filter((r): r is Role => r !== Role.None));
    const desired = this.selectedRoles();

    const toAdd = ([...desired] as Role[]).filter(r => !current.has(r));
    const toRemove = ([...current] as Role[]).filter(r => !desired.has(r));

    try {
      for (const role of toAdd) {
        await this.api.post(`/users/${user.id}/roles/${role}`, {}).toPromise();
      }
      for (const role of toRemove) {
        await this.api.delete(`/users/${user.id}/roles/${role}`).toPromise();
      }

      const updatedRoles: Role[] = desired.size === 0
        ? [Role.None]
        : [...desired];

      this.dialogRef.close({ ...user, roles: updatedRoles });
    } catch {
      this.snackBar.open('Fehler beim Speichern der Rollen.', 'OK', { duration: 3000 });
    } finally {
      this.saving.set(false);
    }
  }

  cancel(): void {
    this.dialogRef.close();
  }
}
