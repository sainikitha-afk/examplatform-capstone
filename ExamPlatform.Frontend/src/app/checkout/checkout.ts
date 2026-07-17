// checkout.ts — Fake Payment Gateway checkout screen (Part 5 of the design).
// Handles two contexts via the route:
//   /checkout/subscription?annual=true|false   — examiner Professional upgrade
//   /checkout/exam/:examId                     — candidate premium exam purchase
// No real gateway is ever contacted — the backend simulates the outcome from
// the card number (see DEMO_CARDS) or the explicit failure-simulation toggles.

import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { SubscriptionApiService } from '../services/subscriptionapi.service';
import { PurchaseApiService } from '../services/purchaseapi.service';
import { CouponApiService } from '../services/couponapi.service';
import { ExamApiService } from '../services/examapi.service';
import { ChargeRequestModel, DEMO_CARDS, CouponValidationResultModel } from '../models/subscription.model';
import { ExamDetailModel } from '../models/exam.model';

@Component({
  selector: 'app-checkout',
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './checkout.html',
  styleUrl: './checkout.css',
})
export class Checkout implements OnInit {
  context: 'subscription' | 'exam' = 'subscription';
  examId = 0;
  annual = false;

  exam: ExamDetailModel | null = null;
  isLoadingContext = true;

  method = 'CreditCard';
  cardNumber = '';
  couponCode = '';
  simulateNetworkFailure = false;
  simulateTimeout = false;

  couponResult: CouponValidationResultModel | null = null;
  isValidatingCoupon = false;

  isProcessing = false;
  errorMessage = '';

  demoCards = DEMO_CARDS;

  // Generated once per checkout attempt; resent unchanged if the SAME
  // attempt needs to be resubmitted. A brand-new attempt (new page load,
  // or explicitly starting over) gets a fresh one — see ngOnInit.
  private idempotencyKey = crypto.randomUUID();

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private subscriptionApi: SubscriptionApiService,
    private purchaseApi: PurchaseApiService,
    private couponApi: CouponApiService,
    private examApi: ExamApiService
  ) {}

  ngOnInit(): void {
    const examIdParam = this.route.snapshot.params['examId'];
    if (examIdParam) {
      this.context = 'exam';
      this.examId = Number(examIdParam);
      this.examApi.loadExamById(this.examId).subscribe({
        next: (exam) => { this.exam = exam; this.isLoadingContext = false; },
        error: () => { this.errorMessage = 'Could not load exam details.'; this.isLoadingContext = false; }
      });
    } else {
      this.context = 'subscription';
      this.annual = this.route.snapshot.queryParams['annual'] === 'true';
      this.isLoadingContext = false;
    }
  }

  get couponScope(): string {
    return this.context === 'subscription' ? 'SubscriptionOnly' : 'ExamPurchaseOnly';
  }

  handleValidateCouponClick(): void {
    if (!this.couponCode.trim()) return;
    this.isValidatingCoupon = true;
    this.couponResult = null;
    this.couponApi.validate(this.couponCode.trim(), this.couponScope).subscribe({
      next: (result) => { this.couponResult = result; this.isValidatingCoupon = false; },
      error: () => { this.isValidatingCoupon = false; }
    });
  }

  get canSubmit(): boolean {
    const cleaned = this.cardNumber.replace(/[\s-]/g, '');
    return cleaned.length >= 12 && cleaned.length <= 19 && !this.isProcessing;
  }

  handleSubmit(): void {
    if (!this.canSubmit) return;
    this.isProcessing = true;
    this.errorMessage = '';

    const request = new ChargeRequestModel(
      this.method, this.cardNumber.trim(), this.idempotencyKey,
      this.annual, this.examId, this.couponCode.trim() || null,
      this.simulateNetworkFailure, this.simulateTimeout
    );

    const charge$ = this.context === 'subscription'
      ? this.subscriptionApi.upgrade(request)
      : this.purchaseApi.purchase(request);

    charge$.subscribe({
      next: (payment) => {
        this.isProcessing = false;
        this.router.navigate(['/payment-success', payment.paymentId], { state: { payment } });
      },
      error: (err) => {
        this.isProcessing = false;
        this.errorMessage = err.error?.error || 'Could not process payment. Please try again.';
      }
    });
  }
}
