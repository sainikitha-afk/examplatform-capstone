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

  constructor(private examService: ExamApiService) {}

  ngOnInit(): void {
    this.loadMyExams();
  }

  loadMyExams(): void {
    this.isLoading.set(true);
    this.examService.loadMyExams(1).subscribe({
      next: (result) => {
        this.exams = result.items;
        this.isLoading.set(false);
      },
      error: () => {
        this.errorMessage = 'Could not load your exams.';
        this.isLoading.set(false);
      }
    });
  }

  countByStatus(status: string): number {
    return this.exams.filter(e => e.examStatus === status).length;
  }
}
