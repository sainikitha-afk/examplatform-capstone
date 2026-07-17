// admin-institution-verifications.ts — Admin action queue for Institute
// Verification requests (Part 3/7).

import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { InstitutionApiService } from '../services/institutionapi.service';
import { InstitutionModel } from '../models/subscription.model';

@Component({
  selector: 'app-admin-institution-verifications',
  imports: [CommonModule],
  templateUrl: './admin-institution-verifications.html',
  styleUrl: './admin-institution-verifications.css',
})
export class AdminInstitutionVerifications implements OnInit {
  requests: InstitutionModel[] = [];
  isLoading = true;
  errorMessage = '';
  processingId: number | null = null;

  constructor(private institutionApi: InstitutionApiService) {}

  ngOnInit(): void {
    this.loadPending();
  }

  loadPending(): void {
    this.isLoading = true;
    this.institutionApi.loadPending().subscribe({
      next: (requests) => { this.requests = requests; this.isLoading = false; },
      error: () => { this.errorMessage = 'Could not load pending requests.'; this.isLoading = false; }
    });
  }

  handleApproveClick(id: number): void {
    this.processingId = id;
    this.institutionApi.approve(id).subscribe({
      next: () => { this.processingId = null; this.loadPending(); },
      error: () => { this.processingId = null; }
    });
  }

  handleRejectClick(id: number): void {
    this.processingId = id;
    this.institutionApi.reject(id).subscribe({
      next: () => { this.processingId = null; this.loadPending(); },
      error: () => { this.processingId = null; }
    });
  }
}
