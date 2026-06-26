// certificates.ts — Lists the candidate's earned certificates.
// CONCEPTS DEMO: date pipe on issuedAt, *ngFor, *ngIf for revoked status.

import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { CertificateApiService } from '../services/certificateapi.service';
import { CandidateCertificateModel } from '../models/certificate.model';

@Component({
  selector: 'app-certificates',
  imports: [CommonModule, RouterModule],
  templateUrl: './certificates.html',
  styleUrl: './certificates.css',
})
export class Certificates implements OnInit {
  certs: CandidateCertificateModel[] = [];
  isLoading: boolean = true;
  errorMessage: string = '';

  constructor(private certService: CertificateApiService) {}

  ngOnInit(): void {
    this.certService.loadMyCertificates().subscribe({
      next: (data) => { this.certs = data; this.isLoading = false; },
      error: () => { this.errorMessage = 'Could not load certificates.'; this.isLoading = false; }
    });
  }

  handleDownloadClick(certId: number): void {
    this.certService.downloadCertificate(certId).subscribe({
      next: (data) => {
        // Open certificate text in a new tab as a simple text file.
        const blob = new Blob([data.certificateText], { type: 'text/plain' });
        const url = URL.createObjectURL(blob);
        window.open(url);
      },
      error: () => alert('Download failed.')
    });
  }
}
