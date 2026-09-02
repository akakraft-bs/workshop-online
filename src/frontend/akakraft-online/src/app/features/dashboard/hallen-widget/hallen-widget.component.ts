import { Component, computed, inject, OnDestroy, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTooltipModule } from '@angular/material/tooltip';
import { ApiService } from '../../../core/api/api.service';
import { AuthService } from '../../../core/auth/auth.service';
import { HallenCheck } from '../../../models/hallen-check.model';

const AVATAR_COLORS = ['#6366f1','#ec4899','#f59e0b','#10b981','#3b82f6','#8b5cf6','#ef4444','#0ea5e9'];

@Component({
  selector: 'app-hallen-widget',
  imports: [FormsModule, MatButtonModule, MatIconModule, MatProgressSpinnerModule, MatTooltipModule],
  templateUrl: './hallen-widget.component.html',
  styleUrl: './hallen-widget.component.scss',
})
export class HallenWidgetComponent implements OnInit, OnDestroy {
  private readonly api  = inject(ApiService);
  private readonly auth = inject(AuthService);

  readonly checks      = signal<HallenCheck[]>([]);
  readonly loading     = signal(true);
  readonly saving      = signal(false);
  readonly showForm    = signal(false);
  message = '';

  readonly myCheck = computed(() => {
    const uid = this.auth.currentUser()?.id;
    return this.checks().find(c => c.userId === uid) ?? null;
  });

  readonly others = computed(() => {
    const uid = this.auth.currentUser()?.id;
    return this.checks().filter(c => c.userId !== uid);
  });

  readonly isCheckedIn = computed(() => this.myCheck() !== null);

  private intervalId?: ReturnType<typeof setInterval>;

  ngOnInit(): void {
    this.load();
    this.intervalId = setInterval(() => this.load(), 60_000);
  }

  ngOnDestroy(): void {
    clearInterval(this.intervalId);
  }

  private load(): void {
    this.api.get<HallenCheck[]>('/halle/anwesend').subscribe({
      next: data => { this.checks.set(data); this.loading.set(false); },
      error: () => this.loading.set(false),
    });
  }

  openForm(): void { this.showForm.set(true); }

  cancelForm(): void { this.showForm.set(false); this.message = ''; }

  checkIn(): void {
    this.saving.set(true);
    this.api.post<HallenCheck>('/halle/checkin', { message: this.message.trim() || null }).subscribe({
      next: check => {
        const uid = this.auth.currentUser()?.id;
        this.checks.update(list => [...list.filter(c => c.userId !== uid), check]);
        this.showForm.set(false);
        this.message = '';
        this.saving.set(false);
      },
      error: () => this.saving.set(false),
    });
  }

  checkOut(): void {
    this.api.delete('/halle/checkin').subscribe({
      next: () => {
        const uid = this.auth.currentUser()?.id;
        this.checks.update(list => list.filter(c => c.userId !== uid));
      },
    });
  }

  timeSince(dateStr: string): string {
    const mins = Math.floor((Date.now() - new Date(dateStr).getTime()) / 60_000);
    if (mins < 1)  return 'gerade eben';
    if (mins < 60) return `vor ${mins} Min.`;
    return `vor ${Math.floor(mins / 60)} Std.`;
  }

  initials(name: string): string {
    return name.split(' ').map(n => n[0] ?? '').join('').toUpperCase().slice(0, 2);
  }

  avatarColor(name: string): string {
    let h = 0;
    for (let i = 0; i < name.length; i++) h = name.charCodeAt(i) + ((h << 5) - h);
    return AVATAR_COLORS[Math.abs(h) % AVATAR_COLORS.length];
  }
}
