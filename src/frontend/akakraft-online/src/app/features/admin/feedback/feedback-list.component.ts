import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { MatTableModule } from '@angular/material/table';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatChipsModule } from '@angular/material/chips';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatButtonToggleModule } from '@angular/material/button-toggle';
import { ApiService } from '../../../core/api/api.service';
import { Feedback, FeedbackStatus } from '../../../models/feedback.model';

@Component({
  selector: 'app-admin-feedback-list',
  imports: [
    DatePipe,
    MatTableModule, MatIconModule, MatButtonModule, MatChipsModule,
    MatTooltipModule, MatProgressSpinnerModule, MatButtonToggleModule,
  ],
  templateUrl: './feedback-list.component.html',
  styleUrl: './feedback-list.component.scss',
})
export class AdminFeedbackListComponent implements OnInit {
  private readonly api = inject(ApiService);
  private readonly snackBar = inject(MatSnackBar);

  readonly feedbacks = signal<Feedback[]>([]);
  readonly loading = signal(true);
  readonly filterStatus = signal<FeedbackStatus | 'all'>('all');

  readonly displayedColumns = ['createdAt', 'user', 'page', 'text', 'status', 'actions'];

  readonly filtered = computed(() => {
    const all = this.feedbacks();
    const f = this.filterStatus();
    return f === 'all' ? all : all.filter(fb => fb.status === f);
  });

  readonly counts = computed(() => ({
    all: this.feedbacks().length,
    New: this.feedbacks().filter(f => f.status === 'New').length,
    Read: this.feedbacks().filter(f => f.status === 'Read').length,
    Done: this.feedbacks().filter(f => f.status === 'Done').length,
  }));

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.api.get<Feedback[]>('/admin/feedback').subscribe({
      next: data => {
        this.feedbacks.set(data);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.snackBar.open('Feedbacks konnten nicht geladen werden.', 'OK', { duration: 3000 });
      },
    });
  }

  updateStatus(feedback: Feedback, status: FeedbackStatus): void {
    this.api.patch<Feedback>(`/admin/feedback/${feedback.id}/status`, { status }).subscribe({
      next: updated => {
        this.feedbacks.update(list => list.map(f => f.id === updated.id ? updated : f));
      },
      error: () => {
        this.snackBar.open('Status konnte nicht gespeichert werden.', 'OK', { duration: 3000 });
      },
    });
  }

  statusLabel(status: FeedbackStatus): string {
    return { New: 'Neu', Read: 'Gelesen', Done: 'Umgesetzt' }[status];
  }

  statusColor(status: FeedbackStatus): string {
    return { New: 'accent', Read: 'primary', Done: '' }[status];
  }
}
