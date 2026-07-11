// candidate-analytics.ts — Dedicated analytics page for the candidate.
// CONCEPTS DEMO: *ngFor on best-scores table, interpolation, pipe (number).

import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { AnalyticsApiService } from '../services/analyticsapi.service';
import { CandidateDashboardModel } from '../models/analytics.model';
import { PerformanceChartModal } from '../performance-chart-modal/performance-chart-modal';

@Component({
  selector: 'app-candidate-analytics',
  standalone: true,
  imports: [CommonModule, RouterModule, PerformanceChartModal],
  templateUrl: './candidate-analytics.html',
  styleUrl: './candidate-analytics.css',
})
export class CandidateAnalytics implements OnInit {
  stats: CandidateDashboardModel = new CandidateDashboardModel();
  isLoading: boolean = true;
  errorMessage: string = '';

  showChartModal: boolean = false;
  selectedTopicScoresUrl: string | null = null;

  constructor(private analyticsService: AnalyticsApiService) {}

  openChartModal(url: string | null): void {
    if (url) {
      this.selectedTopicScoresUrl = url;
      this.showChartModal = true;
    }
  }

  closeChartModal(): void {
    this.showChartModal = false;
    this.selectedTopicScoresUrl = null;
  }

  ngOnInit(): void {
    this.analyticsService.loadCandidateDashboard().subscribe({
      next: (data) => { this.stats = data; this.isLoading = false; },
      error: () => { this.errorMessage = 'Failed to load analytics.'; this.isLoading = false; }
    });
  }
}
