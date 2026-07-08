// manage-exams.ts — Examiner: list, create, edit exams; attach questions; publish.
// CONCEPTS DEMO: *ngFor, *ngIf view switching, event binding, two-way binding.

import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { ExamApiService } from '../services/examapi.service';
import { QuestionApiService } from '../services/questionapi.service';
import { CategoryApiService } from '../services/categoryapi.service';
import { ExamSummaryModel, ExamDetailModel, CreateExamModel, UpdateExamModel } from '../models/exam.model';
import { QuestionModel } from '../models/question.model';
import { CategoryModel } from '../models/category.model';

@Component({
  selector: 'app-manage-exams',
  imports: [CommonModule, FormsModule],
  templateUrl: './manage-exams.html',
  styleUrl: './manage-exams.css',
})
export class ManageExams implements OnInit {
  exams: ExamSummaryModel[] = [];
  selectedExam: ExamDetailModel = new ExamDetailModel();
  questionBank: QuestionModel[] = [];
  categories: CategoryModel[] = [];
  selectedQuestionIds: Set<number> = new Set();
  isLoading: boolean = false;
  isSaving: boolean = false;
  errorMessage: string = '';
  successMessage: string = '';
  view: string = 'list';   // 'list' | 'create' | 'edit' | 'questions'

  createForm: CreateExamModel = new CreateExamModel();
  updateForm: UpdateExamModel = new UpdateExamModel();
  editingExamId: number = 0;

  constructor(
    private examService: ExamApiService,
    private questionService: QuestionApiService,
    private categoryService: CategoryApiService,
    private router: Router
  ) {}

  // Opens the attempt-monitoring view for one exam.
  viewAttempts(examId: number): void {
    this.router.navigate(['/exam-attempts', examId]);
  }

  ngOnInit(): void {
    this.loadMyExams();
    // The create form needs the category dropdown populated.
    this.categoryService.loadCategories().subscribe({
      next: (cats) => this.categories = cats,
      error: () => this.categories = []
    });
  }

  loadMyExams(): void {
    this.isLoading = true;
    this.examService.loadMyExams().subscribe({
      next: (r) => { this.exams = r.items; this.isLoading = false; },
      error: () => { this.errorMessage = 'Failed to load exams.'; this.isLoading = false; }
    });
  }

  showCreateForm(): void {
    this.createForm = new CreateExamModel();
    this.view = 'create';
  }

  handleCreateClick(): void {
    if (this.isSaving) return;
    // Client-side validation mirrors the backend rules — the form must not
    // proceed with missing required fields (end time is intentionally optional).
    if (!this.createForm.title.trim()) { this.errorMessage = 'Exam title is required.'; return; }
    if (!this.createForm.catId)        { this.errorMessage = 'Please select a category.'; return; }
    if (!this.createForm.startTime)    { this.errorMessage = 'Start time is required.'; return; }
    if (this.createForm.endTime && this.createForm.endTime <= this.createForm.startTime) {
      this.errorMessage = 'End time must be after the start time.';
      return;
    }
    this.isSaving = true;
    this.errorMessage = '';
    this.examService.createExam(this.createForm).subscribe({
      next: () => { this.isSaving = false; this.successMessage = 'Exam created!'; this.view = 'list'; this.loadMyExams(); },
      error: (e) => { this.isSaving = false; this.errorMessage = e?.error?.error ?? 'Create failed.'; }
    });
  }

  showEditForm(exam: ExamSummaryModel): void {
    this.editingExamId = exam.examId;
    this.updateForm = new UpdateExamModel(
      exam.title, '', exam.durationMinutes, exam.totalMarks, exam.passingMarks,
      exam.startTime, exam.endTime
    );
    this.view = 'edit';
  }

  handleUpdateClick(): void {
    if (this.isSaving) return;
    this.isSaving = true;
    this.errorMessage = '';
    this.examService.updateExam(this.editingExamId, this.updateForm).subscribe({
      next: () => { this.isSaving = false; this.successMessage = 'Exam updated!'; this.view = 'list'; this.loadMyExams(); },
      error: (e) => { this.isSaving = false; this.errorMessage = e?.error?.error ?? 'Update failed.'; }
    });
  }

  handleDeleteClick(id: number): void {
    if (!confirm('Delete this exam?')) return;
    this.examService.deleteExam(id).subscribe({
      next: () => this.loadMyExams(),
      error: (e) => this.errorMessage = e?.error?.error ?? 'Delete failed.'
    });
  }

  showAttachQuestions(exam: ExamSummaryModel): void {
    this.editingExamId = exam.examId;
    this.selectedQuestionIds = new Set();
    this.view = 'questions';
    // Load exam detail to see already-attached questions.
    this.examService.loadExamById(exam.examId).subscribe({ next: (d) => this.selectedExam = d });
    // Load question bank.
    this.questionService.loadQuestions().subscribe({ next: (r) => this.questionBank = r.items });
  }

  toggleQuestionSelect(qId: number): void {
    this.selectedQuestionIds.has(qId) ? this.selectedQuestionIds.delete(qId) : this.selectedQuestionIds.add(qId);
  }

  handleAttachClick(): void {
    if (this.isSaving) return;
    const ids = Array.from(this.selectedQuestionIds);
    if (ids.length === 0) {
      this.errorMessage = 'Select at least one question to attach.';
      return;
    }
    this.isSaving = true;
    this.errorMessage = '';
    this.examService.attachQuestions(this.editingExamId, ids).subscribe({
      next: () => { this.isSaving = false; this.successMessage = 'Questions attached!'; this.view = 'list'; this.loadMyExams(); },
      error: (e) => { this.isSaving = false; this.errorMessage = e?.error?.error ?? 'Attach failed.'; }
    });
  }

  handleDetachClick(examId: number, questionId: number): void {
    this.examService.detachQuestion(examId, questionId).subscribe({
      next: (d) => { this.selectedExam = d; this.successMessage = 'Question detached.'; },
      error: (e) => this.errorMessage = e?.error?.error ?? 'Detach failed.'
    });
  }

  handlePublishClick(id: number): void {
    if (!confirm('Publish this exam? It will become visible to candidates.')) return;
    this.examService.publishExam(id).subscribe({
      next: () => { this.successMessage = 'Exam published!'; this.loadMyExams(); },
      error: (e) => this.errorMessage = e?.error?.error ?? 'Publish failed.'
    });
  }

  handlePublishResultsClick(id: number): void {
    if (!confirm('Publish results? Candidates will see their scores.')) return;
    this.examService.publishResults(id).subscribe({
      next: () => { this.successMessage = 'Results published!'; this.loadMyExams(); },
      error: (e) => this.errorMessage = e?.error?.error ?? 'Publish results failed.'
    });
  }
}
