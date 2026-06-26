// navigation.ts — Top navigation bar: shows the current user's name, role-aware links,
//   unread notification badge, and a Logout button.
// Why it exists: every authenticated page shares this bar; putting it here avoids repeating
//   it in every component.
// How Angular uses it: included once in app.html above <router-outlet>.
// CONCEPTS DEMO:
//   - BehaviorSubject read via async pipe (currentUser$ | async)
//   - signal() for the unread notification count
//   - ngOnDestroy + Subscription.unsubscribe() to prevent memory leaks
//   - [ngClass] for active-link highlighting
//   - RouterLink / RouterLinkActive

import { Component, OnInit, OnDestroy, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { Subscription } from 'rxjs';
import { AuthService } from '../services/auth.service';
import { NotificationApiService } from '../services/notificationapi.service';
import { currentUser$ } from '../rxjs/auth.subject';
import { AuthResponseModel } from '../models/auth-response.model';

@Component({
  selector: 'app-navigation',
  imports: [CommonModule, RouterModule],
  templateUrl: './navigation.html',
  styleUrl: './navigation.css',
})
export class Navigation implements OnInit, OnDestroy {
  // Expose the BehaviorSubject to the template so the async pipe can subscribe.
  currentUser$ = currentUser$;

  // CONCEPT DEMO: signal() — unread count updated reactively, read in template with ().
  unreadCount = signal(0);

  // Track the subscription so we can cancel it in ngOnDestroy.
  private unreadSub: Subscription | null = null;

  constructor(
    private authService: AuthService,
    private notificationService: NotificationApiService
  ) {}

  ngOnInit(): void {
    // Only fetch the badge count if a user is actually logged in.
    if (this.authService.isLoggedIn()) {
      this.refreshUnreadCount();
    }
  }

  // Load unread count from the backend and update the signal.
  refreshUnreadCount(): void {
    this.unreadSub = this.notificationService.loadUnreadCount().subscribe({
      next: (result) => this.unreadCount.set(result.count),
      error: () => this.unreadCount.set(0)
    });
  }

  // Returns true when the given role matches the stored role — used for *ngIf in template.
  isRole(role: string): boolean {
    return sessionStorage.getItem('role') === role;
  }

  handleLogoutClick(): void {
    this.authService.logout();
  }

  // CONCEPT DEMO: ngOnDestroy — unsubscribe to stop any pending HTTP responses after logout.
  ngOnDestroy(): void {
    this.unreadSub?.unsubscribe();
  }
}
