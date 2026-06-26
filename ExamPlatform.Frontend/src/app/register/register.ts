// register.ts — Candidate self-registration screen.
// Why it exists: new candidates create their own account here (no Admin involvement).
// How Angular uses it: standalone component with FormsModule for template-driven validation.
// CONCEPTS DEMO: two-way binding, template-driven validation (required/minlength),
//   showing validation errors, Observable subscribe, auto-login on success.

import { Component } from '@angular/core';
import { FormsModule, NgForm } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { AuthService } from '../services/auth.service';
import { RegisterModel } from '../models/register.model';

@Component({
  selector: 'app-register',
  imports: [FormsModule, CommonModule, RouterModule],
  templateUrl: './register.html',
  styleUrl: './register.css',
})
export class Register {
  // Two-way binding target for all form inputs.
  registerData: RegisterModel = new RegisterModel();

  isLoading: boolean = false;
  errorMessage: string = '';
  successMessage: string = '';

  constructor(private authService: AuthService) {}

  // handleRegisterClick — called on form submit. Validates the form first.
  handleRegisterClick(form: NgForm): void {
    if (form.invalid) {
      // Mark all controls as touched so validation messages show.
      Object.values(form.controls).forEach(ctrl => ctrl.markAsTouched());
      return;
    }

    this.isLoading = true;
    this.errorMessage = '';

    this.authService.register(this.registerData).subscribe({
      next: (response) => {
        this.isLoading = false;
        this.successMessage = `Welcome, ${response.fullName}! Logging you in...`;
        // Auto-login: the register endpoint returns a token — no second login call needed.
        setTimeout(() => this.authService.saveSessionAndRoute(response), 1000);
      },
      error: (err: Error) => {
        this.isLoading = false;
        this.errorMessage = err.message;
      }
    });
  }
}
