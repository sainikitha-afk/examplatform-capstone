// CertificateApiService — HTTP calls for /api/certificates endpoints.
// Why it exists: certificates screen and admin management need typed certificate data.
// How Angular uses it: constructor-injected; returns Observables.

import { Injectable } from '@angular/core';
import { environment } from '../../environments/environment';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { CandidateCertificateModel, CertificateDownloadModel } from '../models/certificate.model';

@Injectable({ providedIn: 'root' })
export class CertificateApiService {
  private baseUrl = environment.apiBaseUrl + '/api/certificates';

  constructor(private http: HttpClient) {}

  // GET /api/certificates/mine (Candidate) — own certificates
  loadMyCertificates(): Observable<CandidateCertificateModel[]> {
    return this.http.get<CandidateCertificateModel[]>(`${this.baseUrl}/mine`);
  }

  // GET /api/certificates/{id}/download — certificate text for display/download
  downloadCertificate(id: number): Observable<CertificateDownloadModel> {
    return this.http.get<CertificateDownloadModel>(`${this.baseUrl}/${id}/download`);
  }

  // POST /api/certificates/{id}/revoke (Admin)
  revokeCertificate(id: number): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/${id}/revoke`, {});
  }
}
