import { Component, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { AuthService } from '../../core/auth/auth.service';

interface QuickLink {
  label: string;
  description: string;
  icon: string;
  route: string;
}

const QUICK_LINKS: QuickLink[] = [
  { label: 'Werkzeug', description: 'Werkzeug einsehen und ausleihen', icon: 'build', route: '/werkzeug' },
  { label: 'Verbrauchsmaterial', description: 'Aktuellen Bestand einsehen', icon: 'inventory_2', route: '/verbrauchsmaterial' },
];

@Component({
  selector: 'app-dashboard',
  imports: [RouterLink, MatCardModule, MatButtonModule, MatIconModule],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.scss',
})
export class DashboardComponent {
  readonly auth = inject(AuthService);
  readonly quickLinks = QUICK_LINKS;
}
