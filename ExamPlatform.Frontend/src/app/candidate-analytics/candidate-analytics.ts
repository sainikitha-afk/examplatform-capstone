// candidate-analytics.ts — Dedicated analytics page for the candidate.
// CONCEPTS DEMO: *ngFor on best-scores table, interpolation, pipe (number).

import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { AnalyticsApiService } from '../services/analyticsapi.service';
import { CandidateDashboardModel } from '../models/analytics.model';

@Component({
  selector: 'app-candidate-analytics',
  imports: [CommonModule, RouterModule],
  templateUrl: './candidate-analytics.html',
  styleUrl: './candidate-analytics.css',
})
export class CandidateAnalytics implements OnInit {
  stats: CandidateDashboardModel = new CandidateDashboardModel();
  isLoading: boolean = true;
  errorMessage: string = '';

  constructor(private analyticsService: AnalyticsApiService) {}

  ngOnInit(): void {
    this.analyticsService.loadCandidateDashboard().subscribe({
      next: (data) => { this.stats = data; this.isLoading = false; },
      error: () => { this.errorMessage = 'Failed to load analytics.'; this.isLoading = false; }
    });
  }
}
