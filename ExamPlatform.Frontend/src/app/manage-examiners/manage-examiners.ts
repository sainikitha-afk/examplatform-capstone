// manage-examiners.ts — Admin: create examiner accounts.
// CONCEPTS DEMO: template-driven validation, event binding, two-way binding.

import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, NgForm } from '@angular/forms';
import { UserApiService } from '../services/userapi.service';
import { UserModel, CreateExaminerModel } from '../models/user.model';

@Component({
  selector: 'app-manage-examiners',
  imports: [CommonModule, FormsModule],
  templateUrl: './manage-examiners.html',
  styleUrl: './manage-examiners.css',
})
export class ManageExaminers {
  formData: CreateExaminerModel = new CreateExaminerModel();
  createdExaminers: UserModel[] = [];
  isLoading: boolean = false;
  successMessage: string = '';
  errorMessage: string = '';

  constructor(private userService: UserApiService) {}

  handleCreateClick(form: NgForm): void {
    if (form.invalid) {
      Object.values(form.controls).forEach(c => c.markAsTouched());
      return;
    }
    this.isLoading = true;
    this.userService.createExaminer(this.formData).subscribe({
      next: (user) => {
        this.isLoading = false;
        this.successMessage = `Examiner "${user.fullName}" created successfully!`;
        this.createdExaminers.unshift(user);
        this.formData = new CreateExaminerModel();
        form.resetForm();
      },
      error: (err) => {
        this.isLoading = false;
        this.errorMessage = err?.error?.message ?? 'Failed to create examiner.';
      }
    });
  }
}
