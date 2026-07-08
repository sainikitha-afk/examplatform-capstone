// examiner-request.ts — Public form for prospective examiners to apply.
// No auth required. Calls POST /api/examiner-requests (anonymous endpoint).
// On success, shows a confirmation banner — no redirect (user is not logged in).

import { Component } from '@angular/core';
import { FormsModule, NgForm } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { ExaminerRequestApiService } from '../services/examinerrequestapi.service';
import { SubmitExaminerRequestModel } from '../models/examiner-request.model';

@Component({
  selector: 'app-examiner-request',
  imports: [FormsModule, CommonModule, RouterModule],
  templateUrl: './examiner-request.html',
  styleUrl:    './examiner-request.css',
})
export class ExaminerRequest {
  formData: SubmitExaminerRequestModel = new SubmitExaminerRequestModel();

  isLoading:      boolean = false;
  errorMessage:   string  = '';
  successMessage: string  = '';
  submitted:      boolean = false;  // hides form, shows success card

  constructor(private api: ExaminerRequestApiService) {}

  handleSubmitClick(form: NgForm): void {
    if (form.invalid) {
      Object.values(form.controls).forEach(c => c.markAsTouched());
      return;
    }

    this.isLoading    = true;
    this.errorMessage = '';

    this.api.submit(this.formData).subscribe({
      next: () => {
        this.isLoading = false;
        this.submitted = true;
      },
      error: (err: any) => {
        this.isLoading = false;
        if (err.status === 409) {
          this.errorMessage =
            'An application with this email is already pending review. ' +
            'Please wait for an admin to process it, or contact support if it has been more than 7 days.';
        } else if (err.status === 400) {
          this.errorMessage = err.error?.error ?? 'Please check your input and try again.';
        } else if (err.status === 0) {
          this.errorMessage = 'Cannot reach the server. Make sure the backend is running.';
        } else {
          this.errorMessage = err.error?.error ?? 'Something went wrong. Please try again later.';
        }
      }
    });
  }
}
