// candidate-dashboard.ts — Landing page for Candidates after login.
// Shows personal analytics: attempts, pass rate, certificates earned.
// CONCEPTS DEMO: signal() for loading state, Observable subscribe, interpolation, *ngIf.

import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { AnalyticsApiService } from '../services/analyticsapi.service';
import { CandidateDashboardModel } from '../models/analytics.model';
import { PerformanceChartModal } from '../performance-chart-modal/performance-chart-modal';

@Component({
  selector: 'app-candidate-dashboard',
  standalone: true,
  imports: [CommonModule, RouterModule, PerformanceChartModal],
  templateUrl: './candidate-dashboard.html',
  styleUrl: './candidate-dashboard.css',
})
export class CandidateDashboard implements OnInit {
  // CONCEPT DEMO: signal — tracks loading state; template reads with isLoading().
  isLoading = signal(true);

  stats: CandidateDashboardModel = new CandidateDashboardModel();
  errorMessage: string = '';
  fullName: string = sessionStorage.getItem('fullName') ?? 'Candidate';

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
    this.loadDashboard();
  }

  loadDashboard(): void {
    this.isLoading.set(true);
    this.analyticsService.loadCandidateDashboard().subscribe({
      next: (data) => {
        this.stats = data;
        this.isLoading.set(false);
      },
      error: () => {
        this.errorMessage = 'Could not load dashboard data.';
        this.isLoading.set(false);
      }
    });
  }
}
