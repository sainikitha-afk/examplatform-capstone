// categories.ts — Admin: manage exam categories (create, edit, delete).
// Extended from the scaffolded placeholder to a full CRUD screen.
// CONCEPTS DEMO:
//   - *ngFor table, *ngIf for form toggle
//   - Two-way binding [(ngModel)] on the create/edit form
//   - Template-driven validation (required, minlength)
//   - Event binding (click) / (ngSubmit)

import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, NgForm } from '@angular/forms';
import { CategoryApiService } from '../services/categoryapi.service';
import { CategoryModel } from '../models/category.model';

@Component({
  selector: 'app-categories',
  imports: [CommonModule, FormsModule],
  templateUrl: './categories.html',
  styleUrl: './categories.css',
})
export class Categories implements OnInit {
  categories: CategoryModel[] = [];
  isLoading: boolean = false;
  errorMessage: string = '';
  successMessage: string = '';

  // Form fields — two-way bound in the template.
  formName: string = '';
  formDescription: string = '';

  // View toggle: 'list' | 'create' | 'edit'
  view: string = 'list';
  editingId: number = 0;

  constructor(private categoryService: CategoryApiService) {}

  ngOnInit(): void { this.loadCategories(); }

  loadCategories(): void {
    this.isLoading = true;
    this.categoryService.loadCategories().subscribe({
      next: (data) => { this.categories = data; this.isLoading = false; },
      error: () => { this.errorMessage = 'Failed to load categories.'; this.isLoading = false; }
    });
  }

  showCreateForm(): void {
    this.formName = '';
    this.formDescription = '';
    this.editingId = 0;
    this.view = 'create';
    this.errorMessage = '';
    this.successMessage = '';
  }

  showEditForm(cat: CategoryModel): void {
    this.formName = cat.catName;
    this.formDescription = cat.description ?? '';
    this.editingId = cat.catId;
    this.view = 'edit';
    this.errorMessage = '';
    this.successMessage = '';
  }

  handleSaveClick(form: NgForm): void {
    if (form.invalid) {
      Object.values(form.controls).forEach(c => c.markAsTouched());
      return;
    }
    const obs = this.editingId
      ? this.categoryService.updateCategory(this.editingId, this.formName, this.formDescription)
      : this.categoryService.createCategory(this.formName, this.formDescription);

    obs.subscribe({
      next: () => {
        this.successMessage = this.editingId ? 'Category updated!' : 'Category created!';
        this.view = 'list';
        this.loadCategories();
      },
      error: (err) => {
        this.errorMessage = err?.error?.message ?? 'Save failed.';
      }
    });
  }

  handleDeleteClick(id: number, name: string): void {
    if (!confirm(`Delete category "${name}"? This cannot be undone.`)) return;
    this.categoryService.deleteCategory(id).subscribe({
      next: () => { this.successMessage = 'Category deleted.'; this.loadCategories(); },
      error: (err) => this.errorMessage = err?.error?.message ?? 'Delete failed.'
    });
  }
}
