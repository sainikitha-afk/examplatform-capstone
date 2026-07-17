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
import { CategoryBoard } from '../category-board/category-board';

@Component({
  selector: 'app-manage-exams',
  imports: [CommonModule, FormsModule, CategoryBoard],
  templateUrl: './manage-exams.html',
  styleUrl: './manage-exams.css',
})
export class ManageExams implements OnInit {
  exams: ExamSummaryModel[] = [];
  selectedExam: ExamDetailModel = new ExamDetailModel();
  questionBank: QuestionModel[] = [];
  categories: CategoryModel[] = [];

  // Client-side category filter for "My Exams" — the exam list is small
  // enough per examiner that a server round-trip per filter isn't needed.
  selectedListCategoryId: number | null = null;

  get filteredExams(): ExamSummaryModel[] {
    return this.selectedListCategoryId === null
      ? this.exams
      : this.exams.filter(e => e.categoryId === this.selectedListCategoryId);
  }

  handleListBoardSelect(catId: number | null): void {
    this.selectedListCategoryId = catId;
  }

  // Selected-but-not-yet-attached questions. A Map (id → question) instead of
  // a Set so selections survive filter changes AND carry their marks for the
  // running totals.
  selectedQuestions: Map<number, QuestionModel> = new Map();

  // Question-bank filters (attach view).
  bankCatId: number | null = null;
  bankSearch: string = '';
  isBankLoading: boolean = false;
  private searchDebounce: ReturnType<typeof setTimeout> | null = null;

  isLoading: boolean = false;
  isSaving: boolean = false;
  isPublishing: boolean = false;
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

  // ══════════════════════════════════════════════════════════════════
  //   ATTACH QUESTIONS — draft workspace
  //   Selections live in a Map (id → question) so they survive category /
  //   search filter changes AND we always know each selection's marks.
  // ══════════════════════════════════════════════════════════════════

  showAttachQuestions(exam: ExamSummaryModel): void {
    this.editingExamId = exam.examId;
    this.selectedQuestions = new Map();
    this.bankCatId = null;
    this.bankSearch = '';
    this.view = 'questions';
    // Load exam detail to see already-attached questions + marks.
    this.examService.loadExamById(exam.examId).subscribe({
      next: (d) => this.selectedExam = d,
      error: (e) => this.errorMessage = e?.error?.error ?? 'Failed to load exam.'
    });
    this.loadQuestionBank();
  }

  // Loads the bank with the current filters. pageSize 100 = full bank
  // (the backend clamps to 100). Selections are NOT touched here — changing
  // a filter must never clear what the examiner already picked.
  loadQuestionBank(): void {
    this.isBankLoading = true;
    this.questionService.loadQuestions(this.bankCatId, '', this.bankSearch, 1, 100).subscribe({
      next: (r) => { this.questionBank = r.items; this.isBankLoading = false; },
      error: (e) => {
        this.isBankLoading = false;
        this.errorMessage = e?.error?.error ?? 'Failed to load the question bank.';
      }
    });
  }

  handleBankFilterChange(): void { this.loadQuestionBank(); }

  // Debounced search: plain setTimeout so typing doesn't fire one API call
  // per keystroke.
  handleBankSearchInput(): void {
    if (this.searchDebounce) clearTimeout(this.searchDebounce);
    this.searchDebounce = setTimeout(() => this.loadQuestionBank(), 300);
  }

  // ─── Marks arithmetic (drives the summary panel + validation) ────────
  getAttachedMarks(): number {
    return this.selectedExam.questions.reduce((sum, q) => sum + q.marks, 0);
  }
  getSelectedMarks(): number {
    let sum = 0;
    this.selectedQuestions.forEach(q => sum += q.marks);
    return sum;
  }
  getProjectedTotal(): number { return this.getAttachedMarks() + this.getSelectedMarks(); }
  getRemainingMarks(): number { return this.selectedExam.totalMarks - this.getProjectedTotal(); }

  isSelected(qId: number): boolean { return this.selectedQuestions.has(qId); }
  isAttached(qId: number): boolean {
    return this.selectedExam.questions.some(q => q.questionId === qId);
  }

  // Category name lookup for per-question labels + the breakdown line.
  getCategoryName(catId: number): string {
    return this.categories.find(c => c.catId === catId)?.catName ?? `Category ${catId}`;
  }

