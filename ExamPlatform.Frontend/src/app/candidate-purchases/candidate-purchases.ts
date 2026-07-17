// candidate-purchases.ts — Candidate's purchased premium exams, payment
// history, and invoices (Part 6 of the design).

import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { PurchaseApiService } from '../services/purchaseapi.service';
import { CandidatePurchaseModel } from '../models/subscription.model';

@Component({
  selector: 'app-candidate-purchases',
  imports: [CommonModule, RouterModule],
  templateUrl: './candidate-purchases.html',
  styleUrl: './candidate-purchases.css',
})
export class CandidatePurchases implements OnInit {
  purchases: CandidatePurchaseModel[] = [];
  isLoading = true;
  errorMessage = '';

  constructor(private purchaseApi: PurchaseApiService) {}

  ngOnInit(): void {
    this.purchaseApi.loadMine().subscribe({
      next: (purchases) => { this.purchases = purchases; this.isLoading = false; },
      error: () => { this.errorMessage = 'Could not load your purchases.'; this.isLoading = false; }
    });
  }
}
