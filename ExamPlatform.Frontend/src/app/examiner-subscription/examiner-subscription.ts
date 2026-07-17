// examiner-subscription.ts — Examiner subscription dashboard (Part 6):
// status, trial progress, upgrade/decline flow, billing history, and
// Institute Verification submission (Professional-only, Part 3/7).

import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { SubscriptionApiService } from '../services/subscriptionapi.service';
import { InstitutionApiService } from '../services/institutionapi.service';
import { SubscriptionStatusModel, PaymentModel } from '../models/subscription.model';
import { InstitutionModel } from '../models/subscription.model';

@Component({
  selector: 'app-examiner-subscription',
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './examiner-subscription.html',
  styleUrl: './examiner-subscription.css',
})
export class ExaminerSubscription implements OnInit {
  status: SubscriptionStatusModel = new SubscriptionStatusModel();
  billingHistory: PaymentModel[] = [];
  institution: InstitutionModel | null = null;

  isLoading = true;
  errorMessage = '';
  isDeclining = false;

  institutionName = '';
  institutionLogoUrl = '';
  isSubmittingInstitution = false;
  institutionMessage = '';

  constructor(
    private subscriptionApi: SubscriptionApiService,
    private institutionApi: InstitutionApiService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.loadStatus();
    this.loadBillingHistory();
    this.loadInstitution();
  }

  loadStatus(): void {
    this.isLoading = true;
    this.subscriptionApi.loadMine().subscribe({
      next: (status) => { this.status = status; this.isLoading = false; },
      error: () => { this.errorMessage = 'Could not load your subscription.'; this.isLoading = false; }
    });
  }

  loadBillingHistory(): void {
    this.subscriptionApi.loadBillingHistory().subscribe({
      next: (payments) => { this.billingHistory = payments; },
      error: () => {}
    });
  }

  loadInstitution(): void {
    this.institutionApi.loadMine().subscribe({
      next: (institution) => { this.institution = institution; },
      error: () => {}
    });
  }

  get trialProgressPercent(): number {
    if (!this.status.maxExamsLifetime) return 0;
    return Math.min(100, (this.status.trialExamsUsed / this.status.maxExamsLifetime) * 100);
  }

  handleUpgradeClick(annual: boolean): void {
    this.router.navigate(['/checkout/subscription'], { queryParams: { annual } });
  }

  handleDeclineClick(): void {
    this.isDeclining = true;
    this.subscriptionApi.declineUpgrade().subscribe({
      next: () => { this.isDeclining = false; this.loadStatus(); },
      error: () => { this.isDeclining = false; }
    });
  }

  handleSubmitInstitution(): void {
    if (!this.institutionName.trim()) return;
    this.isSubmittingInstitution = true;
    this.institutionMessage = '';
    this.institutionApi.submit(this.institutionName.trim(), this.institutionLogoUrl.trim() || null).subscribe({
      next: (institution) => {
        this.institution = institution;
        this.isSubmittingInstitution = false;
        this.institutionMessage = 'Submitted for admin review.';
      },
      error: (err) => {
        this.isSubmittingInstitution = false;
        this.institutionMessage = err.error?.error || 'Could not submit institution.';
      }
    });
  }
}
