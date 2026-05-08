import { Component, inject, OnDestroy, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { AuthService } from '../../../core/auth/auth.service';

@Component({
  selector: 'app-pending',
  imports: [MatButtonModule, MatIconModule, MatProgressSpinnerModule],
  templateUrl: './pending.component.html',
  styleUrl: './pending.component.scss',
})
export class PendingComponent implements OnInit, OnDestroy {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  readonly currentUser = this.auth.currentUser;

  private intervalId: ReturnType<typeof setInterval> | null = null;

  ngOnInit(): void {
    this.check();
    this.intervalId = setInterval(() => this.check(), 30_000);
  }

  ngOnDestroy(): void {
    if (this.intervalId !== null) clearInterval(this.intervalId);
  }

  private check(): void {
    this.auth.refresh().subscribe({
      next: () => {
        this.auth.refreshCurrentUser();
        // refreshCurrentUser ist fire-and-forget; kurz warten bis Signal aktualisiert
        setTimeout(() => {
          if (this.auth.hasAccess()) {
            this.router.navigateByUrl('/dashboard');
          }
        }, 500);
      },
      // Fehler (z.B. Refresh-Token abgelaufen) werden von refresh() selbst behandelt
    });
  }

  logout(): void {
    this.auth.logout();
  }
}
