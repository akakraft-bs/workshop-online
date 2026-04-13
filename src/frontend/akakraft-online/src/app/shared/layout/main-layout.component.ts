import { Component, computed, inject, OnInit, signal, viewChild } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { BreakpointObserver, Breakpoints } from '@angular/cdk/layout';
import { MatSidenav, MatSidenavModule } from '@angular/material/sidenav';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatListModule } from '@angular/material/list';
import { MatMenuModule } from '@angular/material/menu';
import { MatDividerModule } from '@angular/material/divider';
import { MatDialog } from '@angular/material/dialog';
import { MatTooltipModule } from '@angular/material/tooltip';
import { AuthService } from '../../core/auth/auth.service';
import { Role } from '../../models/user.model';
import { FeedbackDialogComponent } from '../../features/feedback/feedback-dialog/feedback-dialog.component';

interface NavItem {
  label: string;
  icon: string;
  route: string;
  requiredRoles?: Role[];
}

const ALL_NAV_ITEMS: NavItem[] = [
  { label: 'Dashboard', icon: 'dashboard', route: '/dashboard' },
  { label: 'Werkzeug', icon: 'build', route: '/werkzeug' },
  { label: 'Verbrauchsmaterial', icon: 'inventory_2', route: '/verbrauchsmaterial' },
  { label: 'Nutzerverwaltung', icon: 'manage_accounts', route: '/admin/users', requiredRoles: [Role.Admin] },
  { label: 'Feedback', icon: 'feedback', route: '/admin/feedback', requiredRoles: [Role.Admin] },
];

@Component({
  selector: 'app-main-layout',
  imports: [
    RouterOutlet, RouterLink, RouterLinkActive,
    MatSidenavModule, MatToolbarModule, MatIconModule,
    MatButtonModule, MatListModule, MatMenuModule, MatDividerModule, MatTooltipModule,
  ],
  templateUrl: './main-layout.component.html',
  styleUrl: './main-layout.component.scss',
})
export class MainLayoutComponent implements OnInit {
  private readonly auth = inject(AuthService);
  private readonly breakpointObserver = inject(BreakpointObserver);
  private readonly dialog = inject(MatDialog);

  readonly sidenavRef = viewChild.required<MatSidenav>('sidenav');
  readonly isMobile = signal(false);
  logoFailed = false;
  readonly currentUser = this.auth.currentUser;

  readonly navItems = computed(() =>
    ALL_NAV_ITEMS.filter(item =>
      !item.requiredRoles || this.auth.hasAnyRole(item.requiredRoles)
    )
  );

  ngOnInit(): void {
    this.breakpointObserver
      .observe([Breakpoints.Handset, Breakpoints.TabletPortrait])
      .subscribe(result => this.isMobile.set(result.matches));
  }

  logout(): void {
    this.auth.logout();
  }

  openFeedbackDialog(): void {
    this.dialog.open(FeedbackDialogComponent, {
      width: '480px',
      maxWidth: '95vw',
    });
  }
}
