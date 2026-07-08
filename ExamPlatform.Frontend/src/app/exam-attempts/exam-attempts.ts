// exam-attempts.ts — Examiner monitoring view for a single exam.
// Shows every attempt (all statuses): who took it, live answered-count progress,
// disconnect signals (in-progress attempts that stopped moving, auto-submits),
// scores and pass/fail. Reached from Manage Exams via the "View Attempts" button.

import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, ActivatedRoute } from '@angular/router';
import { ExamApiService } from '../services/examapi.service';
import { ExamAttemptMonitorModel } from '../models/attempt.model';
import { ExamDetailModel } from '../models/exam.model';

@Component({
  selector: 'app-exam-attempts',
  imports: [CommonModule, RouterModule],
  templateUrl: './exam-attempts.html',
  styleUrl: './exam-attempts.css',
})
export class ExamAttempts implements OnInit {
  examId: number = 0;
  exam: ExamDetailModel = new ExamDetailModel();
  attempts: ExamAttemptMonitorModel[] = [];

  isLoading = signal(true);
  isRefreshing = signal(false);
  errorMessage: string = '';

  // Summary counts, computed once per load.
  totalCount: number = 0;
  inProgressCount: number = 0;
  submittedCount: number = 0;
  autoSubmittedCount: number = 0;
  passedCount: number = 0;

  constructor(
    private route: ActivatedRoute,
    private examService: ExamApiService
  ) {}

  ngOnInit(): void {
    this.examId = Number(this.route.snapshot.params['examId']);
    this.loadAll();
  }

  loadAll(): void {
    this.isLoading.set(true);
    this.errorMessage = '';

    this.examService.loadExamById(this.examId).subscribe({
      next: (exam) => this.exam = exam,
      error: () => {}   // title is cosmetic here; the attempts call reports real errors
    });

    this.examService.loadExamAttempts(this.examId).subscribe({
      next: (attempts) => {
        this.applyAttempts(attempts);
        this.isLoading.set(false);
      },
      error: (err) => {
        this.errorMessage = err?.error?.message ?? 'Could not load attempts for this exam.';
        this.isLoading.set(false);
      }
    });
  }

  // Lightweight refresh that keeps the table on screen (no full-page spinner).
  handleRefreshClick(): void {
    if (this.isRefreshing()) return;
    this.isRefreshing.set(true);
    this.examService.loadExamAttempts(this.examId).subscribe({
      next: (attempts) => {
        this.applyAttempts(attempts);
        this.isRefreshing.set(false);
      },
      error: () => this.isRefreshing.set(false)
    });
  }

  private applyAttempts(attempts: ExamAttemptMonitorModel[]): void {
    this.attempts = attempts;
    this.totalCount = attempts.length;
    this.inProgressCount   = attempts.filter(a => a.attemptStatus === 'InProgress').length;
    this.submittedCount    = attempts.filter(a => a.attemptStatus === 'Submitted').length;
    this.autoSubmittedCount = attempts.filter(a => a.attemptStatus === 'AutoSubmitted').length;
    this.passedCount       = attempts.filter(a => a.isPassed === true).length;
  }

  formatDuration(seconds: number | null): string {
    if (seconds == null) return '—';
    const m = Math.floor(seconds / 60);
    const s = seconds % 60;
    return `${m}m ${s}s`;
  }

  statusBadgeClass(status: string): string {
    switch (status) {
      case 'InProgress':    return 'badge-pending';
      case 'Submitted':     return 'badge-published';
      case 'AutoSubmitted': return 'badge-failed';
      default:              return 'badge-draft';
    }
  }

  statusLabel(status: string): string {
    // AutoSubmitted deserves an explanation — it usually means the candidate
    // disconnected and the server timer closed the attempt.
    return status === 'AutoSubmitted' ? 'Auto-submitted (timed out)' : status;
  }
}
