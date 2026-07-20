// browse-exams.ts — Lists all published exams. Candidates browse categories
// via clickable "board" cards, each showing the category's name, description
// and how many published exams are inside it.
// CONCEPTS DEMO: *ngFor, *ngIf, event binding on category boards, programmatic navigation.

import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, Router } from '@angular/router';
import { ExamApiService } from '../services/examapi.service';
import { CategoryApiService } from '../services/categoryapi.service';
import { ExamSummaryModel } from '../models/exam.model';
import { CategoryModel } from '../models/category.model';
import { CategoryBoard } from '../category-board/category-board';

@Component({
  selector: 'app-browse-exams',
  imports: [CommonModule, RouterModule, CategoryBoard],
  templateUrl: './browse-exams.html',
  styleUrl: './browse-exams.css',
})
export class BrowseExams implements OnInit {
  exams: ExamSummaryModel[] = [];
  categories: CategoryModel[] = [];
  selectedCategoryId: number | null = null;
  currentPage: number = 1;
  totalPages: number = 1;
  // Total published exams matching the current filter — shown in the header.
  totalCount: number = 0;
  isLoading: boolean = false;
  errorMessage: string = '';

  constructor(
    private examService: ExamApiService,
    private categoryService: CategoryApiService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.loadCategories();
    this.loadExams();
  }

  loadCategories(): void {
    this.categoryService.loadCategories().subscribe({
      next: (cats) => this.categories = cats,
      error: () => {}
    });
  }

  loadExams(): void {
    this.isLoading = true;
    this.examService.loadPublishedExams(this.selectedCategoryId, this.currentPage).subscribe({
      next: (result) => {
        this.exams = result.items;
        this.totalPages = result.totalPages;
        this.totalCount = result.totalCount;
        this.isLoading = false;
      },
      error: () => {
        this.errorMessage = 'Could not load exams.';
        this.isLoading = false;
      }
    });
  }

  // Event binding: fires when the category dropdown changes.
  handleCategoryChange(): void {
    this.currentPage = 1;
    this.loadExams();
  }

  // Fires when a category board card is clicked (null = "All Categories").
  handleBoardSelect(catId: number | null): void {
    this.selectedCategoryId = catId;
    this.handleCategoryChange();
  }

  handlePageChange(page: number): void {
    this.currentPage = page;
    this.loadExams();
  }

  // Programmatic navigation to exam details.
  viewExamDetails(examId: number): void {
    this.router.navigate(['/exam-details', examId]);
  }
}
