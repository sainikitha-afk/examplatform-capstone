// issue-reports.ts — Examiner triage inbox for candidate-raised issue reports.
// Candidates flag problems during an exam ("this question has two right answers",
// "the timer jumped"); each report notifies the examiner and lands here so they
// can review, resolve, and — if it's a genuine technical fault — email the admin.
// CONCEPTS DEMO: signal() for loading, constructor injection, .subscribe({next,error}).

import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { IssueReportApiService } from '../services/issuereportapi.service';
import { IssueReportModel } from '../models/issue-report.model';

@Component({
  selector: 'app-issue-reports',
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './issue-reports.html',
  styleUrl: './issue-reports.css',
})
export class IssueReports implements OnInit {
  isLoading = signal(true);
  reports: IssueReportModel[] = [];
  errorMessage = '';

  // Filter toggle: show only still-open reports (default) or everything.
  openOnly = true;
  openCount = signal(0);

  // Per-row resolution note being typed, keyed by report id.
  resolutionNotes: Record<number, string> = {};
  // Which report rows are mid-update (disables their buttons).
  updatingIds = new Set<number>();

  constructor(private issueApi: IssueReportApiService) {}

  ngOnInit(): void {
    this.loadReports();
  }

  loadReports(): void {
    this.isLoading.set(true);
    this.errorMessage = '';
    this.issueApi.loadForExaminer(1, this.openOnly).subscribe({
      next: result => {
        this.reports = result.items;
        this.isLoading.set(false);
      },
      error: () => {
        this.errorMessage = 'Could not load issue reports. Please try again.';
        this.isLoading.set(false);
      }
    });
    // Keep the open-count badge fresh independently of the current filter.
    this.issueApi.loadOpenCount().subscribe({
      next: res => this.openCount.set(res.count),
      error: () => {}
    });
  }

  // Flip the open-only filter and reload.
  toggleFilter(openOnly: boolean): void {
    this.openOnly = openOnly;
    this.loadReports();
  }

  // Mark a report Reviewed (acknowledged) or Resolved (handled), with an
  // optional note the candidate will see on resolution.
  updateStatus(report: IssueReportModel, status: string): void {
    this.updatingIds.add(report.issueReportId);
    const note = this.resolutionNotes[report.issueReportId] ?? null;
    this.issueApi.updateStatus(report.issueReportId, status, note).subscribe({
      next: updated => {
        this.updatingIds.delete(report.issueReportId);
        // Reflect the new status in place; drop it if the open-only filter no
        // longer matches so the list stays honest without a full reload.
        if (this.openOnly && updated.status !== 'Open') {
          this.reports = this.reports.filter(r => r.issueReportId !== report.issueReportId);
        } else {
          report.status = updated.status;
          report.resolvedAt = updated.resolvedAt;
          report.resolutionNote = updated.resolutionNote;
        }
        this.issueApi.loadOpenCount().subscribe({
          next: res => this.openCount.set(res.count),
          error: () => {}
        });
      },
      error: () => {
        this.updatingIds.delete(report.issueReportId);
        this.errorMessage = 'Could not update that report. Please try again.';
      }
    });
  }

  isUpdating(id: number): boolean {
    return this.updatingIds.has(id);
  }

  // Bootstrap badge colour per status.
  statusClass(status: string): string {
    if (status === 'Open') return 'bg-danger';
    if (status === 'Reviewed') return 'bg-warning text-dark';
    return 'bg-success';
  }
}
