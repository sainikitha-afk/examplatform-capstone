// admin-dashboard.ts — Landing page for Admins after login.
// Shows platform-wide statistics: users, exams, attempts, certificates.
// CONCEPTS DEMO: signal for loading state, interpolation, *ngIf.

import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { AnalyticsApiService } from '../services/analyticsapi.service';
import { PlatformStatsModel } from '../models/analytics.model';

@Component({
  selector: 'app-admin-dashboard',
  imports: [CommonModule, RouterModule],
  templateUrl: './admin-dashboard.html',
  styleUrl: './admin-dashboard.css',
})
export class AdminDashboard implements OnInit {
  isLoading = signal(true);
  stats: PlatformStatsModel = new PlatformStatsModel();
  errorMessage: string = '';
  fullName: string = sessionStorage.getItem('fullName') ?? 'Admin';

  constructor(private analyticsService: AnalyticsApiService) {}

  ngOnInit(): void {
    this.loadPlatformStats();
  }

  loadPlatformStats(): void {
    this.isLoading.set(true);
    this.analyticsService.loadPlatformStats().subscribe({
      next: (data) => {
        this.stats = data;
        this.isLoading.set(false);
      },
      error: () => {
        this.errorMessage = 'Could not load platform statistics.';
        this.isLoading.set(false);
      }
    });
  }
}
