// exam-result.ts — Shows the per-question breakdown after results are published.
// CONCEPTS DEMO: route parameter (:attemptId), *ngFor table, [ngClass] for pass/fail colouring.

import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, ActivatedRoute } from '@angular/router';
import { AttemptApiService } from '../services/attemptapi.service';
import { AttemptResultModel } from '../models/attempt-result.model';

@Component({
  selector: 'app-exam-result',
  imports: [CommonModule, RouterModule],
  templateUrl: './exam-result.html',
  styleUrl: './exam-result.css',
})
export class ExamResult implements OnInit {
  result: AttemptResultModel = new AttemptResultModel();
  isLoading: boolean = true;
  errorMessage: string = '';

  constructor(
    private route: ActivatedRoute,
    private attemptService: AttemptApiService
  ) {}

  ngOnInit(): void {
    const attemptId = Number(this.route.snapshot.params['attemptId']);
    this.attemptService.loadAttemptResult(attemptId).subscribe({
      next: (data) => { this.result = data; this.isLoading = false; },
      error: (err) => {
        this.errorMessage = err?.error?.message ?? 'Results are not available yet.';
        this.isLoading = false;
      }
    });
  }
}
