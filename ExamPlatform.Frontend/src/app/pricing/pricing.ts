// pricing.ts — Public pricing page (Part 1/6). Anyone can view it; the CTA
// on the Professional card adapts to whether the visitor is signed in and,
// if so, which role they hold (only Examiners can subscribe).

import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterModule } from '@angular/router';
import { SubscriptionApiService } from '../services/subscriptionapi.service';
import { AuthService } from '../services/auth.service';
import { SubscriptionPlanModel } from '../models/subscription.model';

@Component({
  selector: 'app-pricing',
  imports: [CommonModule, RouterModule],
  templateUrl: './pricing.html',
  styleUrl: './pricing.css',
})
export class Pricing implements OnInit {
  plans: SubscriptionPlanModel[] = [];
  isLoading = true;
  errorMessage = '';

  constructor(
    private subscriptionApi: SubscriptionApiService,
    private authService: AuthService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.subscriptionApi.loadPlans().subscribe({
      next: (plans) => {
        this.plans = plans;
        this.isLoading = false;
      },
      error: () => {
        this.errorMessage = 'Could not load pricing plans.';
        this.isLoading = false;
      }
    });
  }

  get isLoggedIn(): boolean {
    return this.authService.isLoggedIn();
  }

  get isExaminer(): boolean {
    return this.authService.getRole() === 'Examiner';
  }

  planByTier(tier: string): SubscriptionPlanModel | undefined {
    return this.plans.find(p => p.tier === tier);
  }

  handleProfessionalCtaClick(): void {
    if (!this.isLoggedIn) {
      this.router.navigate(['/login']);
    } else if (this.isExaminer) {
      this.router.navigate(['/examiner/subscription']);
    }
  }
}
