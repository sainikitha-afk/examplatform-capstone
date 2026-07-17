// admin-revenue-dashboard.ts — Admin Revenue Dashboard (Part 7): monthly
// revenue trend, plan distribution, top purchased exams, recent
// transactions, conversion/failure metrics.

import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { BaseChartDirective } from 'ng2-charts';
import { ChartConfiguration } from 'chart.js';
import { RevenueApiService } from '../services/revenueapi.service';
import { RevenueSummaryModel } from '../models/subscription.model';

@Component({
  selector: 'app-admin-revenue-dashboard',
  imports: [CommonModule, BaseChartDirective],
  templateUrl: './admin-revenue-dashboard.html',
  styleUrl: './admin-revenue-dashboard.css',
})
export class AdminRevenueDashboard implements OnInit {
  summary: RevenueSummaryModel = new RevenueSummaryModel();
  isLoading = true;
  errorMessage = '';

  monthlyChartData: ChartConfiguration<'bar'>['data'] = { labels: [], datasets: [] };
  monthlyChartOptions: ChartConfiguration<'bar'>['options'] = {
    responsive: true,
    plugins: { legend: { display: false } }
  };

  planChartData: ChartConfiguration<'doughnut'>['data'] = { labels: [], datasets: [] };
  planChartOptions: ChartConfiguration<'doughnut'>['options'] = {
    responsive: true,
    plugins: { legend: { position: 'bottom' } }
  };

  constructor(private revenueApi: RevenueApiService) {}

  ngOnInit(): void {
    this.revenueApi.loadSummary().subscribe({
      next: (summary) => {
        this.summary = summary;
        this.buildCharts();
        this.isLoading = false;
      },
      error: () => { this.errorMessage = 'Could not load revenue data.'; this.isLoading = false; }
    });
  }

  private buildCharts(): void {
    this.monthlyChartData = {
      labels: this.summary.monthlyRevenue.map(m => m.month),
      datasets: [{
        label: 'Revenue (₹)',
        data: this.summary.monthlyRevenue.map(m => m.revenue),
        backgroundColor: '#0d9488'
      }]
    };

    this.planChartData = {
      labels: this.summary.planDistribution.map(p => p.status),
      datasets: [{
        data: this.summary.planDistribution.map(p => p.count),
        backgroundColor: ['#0d9488', '#14b8a6', '#f59e0b', '#ef4444', '#06b6d4', '#94a3b8']
      }]
    };
  }
}
