import { Component, inject } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { AuthService } from '../../../core/auth/auth.service';

@Component({
  selector: 'app-pending',
  imports: [MatButtonModule, MatIconModule],
  templateUrl: './pending.component.html',
  styleUrl: './pending.component.scss',
})
export class PendingComponent {
  private readonly auth = inject(AuthService);

  readonly currentUser = this.auth.currentUser;

  logout(): void {
    this.auth.logout();
  }
}
