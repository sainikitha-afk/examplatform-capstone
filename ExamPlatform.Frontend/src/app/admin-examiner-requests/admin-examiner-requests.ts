// admin-examiner-requests.ts — Admin screen for reviewing pending examiner applications.
// Calls GET /api/examiner-requests/pending, POST …/{id}/approve, POST …/{id}/reject.
// Shows a reject modal with an optional adminNote text field.

import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { ExaminerRequestApiService } from '../services/examinerrequestapi.service';
import { ExaminerRequestModel } from '../models/examiner-request.model';

@Component({
  selector: 'app-admin-examiner-requests',
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './admin-examiner-requests.html',
  styleUrl:    './admin-examiner-requests.css',
})
export class AdminExaminerRequests implements OnInit {
  requests:        ExaminerRequestModel[] = [];
  isLoading:       boolean = false;
  errorMessage:    string  = '';
  successMessage:  string  = '';

  // Reject modal state
  showRejectModal: boolean = false;
  rejectingId:     number  = 0;
  rejectingName:   string  = '';
  adminNote:       string  = '';
  isRejecting:     boolean = false;

  // Approve loading state (track by id so only one spinner shows at a time)
  approvingId:     number | null = null;

  constructor(private api: ExaminerRequestApiService) {}

  ngOnInit(): void {
    this.loadRequests();
  }

  loadRequests(): void {
    this.isLoading    = true;
    this.errorMessage = '';
    this.api.getPending().subscribe({
      next: (data) => {
        this.isLoading = false;
        this.requests  = data;
      },
      error: (err: any) => {
        this.isLoading    = false;
        this.errorMessage =
          err.status === 0
            ? 'Cannot reach the server. Make sure the backend is running.'
            : err.error?.error ?? 'Failed to load requests.';
      }
    });
  }

  handleApproveClick(req: ExaminerRequestModel): void {
    if (!confirm(`Approve ${req.fullName} as an Examiner? An account will be created and credentials emailed.`)) return;

    this.approvingId      = req.examinerRequestId;
    this.successMessage   = '';
    this.errorMessage     = '';

    this.api.approve(req.examinerRequestId).subscribe({
      next: () => {
        this.approvingId    = null;
        this.successMessage = `${req.fullName} approved — examiner account created.`;
        this.requests       = this.requests.filter(r => r.examinerRequestId !== req.examinerRequestId);
      },
      error: (err: any) => {
        this.approvingId  = null;
        if (err.status === 409) {
          this.errorMessage =
            `Cannot approve: ${err.error?.error ?? 'this email already has an account or the request was already processed.'}`;
        } else if (err.status === 0) {
          this.errorMessage = 'Cannot reach the server. Make sure the backend is running.';
        } else {
          this.errorMessage = err.error?.error ?? `Approval failed (${err.status}). Please try again.`;
        }
      }
    });
  }

  openRejectModal(req: ExaminerRequestModel): void {
    this.rejectingId   = req.examinerRequestId;
    this.rejectingName = req.fullName;
    this.adminNote     = '';
    this.showRejectModal = true;
  }

  closeRejectModal(): void {
    this.showRejectModal = false;
  }

  handleConfirmReject(): void {
    this.isRejecting    = true;
    this.successMessage = '';
    this.errorMessage   = '';

    this.api.reject(this.rejectingId, this.adminNote).subscribe({
      next: () => {
        this.isRejecting     = false;
        this.showRejectModal = false;
        this.successMessage  = `${this.rejectingName}'s application rejected.`;
        this.requests        = this.requests.filter(r => r.examinerRequestId !== this.rejectingId);
      },
      error: (err: any) => {
        this.isRejecting     = false;
        this.showRejectModal = false;
        this.errorMessage    =
          err.status === 0
            ? 'Cannot reach the server. Make sure the backend is running.'
            : err.error?.error ?? `Rejection failed (${err.status}). Please try again.`;
      }
    });
  }
}
