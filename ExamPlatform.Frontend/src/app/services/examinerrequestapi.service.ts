// ExaminerRequestApiService — HTTP calls for /api/examiner-requests.
// POST submit is anonymous (no auth token). Admin read/approve/reject require Admin JWT.

import { Injectable } from '@angular/core';
import { environment } from '../../environments/environment';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { ExaminerRequestModel, SubmitExaminerRequestModel } from '../models/examiner-request.model';

@Injectable({ providedIn: 'root' })
export class ExaminerRequestApiService {
  private baseUrl = environment.apiBaseUrl + '/api/examiner-requests';

  constructor(private http: HttpClient) {}

  // POST /api/examiner-requests — anonymous, no auth header
  submit(data: SubmitExaminerRequestModel): Observable<ExaminerRequestModel> {
    return this.http.post<ExaminerRequestModel>(this.baseUrl, data);
  }

  // GET /api/examiner-requests — Admin: all requests (any status)
  getAll(): Observable<ExaminerRequestModel[]> {
    return this.http.get<ExaminerRequestModel[]>(this.baseUrl);
  }

  // GET /api/examiner-requests/pending — Admin: only Pending requests
  getPending(): Observable<ExaminerRequestModel[]> {
    return this.http.get<ExaminerRequestModel[]>(`${this.baseUrl}/pending`);
  }

  // GET /api/examiner-requests/{id} — Admin: single request detail
  getById(id: number): Observable<ExaminerRequestModel> {
    return this.http.get<ExaminerRequestModel>(`${this.baseUrl}/${id}`);
  }

  // POST /api/examiner-requests/{id}/approve — Admin: approve → creates Examiner account
  approve(id: number): Observable<any> {
    return this.http.post<any>(`${this.baseUrl}/${id}/approve`, {});
  }

  // POST /api/examiner-requests/{id}/reject — Admin: reject with optional note
  reject(id: number, adminNote: string): Observable<ExaminerRequestModel> {
    return this.http.post<ExaminerRequestModel>(`${this.baseUrl}/${id}/reject`, { adminNote });
  }
}
