// login.ts — Login screen. Accepts email or username + password, calls AuthService,
//   stores the JWT and routes by role.
// Why it exists: the entry point for returning users to get their JWT token.
// How Angular uses it: standalone component; FormsModule enables [(ngModel)] two-way binding.
// CONCEPTS DEMO: two-way binding, event binding, interpolation, Observable subscribe.

import { Component } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { AuthService } from '../services/auth.service';
import { LoginModel } from '../models/login.model';

@Component({
  selector: 'app-login',
  imports: [FormsModule, CommonModule, RouterModule],
  templateUrl: './login.html',
  styleUrl: './login.css',
})
export class Login {
  // Two-way binding target: form inputs read/write this object.
  loginData: LoginModel = new LoginModel();

  // UI state flags.
  isLoading: boolean = false;
  errorMessage: string = '';

  // Constructor injection — Angular provides AuthService via DI.
  constructor(private authService: AuthService) {}

  // Event binding target: called when the Login button is clicked.
  handleLoginClick(): void {
    this.isLoading = true;
    this.errorMessage = '';

    // Credentials pasted from an email (e.g. the examiner temp-password mail) often
    // carry a trailing space or newline, which then fails password verification for
    // an otherwise-correct password. The backend trims too — this just makes the
    // common copy-paste case work on the first try.
    this.loginData.emailOrUsername = this.loginData.emailOrUsername?.trim() ?? '';
    this.loginData.password = this.loginData.password?.trim() ?? '';

    // Observable + subscribe pattern (concept #10).
    this.authService.login(this.loginData).subscribe({
      next: (response) => {
        this.isLoading = false;
        // AuthService handles sessionStorage, BehaviorSubject push, and routing.
        this.authService.saveSessionAndRoute(response);
      },
      error: (err: Error) => {
        this.isLoading = false;
        this.errorMessage = err.message;
      }
    });
  }
}
