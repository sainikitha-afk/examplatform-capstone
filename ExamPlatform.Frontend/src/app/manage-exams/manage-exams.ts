// manage-exams.ts — Examiner: list, create, edit exams; attach questions; publish.
// CONCEPTS DEMO: *ngFor, *ngIf view switching, event binding, two-way binding.

import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ExamApiService } from '../services/examapi.service';
import { QuestionApiService } from '../services/questionapi.service';
import { ExamSummaryModel, ExamDetailModel, CreateExamModel, UpdateExamModel } from '../models/exam.model';
import { QuestionModel } from '../models/question.model';

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
  selectedQuestionIds: Set<number> = new Set();
  isLoading: boolean = false;
  errorMessage: string = '';
  successMessage: string = '';
  view: string = 'list';   // 'list' | 'create' | 'edit' | 'questions'

  createForm: CreateExamModel = new CreateExamModel();
  updateForm: UpdateExamModel = new UpdateExamModel();
  editingExamId: number = 0;

  constructor(
    private examService: ExamApiService,
    private questionService: QuestionApiService
  ) {}

  ngOnInit(): void { this.loadMyExams(); }

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
    this.examService.createExam(this.createForm).subscribe({
      next: () => { this.successMessage = 'Exam created!'; this.view = 'list'; this.loadMyExams(); },
      error: (e) => this.errorMessage = e?.error?.message ?? 'Create failed.'
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
    this.examService.updateExam(this.editingExamId, this.updateForm).subscribe({
      next: () => { this.successMessage = 'Exam updated!'; this.view = 'list'; this.loadMyExams(); },
      error: (e) => this.errorMessage = e?.error?.message ?? 'Update failed.'
    });
  }

  handleDeleteClick(id: number): void {
    if (!confirm('Delete this exam?')) return;
    this.examService.deleteExam(id).subscribe({
      next: () => this.loadMyExams(),
      error: (e) => this.errorMessage = e?.error?.message ?? 'Delete failed.'
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
    const ids = Array.from(this.selectedQuestionIds);
    this.examService.attachQuestions(this.editingExamId, ids).subscribe({
      next: () => { this.successMessage = 'Questions attached!'; this.view = 'list'; this.loadMyExams(); },
      error: (e) => this.errorMessage = e?.error?.message ?? 'Attach failed.'
    });
  }

  handleDetachClick(examId: number, questionId: number): void {
    this.examService.detachQuestion(examId, questionId).subscribe({
      next: (d) => { this.selectedExam = d; this.successMessage = 'Question detached.'; },
      error: (e) => this.errorMessage = e?.error?.message ?? 'Detach failed.'
    });
  }

  handlePublishClick(id: number): void {
    if (!confirm('Publish this exam? It will become visible to candidates.')) return;
    this.examService.publishExam(id).subscribe({
      next: () => { this.successMessage = 'Exam published!'; this.loadMyExams(); },
      error: (e) => this.errorMessage = e?.error?.message ?? 'Publish failed.'
    });
  }

  handlePublishResultsClick(id: number): void {
    if (!confirm('Publish results? Candidates will see their scores.')) return;
    this.examService.publishResults(id).subscribe({
      next: () => { this.successMessage = 'Results published!'; this.loadMyExams(); },
      error: (e) => this.errorMessage = e?.error?.message ?? 'Publish results failed.'
    });
  }
}
