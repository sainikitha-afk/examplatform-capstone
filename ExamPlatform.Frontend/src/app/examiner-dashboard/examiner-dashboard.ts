// examiner-dashboard.ts — Landing page for Examiners after login.
// Shows a quick count of their exams by status.
// CONCEPTS DEMO: signal for loading, *ngFor on exam list, interpolation.

import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { ExamApiService } from '../services/examapi.service';
import { ExamSummaryModel } from '../models/exam.model';

@Component({
  selector: 'app-examiner-dashboard',
  imports: [CommonModule, RouterModule],
  templateUrl: './examiner-dashboard.html',
  styleUrl: './examiner-dashboard.css',
})
export class ExaminerDashboard implements OnInit {
  isLoading = signal(true);
  exams: ExamSummaryModel[] = [];
  errorMessage: string = '';
  fullName: string = sessionStorage.getItem('fullName') ?? 'Examiner';

  // Pre-computed counts — set once when data loads, bound directly in template.
  // Avoids calling a filter method on every change-detection cycle.
  draftCount: number = 0;
  publishedCount: number = 0;
  resultsPublishedCount: number = 0;

  constructor(private examService: ExamApiService) {}

  ngOnInit(): void {
    this.loadMyExams();
  }

  loadMyExams(): void {
    this.isLoading.set(true);
    this.examService.loadMyExams(1).subscribe({
      next: (result) => {
        this.exams = result.items;
        this.draftCount           = this.exams.filter(e => e.examStatus === 'Draft').length;
        this.publishedCount       = this.exams.filter(e => e.examStatus === 'Published').length;
        this.resultsPublishedCount = this.exams.filter(e => e.examStatus === 'ResultsPublished').length;
        this.isLoading.set(false);
      },
      error: () => {
        this.errorMessage = 'Could not load your exams.';
        this.isLoading.set(false);
      }
    });
  }
}
