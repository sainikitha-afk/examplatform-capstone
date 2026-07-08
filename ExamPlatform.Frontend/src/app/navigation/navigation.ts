// navigation.ts — Sticky top navigation bar.
// Preserves ALL original logic (auth, notifications, logout) and adds:
//   • Dark/light theme toggle stored in localStorage
//   • Profile dropdown state signal
// CONCEPTS DEMO: BehaviorSubject via async pipe, signal(), ngOnDestroy cleanup.

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
  currentUser$ = currentUser$;

  // CONCEPT DEMO: signal() — values read in template with ()
  unreadCount    = signal(0);
  isDark         = signal(false);
  profileOpen    = signal(false);
  mobileMenuOpen = signal(false);

  private userSub: Subscription | null = null;

  constructor(
    private authService: AuthService,
    private notificationService: NotificationApiService
  ) {
    // Restore saved theme from localStorage on component creation.
    const saved = localStorage.getItem('examhub-theme');
    const dark  = saved === 'dark';
    this.isDark.set(dark);
    this.applyTheme(dark ? 'dark' : 'light');
  }

  // Sets BOTH theme attributes on <html>:
  //   data-theme    → our own CSS variables (styles.css)
  //   data-bs-theme → Bootstrap 5.3's built-in dark mode (tables, modals,
  //                   dropdowns, form controls would otherwise keep black text)
  private applyTheme(theme: string): void {
    document.documentElement.setAttribute('data-theme', theme);
    document.documentElement.setAttribute('data-bs-theme', theme);
  }

  ngOnInit(): void {
    this.userSub = currentUser$
      .pipe(filter(u => u !== null))
      .subscribe(() => this.refreshUnreadCount());
  }

  private refreshUnreadCount(): void {
    this.notificationService.loadUnreadCount().subscribe({
      next:  (result) => this.unreadCount.set(result.count),
      error: ()       => this.unreadCount.set(0)
    });
  }

  // Toggle between light and dark themes and persist the choice.
  toggleTheme(): void {
    const dark = !this.isDark();
    this.isDark.set(dark);
    const theme = dark ? 'dark' : 'light';
    this.applyTheme(theme);
    localStorage.setItem('examhub-theme', theme);
  }

  toggleProfile(): void   { this.profileOpen.update(v => !v); }
  closeProfle():   void   { this.profileOpen.set(false); }
  toggleMobile():  void   { this.mobileMenuOpen.update(v => !v); }
  closeMobile():   void   { this.mobileMenuOpen.set(false); }

  handleLogoutClick(): void {
    this.profileOpen.set(false);
    this.authService.logout();
  }

  ngOnDestroy(): void {
    this.userSub?.unsubscribe();
  }
}
