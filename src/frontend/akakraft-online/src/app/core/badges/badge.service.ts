import { inject, Injectable, signal } from '@angular/core';
import { ApiService } from '../api/api.service';

interface Badges {
  pendingUmfragen: number;
  openMaengel: number;
  lowStock: number;
  unseenFeedback: number;
}

@Injectable({ providedIn: 'root' })
export class BadgeService {
  private readonly api = inject(ApiService);

  readonly pendingUmfragen = signal(0);
  readonly openMaengel = signal(0);
  readonly lowStock = signal(0);
  readonly unseenFeedback = signal(0);

  refresh(): void {
    this.api.get<Badges>('/users/me/badges').subscribe({
      next: b => {
        this.pendingUmfragen.set(b.pendingUmfragen);
        this.openMaengel.set(b.openMaengel);
        this.lowStock.set(b.lowStock);
        this.unseenFeedback.set(b.unseenFeedback);
      },
    });
  }
}
