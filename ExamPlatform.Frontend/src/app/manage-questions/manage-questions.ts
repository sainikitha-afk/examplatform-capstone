// manage-questions.ts — Examiner's question bank: list, create, edit, delete, Excel import.
// CONCEPTS DEMO:
//   - Template reference variable #fileInput (Excel upload)
//   - *ngFor table, *ngIf view switching
//   - Two-way binding on create/edit form
//   - Event binding on all action buttons

import { Component, OnInit, ViewChild, ElementRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { QuestionApiService } from '../services/questionapi.service';
import { CategoryApiService } from '../services/categoryapi.service';
import { QuestionModel, CreateQuestionModel } from '../models/question.model';
import { CreateOptionModel } from '../models/option.model';
import { CategoryModel } from '../models/category.model';

@Component({
  selector: 'app-manage-questions',
  imports: [CommonModule, FormsModule],
  templateUrl: './manage-questions.html',
  styleUrl: './manage-questions.css',
})
export class ManageQuestions implements OnInit {
  questions: QuestionModel[] = [];
  categories: CategoryModel[] = [];
  isLoading: boolean = false;
  isSaving: boolean = false;
  isImporting: boolean = false;
  isDownloadingTemplate: boolean = false;
  errorMessage: string = '';
  successMessage: string = '';

  // View mode toggle: 'list' | 'create' | 'edit'
  view: string = 'list';

  // Form model for create/edit.
  formData: CreateQuestionModel = new CreateQuestionModel();
  editingId: number = 0;

  // Filter state
  filterCatId: number | null = null;
  filterDifficulty: string = '';

  // CONCEPT DEMO: @ViewChild + ElementRef — template reference variable #fileInput
  // accessed programmatically so we can read the selected file.
  @ViewChild('fileInput') fileInputRef!: ElementRef<HTMLInputElement>;

  importResult: { successCount: number; failureCount: number; errors: string[] } | null = null;

  constructor(
    private questionService: QuestionApiService,
    private categoryService: CategoryApiService
  ) {}

  ngOnInit(): void {
    this.categoryService.loadCategories().subscribe({ next: (c) => this.categories = c });
    this.loadQuestions();
  }

  loadQuestions(): void {
    this.isLoading = true;
    this.questionService.loadQuestions(this.filterCatId, this.filterDifficulty).subscribe({
      next: (result) => { this.questions = result.items; this.isLoading = false; },
      error: () => { this.errorMessage = 'Failed to load questions.'; this.isLoading = false; }
    });
  }

  showCreateForm(): void {
    this.formData = new CreateQuestionModel();
    this.editingId = 0;
    this.view = 'create';
  }

  showEditForm(q: QuestionModel): void {
    this.formData = new CreateQuestionModel(
      q.catId, q.questionText, q.marks, q.difficulty,
      q.options.map(o => new CreateOptionModel(o.optionText, o.isCorrect))
    );
    this.editingId = q.questionId;
    this.view = 'edit';
  }

  handleSaveClick(): void {
    if (this.isSaving) return;
    this.isSaving = true;
    this.errorMessage = '';

    const obs = this.editingId
      ? this.questionService.updateQuestion(this.editingId, this.formData)
      : this.questionService.createQuestion(this.formData);

    obs.subscribe({
      next: () => {
        this.isSaving = false;
        this.successMessage = 'Question saved!';
        this.view = 'list';
        this.loadQuestions();
      },
      error: (err) => {
        this.isSaving = false;
        this.errorMessage = err?.error?.error ?? 'Save failed.';
      }
    });
  }

  handleDeleteClick(id: number): void {
    if (!confirm('Delete this question?')) return;
    this.questionService.deleteQuestion(id).subscribe({
      next: () => this.loadQuestions(),
      error: () => this.errorMessage = 'Delete failed.'
    });
  }

  // CONCEPT DEMO: template reference variable #fileInput — trigger click programmatically.
  handleUploadClick(): void {
    this.fileInputRef.nativeElement.click();
  }

  handleFileSelected(): void {
    const file = this.fileInputRef.nativeElement.files?.[0];
    if (!file) return;
    this.importResult = null;
    this.isImporting = true;
    this.errorMessage = '';
    this.questionService.importExcel(file).subscribe({
      next: (result) => {
        this.isImporting = false;
        this.importResult = result;
        this.loadQuestions();
        // Reset the file input so the same file can be re-selected.
        this.fileInputRef.nativeElement.value = '';
      },
      error: () => {
        this.isImporting = false;
        this.errorMessage = 'Import failed. Make sure the file follows the template format.';
        this.fileInputRef.nativeElement.value = '';
      }
    });
  }

  // Downloads the pre-formatted Excel template so examiners know the exact
  // column layout (CategoryId | QuestionText | Marks | Difficulty | OptionA-D | CorrectOption).
  handleDownloadTemplateClick(): void {
    if (this.isDownloadingTemplate) return;
    this.isDownloadingTemplate = true;
    this.errorMessage = '';
    this.questionService.downloadImportTemplate().subscribe({
      next: (blob) => {
        this.isDownloadingTemplate = false;
        // Trigger a browser download from the received blob.
        const url = URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.href = url;
        link.download = 'questions_template.xlsx';
        link.click();
        URL.revokeObjectURL(url);
      },
      error: () => {
        this.isDownloadingTemplate = false;
        this.errorMessage = 'Could not download the template. Please try again.';
      }
    });
  }

  setCorrectOption(index: number): void {
    this.formData.options.forEach((o, i) => o.isCorrect = (i === index));
  }
}
