// change-password.ts — Self-service password change (any signed-in role).
//
// The missing half of examiner onboarding: the approval email sends a temporary
// password and says "change it after your first login" — this screen is where that
// finally happens. Candidates and admins use it the same way.
//
// CONCEPTS DEMO: template-driven validation, two-way binding, Observable subscribe.

import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { AuthService } from '../services/auth.service';

@Component({
  selector: 'app-change-password',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './change-password.html',
  styleUrl: './change-password.css',
})
export class ChangePassword {
  currentPassword = '';
  newPassword = '';
  confirmPassword = '';

  isLoading = false;
  errorMessage = '';
  successMessage = '';

  constructor(private authService: AuthService) {}

  // Mirrors the backend rules so most mistakes are caught before a request is sent.
  get newTooShort(): boolean {
    return this.newPassword.trim().length > 0 && this.newPassword.trim().length < 6;
  }

  get confirmMismatch(): boolean {
    return this.confirmPassword.length > 0 && this.newPassword.trim() !== this.confirmPassword.trim();
  }

  get sameAsCurrent(): boolean {
    return this.newPassword.trim().length > 0 &&
           this.newPassword.trim() === this.currentPassword.trim();
  }

  get canSubmit(): boolean {
    return !this.isLoading &&
           this.currentPassword.trim().length > 0 &&
           this.newPassword.trim().length >= 6 &&
           !this.confirmMismatch &&
           !this.sameAsCurrent;
  }

  handleSubmit(): void {
    if (!this.canSubmit) return;

    this.isLoading = true;
    this.errorMessage = '';
    this.successMessage = '';

    this.authService.changePassword(this.currentPassword, this.newPassword).subscribe({
      next: () => {
        this.isLoading = false;
        this.successMessage = 'Password changed successfully. Use the new password next time you sign in.';
        // Clear the form — the old values have no business staying on screen.
        this.currentPassword = '';
        this.newPassword = '';
        this.confirmPassword = '';
      },
      error: (err: Error) => {
        this.isLoading = false;
        this.errorMessage = err.message;
      },
    });
  }
}
