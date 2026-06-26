// navigation.ts — Top navigation bar: shows the current user's name, role-aware links,
//   unread notification badge, and a Logout button.
// CONCEPTS DEMO:
//   - BehaviorSubject read via async pipe (currentUser$ | async) — template variable 'user'
//     used for role comparisons; NO method calls in template for role checks
//   - signal() for the unread notification count
//   - ngOnDestroy + Subscription.unsubscribe() to prevent memory leaks
//   - [ngClass] for role badge styling
//   - RouterLink / RouterLinkActive

import { Component, OnInit, OnDestroy, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { Subscription } from 'rxjs';
import { filter } from 'rxjs/operators';
import { AuthService } from '../services/auth.service';
import { NotificationApiService } from '../services/notificationapi.service';
import { currentUser$ } from '../rxjs/auth.subject';

@Component({
  selector: 'app-navigation',
  imports: [CommonModule, RouterModule],
  templateUrl: './navigation.html',
  styleUrl: './navigation.css',
})
export class Navigation implements OnInit, OnDestroy {
  // Expose the BehaviorSubject to the template so the async pipe can subscribe.
  // CONCEPT DEMO: async pipe subscribes here — NOT in a getter/method.
  currentUser$ = currentUser$;

  // CONCEPT DEMO: signal() — unread count updated reactively, read in template with ().
  unreadCount = signal(0);

  private userSub: Subscription | null = null;

  constructor(
    private authService: AuthService,
    private notificationService: NotificationApiService
  ) {}

  // Subscribe to the user stream so the badge reloads whenever someone logs in.
  // Using filter(Boolean) skips the null emissions (logout / initial unauthenticated state).
  ngOnInit(): void {
    this.userSub = currentUser$
      .pipe(filter(u => u !== null))
      .subscribe(() => this.refreshUnreadCount());
  }

  // Load unread count from the backend and update the signal.
  // Called once per non-null user emission (i.e., once on login / once on page refresh if
  // already logged in). HttpClient observables complete after the response — no manual
  // unsubscribe needed for this inner call.
  private refreshUnreadCount(): void {
    this.notificationService.loadUnreadCount().subscribe({
      next: (result) => this.unreadCount.set(result.count),
      error: () => this.unreadCount.set(0)
    });
  }

  handleLogoutClick(): void {
    this.authService.logout();
  }

  // CONCEPT DEMO: ngOnDestroy — unsubscribe the user stream to prevent memory leaks.
  ngOnDestroy(): void {
    this.userSub?.unsubscribe();
  }
}
