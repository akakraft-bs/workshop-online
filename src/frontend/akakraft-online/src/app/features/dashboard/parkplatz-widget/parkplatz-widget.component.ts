import { Component, computed, inject, OnDestroy, OnInit, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatButtonModule } from '@angular/material/button';
import { MatTooltipModule } from '@angular/material/tooltip';
import { ParkplatzService } from '../../../core/parkplatz/parkplatz.service';
import { AuthService } from '../../../core/auth/auth.service';
import { ParkOverview } from '../../../models/parkplatz.model';

@Component({
  selector: 'app-parkplatz-widget',
  imports: [RouterLink, MatIconModule, MatProgressSpinnerModule, MatButtonModule, MatTooltipModule],
  templateUrl: './parkplatz-widget.component.html',
  styleUrl: './parkplatz-widget.component.scss',
})
export class ParkplatzWidgetComponent implements OnInit, OnDestroy {
  private readonly api = inject(ParkplatzService);
  private readonly auth = inject(AuthService);

  readonly overview = signal<ParkOverview | null>(null);
  readonly loading = signal(true);
  private readonly now = signal(Date.now());
  private intervalId?: ReturnType<typeof setInterval>;

  readonly myUserId = computed(() => this.auth.currentUser()?.id ?? null);

  readonly freieAnzahl = computed(() =>
    this.overview()?.accounts.filter(a => a.istFrei).length ?? 0
  );

  readonly meinClaimAccountId = computed(() =>
    this.overview()?.accounts.find(a => a.belegung?.userId === this.myUserId())?.id ?? null
  );

  ngOnInit(): void {
    this.load();
    this.intervalId = setInterval(() => { this.now.set(Date.now()); this.load(); }, 60_000);
  }

  ngOnDestroy(): void {
    clearInterval(this.intervalId);
  }

  private load(): void {
    this.api.getOverview().subscribe({
      next: data => { this.overview.set(data); this.loading.set(false); },
      error: () => this.loading.set(false),
    });
  }

  restzeit(autoExpiresAt: string): string {
    const ms = new Date(autoExpiresAt).getTime() - this.now();
    if (ms <= 0) return 'abgelaufen';
    const h = Math.floor(ms / 3_600_000);
    const m = Math.floor((ms % 3_600_000) / 60_000);
    return h > 0 ? `${h} Std.` : `${m} Min.`;
  }
}
