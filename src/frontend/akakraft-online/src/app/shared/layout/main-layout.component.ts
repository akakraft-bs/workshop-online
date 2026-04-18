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
import { ProfileDialogComponent } from '../../features/profile/profile-dialog.component';
import { PushNotificationService } from '../../core/push/push-notification.service';
import { PushPromptDialogComponent } from '../../features/push/push-prompt-dialog.component';
import { IosInstallBannerComponent } from '../../features/push/ios-install-banner.component';
import { AndroidInstallBannerComponent } from '../../features/push/android-install-banner.component';

interface NavItem {
  label: string;
  icon: string;
  route: string;
  requiredRoles?: Role[];
}

const ALL_NAV_ITEMS: NavItem[] = [
  { label: 'Dashboard', icon: 'dashboard', route: '/dashboard' },
  { label: 'Hallenbelegung', icon: 'calendar_month', route: '/kalender' },
  { label: 'Veranstaltungen', icon: 'celebration', route: '/veranstaltungen' },
  { label: 'Werkzeug', icon: 'build', route: '/werkzeug' },
  { label: 'Verbrauchsmaterial', icon: 'inventory_2', route: '/verbrauchsmaterial' },
  { label: 'Mängelmelder', icon: 'report_problem', route: '/mangel' },
  { label: 'Wunschliste', icon: 'playlist_add', route: '/wunsch' },
  { label: 'Umfragen', icon: 'poll', route: '/umfrage' },
  { label: 'Nutzerverwaltung', icon: 'manage_accounts', route: '/admin/users', requiredRoles: [Role.Admin] },
  { label: 'Kalender-Einstellungen', icon: 'tune', route: '/admin/kalender', requiredRoles: [Role.Admin] },
  { label: 'Feedback', icon: 'feedback', route: '/admin/feedback', requiredRoles: [Role.Admin] },
  { label: 'Test-Benachrichtigung', icon: 'notifications_active', route: '/admin/push', requiredRoles: [Role.Admin] },
];

@Component({
  selector: 'app-main-layout',
  imports: [
    RouterOutlet, RouterLink, RouterLinkActive,
    MatSidenavModule, MatToolbarModule, MatIconModule,
    MatButtonModule, MatListModule, MatMenuModule, MatDividerModule, MatTooltipModule,
    IosInstallBannerComponent,
    AndroidInstallBannerComponent,
  ],
  templateUrl: './main-layout.component.html',
  styleUrl: './main-layout.component.scss',
})
export class MainLayoutComponent implements OnInit {
  private readonly auth = inject(AuthService);
  private readonly breakpointObserver = inject(BreakpointObserver);
  private readonly dialog = inject(MatDialog);
  private readonly push = inject(PushNotificationService);

  readonly sidenavRef = viewChild.required<MatSidenav>('sidenav');
  readonly isMobile = signal(false);
  readonly showIosBanner = signal(false);
  readonly showAndroidBanner = signal(false);
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

    // Push-Prompt nach kurzer Verzögerung zeigen damit die UI settled ist
    setTimeout(() => this.checkPushPrompt(), 1200);
  }

  private checkPushPrompt(): void {
    if (!this.auth.hasAccess()) return;

    if (this.push.shouldShowIosInstallHint()) {
      this.showIosBanner.set(true);
      return;
    }

    if (this.push.shouldShowAndroidInstallPrompt()) {
      this.showAndroidBanner.set(true);
      return;
    }

    if (this.push.shouldShowPushPrompt()) {
      this.dialog.open(PushPromptDialogComponent, {
        width: '380px',
        maxWidth: '95vw',
        disableClose: true,
      });
    }
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

  openProfileDialog(): void {
    this.dialog.open(ProfileDialogComponent, {
      width: '440px',
      maxWidth: '95vw',
    });
  }
}
