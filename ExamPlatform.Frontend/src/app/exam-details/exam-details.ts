// exam-details.ts — Shows full exam info and a Start Exam button.
// Reads :examId from the route, fetches exam detail from the API.
// CONCEPTS DEMO: route parameter reading via ActivatedRoute, programmatic navigation.

import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, ActivatedRoute, Router } from '@angular/router';
import { ExamApiService } from '../services/examapi.service';
import { ExamDetailModel } from '../models/exam.model';

@Component({
  selector: 'app-exam-details',
  imports: [CommonModule, RouterModule],
  templateUrl: './exam-details.html',
  styleUrl: './exam-details.css',
})
export class ExamDetails implements OnInit {
  exam: ExamDetailModel = new ExamDetailModel();
  isLoading: boolean = true;
  errorMessage: string = '';
  examId: number = 0;

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private examService: ExamApiService
  ) {}

  ngOnInit(): void {
    // CONCEPT DEMO: reading a route parameter (:examId).
    this.examId = Number(this.route.snapshot.params['examId']);
    this.loadExamDetails();
  }

  loadExamDetails(): void {
    this.examService.loadExamById(this.examId).subscribe({
      next: (data) => {
        this.exam = data;
        this.isLoading = false;
      },
      error: () => {
        this.errorMessage = 'Could not load exam details.';
        this.isLoading = false;
      }
    });
  }

  // Programmatic navigation to the attempt screen, passing examId.
  handleStartExamClick(): void {
    this.router.navigate(['/attempt', this.examId]);
  }
}