  // "2 × C# & .NET, 1 × DBMS" — built from the attached questions.
  getCategoryBreakdown(): string {
    const counts = new Map<number, number>();
    for (const q of this.selectedExam.questions) {
      counts.set(q.catId, (counts.get(q.catId) ?? 0) + 1);
    }
    const parts: string[] = [];
    counts.forEach((count, catId) => parts.push(`${count} × ${this.getCategoryName(catId)}`));
    return parts.join(', ');
  }

  // Selecting a question is blocked when it would push the projected total
  // past the exam's configured marks — with a message saying what remains.
  toggleQuestionSelect(q: QuestionModel): void {
    if (this.isAttached(q.questionId)) return; // already on the exam — not selectable

    if (this.selectedQuestions.has(q.questionId)) {
      this.selectedQuestions.delete(q.questionId);
      this.errorMessage = '';
      return;
    }

    const remaining = this.selectedExam.totalMarks - this.getProjectedTotal();
    if (q.marks > remaining) {
      this.errorMessage =
        `This question (${q.marks} marks) cannot be added because the exam total would exceed ` +
        `${this.selectedExam.totalMarks} marks. Only ${remaining} marks remain.`;
      return;
    }

    this.selectedQuestions.set(q.questionId, q);
    this.errorMessage = '';
  }

  handleAttachClick(): void {
    if (this.isSaving) return;
    const ids = Array.from(this.selectedQuestions.keys());
    if (ids.length === 0) {
      this.errorMessage = 'Select at least one question to attach.';
      return;
    }
    // Belt-and-braces cap check (the backend enforces the same rule).
    if (this.getProjectedTotal() > this.selectedExam.totalMarks) {
      this.errorMessage =
        `Cannot attach: the total would exceed ${this.selectedExam.totalMarks} marks.`;
      return;
    }
    this.isSaving = true;
    this.errorMessage = '';
    this.examService.attachQuestions(this.editingExamId, ids).subscribe({
      next: (d) => {
        // Stay in the draft workspace so the examiner can keep building.
        this.isSaving = false;
        this.selectedExam = d;
        this.selectedQuestions = new Map();
        this.successMessage = 'Questions attached!';
      },
      error: (e) => { this.isSaving = false; this.errorMessage = e?.error?.error ?? 'Attach failed.'; }
    });
  }

  handleDetachClick(examId: number, questionId: number): void {
    this.examService.detachQuestion(examId, questionId).subscribe({
      next: (d) => { this.selectedExam = d; this.successMessage = 'Question detached.'; },
      error: (e) => this.errorMessage = e?.error?.error ?? 'Detach failed.'
    });
  }

  // ─── Publish readiness ───────────────────────────────────────────────
  // 'Published' | 'Ready' | 'Incomplete' — drives the badge and the button.
  getReadiness(): string {
    if (this.selectedExam.examStatus !== 'Draft') return 'Published';
    return this.getAttachedMarks() === this.selectedExam.totalMarks ? 'Ready' : 'Incomplete';
  }

  // ─── Publish / unpublish / results ──────────────────────────────────
  handlePublishClick(id: number): void {
    if (this.isPublishing) return;
    if (!confirm('Publish this exam?\n\nOnce published, it becomes visible to candidates and ALL active candidates are notified in-app and by email.')) return;
    this.isPublishing = true;
    this.examService.publishExam(id).subscribe({
      next: () => {
        this.isPublishing = false;
        this.successMessage = 'Exam published! All active candidates have been notified.';
        if (this.view === 'questions') this.selectedExam.examStatus = 'Published';
        this.loadMyExams();
      },
      error: (e) => {
        this.isPublishing = false;
        this.errorMessage = e?.error?.error ?? 'Publish failed.';
      }
    });
  }

  handleUnpublishClick(id: number): void {
    if (this.isPublishing) return;
    if (!confirm('Revert this exam to Draft?\n\nOnly possible while no candidate has attempted it.')) return;
    this.isPublishing = true;
    this.examService.unpublishExam(id).subscribe({
      next: () => { this.isPublishing = false; this.successMessage = 'Exam reverted to Draft.'; this.loadMyExams(); },
      error: (e) => { this.isPublishing = false; this.errorMessage = e?.error?.error ?? 'Unpublish failed.'; }
    });
  }

  handlePublishResultsClick(id: number): void {
    if (this.isPublishing) return;
    if (!confirm('Publish results? Candidates will see their scores, and passers receive certificates.')) return;
    this.isPublishing = true;
    this.examService.publishResults(id).subscribe({
      next: () => { this.isPublishing = false; this.successMessage = 'Results published!'; this.loadMyExams(); },
      error: (e) => { this.isPublishing = false; this.errorMessage = e?.error?.error ?? 'Publish results failed.'; }
    });
  }
}
