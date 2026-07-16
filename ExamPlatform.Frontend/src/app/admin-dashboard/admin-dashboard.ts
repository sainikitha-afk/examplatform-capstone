// admin-dashboard.ts — Landing page for Admins after login.
// Shows platform-wide statistics: users, exams, attempts, certificates.
// CONCEPTS DEMO: signal for loading state, interpolation, *ngIf.

import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { AnalyticsApiService } from '../services/analyticsapi.service';
import { PlatformStatsModel } from '../models/analytics.model';
import { ExaminerRequestApiService } from '../services/examinerrequestapi.service';

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

  // Pending examiner applications — the one number on this page an Admin can act on
  // right now, so it gets a stat card and a badge on the Examiner Requests tile.
  pendingRequests = signal(0);

  constructor(
    private analyticsService: AnalyticsApiService,
    private requestService: ExaminerRequestApiService
  ) {}

  ngOnInit(): void {
    this.loadPlatformStats();
    // Best-effort: a failure here must not block the stats grid.
    this.requestService.getPending().subscribe({
      next: (requests) => this.pendingRequests.set(requests.length),
      error: () => this.pendingRequests.set(0),
    });
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
