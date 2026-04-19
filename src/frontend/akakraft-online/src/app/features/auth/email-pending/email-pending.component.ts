import { Component, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { catchError, EMPTY } from 'rxjs';
import { AuthService } from '../../../core/auth/auth.service';

@Component({
  selector: 'app-email-pending',
  imports: [MatButtonModule, MatIconModule, RouterLink],
  templateUrl: './email-pending.component.html',
  styleUrl: './email-pending.component.scss',
})
export class EmailPendingComponent {
  private readonly auth = inject(AuthService);
  private readonly route = inject(ActivatedRoute);

  readonly email = this.route.snapshot.queryParamMap.get('email') ?? '';
  readonly resent = signal(false);
  readonly resending = signal(false);

  resend(): void {
    if (!this.email || this.resending()) return;
    this.resending.set(true);
    this.auth.resendConfirmation(this.email)
      .pipe(catchError(() => EMPTY))
      .subscribe(() => {
        this.resending.set(false);
        this.resent.set(true);
      });
  }
}
