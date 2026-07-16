// candidate-dashboard.ts — Landing page for Candidates after login.
//
// REDESIGNED to stop duplicating the Analytics page. The stat cards, best-scores table and
// radar chart all live in /my-analytics (which owns them); this dashboard is now the
// "what should I do right now?" screen:
//   • resume an in-progress attempt (the one workflow the UI previously had no way back to);
//   • an AI Coach insight card built from the FREE deterministic snapshot endpoint
//     (no Gemini call) — strengths, focus areas, and a jump into the coach;
//   • a recent-activity feed with honest states: in progress / results pending / scored.
//
// CONCEPTS DEMO: signal() for loading state, Observable subscribe, *ngFor, [ngClass].

import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { AttemptApiService } from '../services/attemptapi.service';
import { AttemptSummaryModel } from '../models/attempt.model';
import { CoachApiService } from '../services/coachapi.service';
import { CoachSnapshotModel } from '../models/coach.model';

@Component({
  selector: 'app-candidate-dashboard',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './candidate-dashboard.html',
  styleUrl: './candidate-dashboard.css',
})
export class CandidateDashboard implements OnInit {
  isLoadingActivity = signal(true);
  isLoadingInsight = signal(true);

  fullName: string = sessionStorage.getItem('fullName') ?? 'Candidate';

  // Recent attempts, newest first (max 5 shown). Includes every status — an in-progress
  // attempt here is what powers the resume banner.
  recentAttempts: AttemptSummaryModel[] = [];
  inProgressAttempt: AttemptSummaryModel | null = null;

  // The AI coach's deterministic snapshot — computed server-side from published results
  // only, no AI call involved, so it is safe to load on every dashboard visit.
  insight: CoachSnapshotModel | null = null;

  // Both loads are best-effort: if one fails the dashboard still renders the rest.
  activityError = '';

  constructor(
    private attemptService: AttemptApiService,
    private coachService: CoachApiService
  ) {}

  ngOnInit(): void {
    this.loadRecentActivity();
    this.loadInsight();
  }

  private loadRecentActivity(): void {
    this.attemptService.loadMyAttempts(1).subscribe({
      next: (page) => {
        const items = page.items ?? [];
        this.inProgressAttempt = items.find(a => a.attemptStatus === 'InProgress') ?? null;
        this.recentAttempts = items.slice(0, 5);
        this.isLoadingActivity.set(false);
      },
      error: () => {
        this.activityError = 'Could not load your recent activity.';
        this.isLoadingActivity.set(false);
      },
    });
  }

  private loadInsight(): void {
    this.coachService.loadSnapshot().subscribe({
      next: (data) => {
        this.insight = data;
        this.isLoadingInsight.set(false);
      },
      // No error banner: the insight card simply shows its explore-the-coach fallback.
      error: () => this.isLoadingInsight.set(false),
    });
  }

  // One place decides how an attempt row reads. Score/isPassed are null until the examiner
  // publishes results (the backend withholds them), which is exactly the "pending" state.
  statusOf(a: AttemptSummaryModel): 'in-progress' | 'pending' | 'passed' | 'failed' {
    if (a.attemptStatus === 'InProgress') return 'in-progress';
    if (a.score === null || a.isPassed === null) return 'pending';
    return a.isPassed ? 'passed' : 'failed';
  }

  statusLabel(a: AttemptSummaryModel): string {
    switch (this.statusOf(a)) {
      case 'in-progress': return 'In progress';
      case 'pending':     return 'Results pending';
      case 'passed':      return `Passed · ${a.score}`;
      case 'failed':      return `Failed · ${a.score}`;
    }
  }
}
