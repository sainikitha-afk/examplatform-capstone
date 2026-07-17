// payment-success.ts — Outcome screen after checkout (Part 5). Reads the
// Payment object passed via router state from Checkout (no extra GET is
// needed — the charge response already carries everything, including the
// invoice on success). Renders success or a specific failure state, both
// with the real fake transaction id/reference so the flow feels authentic.

import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterModule } from '@angular/router';
import { AuthService } from '../services/auth.service';
import { PaymentModel } from '../models/subscription.model';

@Component({
  selector: 'app-payment-success',
  imports: [CommonModule, RouterModule],
  templateUrl: './payment-success.html',
  styleUrl: './payment-success.css',
})
export class PaymentSuccess implements OnInit {
  payment: PaymentModel | null = null;

  constructor(
    private router: Router,
    private authService: AuthService
  ) {}

  ngOnInit(): void {
    // Angular's router.navigate(..., { state }) stores the state via the
    // browser History API — history.state is the reliable read after
    // navigation has already completed (by the time ngOnInit runs).
    this.payment = history.state?.['payment'] ?? null;
  }

  get isSuccess(): boolean {
    return this.payment?.status === 'Success';
  }

  get failureLabel(): string {
    switch (this.payment?.status) {
      case 'Failed': return 'Payment Declined';
      case 'Cancelled': return 'Payment Cancelled';
      case 'Expired': return 'Card Expired';
      case 'NetworkFailure': return 'Network Error';
      case 'Timeout': return 'Gateway Timeout';
      default: return 'Payment Not Completed';
    }
  }

  handleContinueClick(): void {
    const role = this.authService.getRole();
    if (role === 'Examiner') this.router.navigate(['/examiner/subscription']);
    else if (role === 'Candidate') this.router.navigate(['/my-certificates']);
    else this.router.navigate(['/']);
  }

  handleRetryClick(): void {
    // A retry is a NEW checkout attempt — go back to browse/pricing rather
    // than resubmitting the same (already-decided) idempotency key.
    const role = this.authService.getRole();
    if (role === 'Examiner') this.router.navigate(['/examiner/subscription']);
    else this.router.navigate(['/browse-exams']);
  }
}
