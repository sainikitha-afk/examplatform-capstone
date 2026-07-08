// publish-results.ts — Quick screen for examiner to publish results for a specific exam.
// CONCEPTS DEMO: *ngFor on published exams, event binding, *ngIf.

import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ExamApiService } from '../services/examapi.service';
import { ExamSummaryModel } from '../models/exam.model';

@Component({
  selector: 'app-publish-results',
  imports: [CommonModule],
  templateUrl: './publish-results.html',
  styleUrl: './publish-results.css',
})
export class PublishResults implements OnInit {
  exams: ExamSummaryModel[] = [];
  isLoading: boolean = false;
  successMessage: string = '';
  errorMessage: string = '';

  constructor(private examService: ExamApiService) {}

  ngOnInit(): void {
    this.isLoading = true;
    // Load only Published exams (results not yet published).
    this.examService.loadMyExams().subscribe({
      next: (r) => {
        this.exams = r.items.filter(e => e.examStatus === 'Published');
        this.isLoading = false;
      },
      error: () => { this.errorMessage = 'Failed to load exams.'; this.isLoading = false; }
    });
  }

  handlePublishResultsClick(id: number, title: string): void {
    if (!confirm(`Publish results for "${title}"?`)) return;
    this.examService.publishResults(id).subscribe({
      next: () => {
        this.successMessage = `Results published for "${title}".`;
        this.exams = this.exams.filter(e => e.examId !== id);
      },
      error: (e) => this.errorMessage = e?.error?.error ?? 'Failed to publish results.'
    });
  }
}
