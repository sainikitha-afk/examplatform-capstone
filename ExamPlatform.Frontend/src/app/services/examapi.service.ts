// ExamApiService — HTTP calls for /api/exams endpoints.
// Why it exists: components never talk to HttpClient directly; this service encapsulates all
//   exam-related HTTP logic and is shared across browse-exams, exam-details, and manage-exams.
// How Angular uses it: @Injectable root singleton, constructor-injected into components.

import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { ExamSummaryModel, ExamDetailModel, CreateExamModel, UpdateExamModel } from '../models/exam.model';
import { ExamAttemptMonitorModel } from '../models/attempt.model';
import { PagedResultModel } from '../models/paged-result.model';

@Injectable({ providedIn: 'root' })
export class ExamApiService {
  private baseUrl = 'http://localhost:5014/api/exams';

  constructor(private http: HttpClient) {}

  // GET /api/exams?categoryId=&page= — paginated published exam list (Candidates browse here)
  loadPublishedExams(categoryId: number | null = null, page: number = 1): Observable<PagedResultModel<ExamSummaryModel>> {
    let params = new HttpParams().set('page', page).set('pageSize', 10);
    if (categoryId) params = params.set('categoryId', categoryId);
    return this.http.get<PagedResultModel<ExamSummaryModel>>(this.baseUrl, { params });
  }

  // GET /api/exams/mine — examiner's own exams (paginated)
  loadMyExams(page: number = 1): Observable<PagedResultModel<ExamSummaryModel>> {
    const params = new HttpParams().set('page', page).set('pageSize', 10);
    return this.http.get<PagedResultModel<ExamSummaryModel>>(`${this.baseUrl}/mine`, { params });
  }

  // GET /api/exams/{id} — full exam detail (content varies by role)
  loadExamById(id: number): Observable<ExamDetailModel> {
    return this.http.get<ExamDetailModel>(`${this.baseUrl}/${id}`);
  }

  // Converts a datetime-local input value (e.g. "2026-07-08T17:00", no timezone)
  // to a UTC ISO string. The backend stores timestamps as UTC in PostgreSQL, and
  // Npgsql rejects timezone-less values — so we convert BEFORE sending.
  // Empty values become null (end time is optional; the backend defaults it).
  private toUtcIso(localValue: string): string | null {
    if (!localValue) return null;
    return new Date(localValue).toISOString();
  }

  // POST /api/exams (Examiner) — create a draft exam
  createExam(data: CreateExamModel): Observable<ExamSummaryModel> {
    const payload = {
      ...data,
      startTime: this.toUtcIso(data.startTime),
      endTime: this.toUtcIso(data.endTime)   // null → backend defaults to +30 days
    };
    return this.http.post<ExamSummaryModel>(this.baseUrl, payload);
  }

  // PUT /api/exams/{id} (Examiner) — update a draft exam
  updateExam(id: number, data: UpdateExamModel): Observable<ExamSummaryModel> {
    const payload = {
      ...data,
      startTime: this.toUtcIso(data.startTime),
      endTime: this.toUtcIso(data.endTime)
    };
    return this.http.put<ExamSummaryModel>(`${this.baseUrl}/${id}`, payload);
  }

  // DELETE /api/exams/{id} (Examiner) → 204
  deleteExam(id: number): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }

  // POST /api/exams/{id}/questions — attach question IDs to an exam (Examiner)
  attachQuestions(examId: number, questionIds: number[]): Observable<ExamDetailModel> {
    return this.http.post<ExamDetailModel>(`${this.baseUrl}/${examId}/questions`, { questionIds });
  }

  // DELETE /api/exams/{examId}/questions/{questionId} — detach a single question
  detachQuestion(examId: number, questionId: number): Observable<ExamDetailModel> {
    return this.http.delete<ExamDetailModel>(`${this.baseUrl}/${examId}/questions/${questionId}`);
  }

  // GET /api/exams/{id}/attempts — every attempt on this exam (Examiner monitoring view)
  loadExamAttempts(examId: number): Observable<ExamAttemptMonitorModel[]> {
    return this.http.get<ExamAttemptMonitorModel[]>(`${this.baseUrl}/${examId}/attempts`);
  }

  // POST /api/exams/{id}/publish — moves exam from Draft → Published
  publishExam(id: number): Observable<ExamSummaryModel> {
    return this.http.post<ExamSummaryModel>(`${this.baseUrl}/${id}/publish`, {});
  }

  // POST /api/exams/{id}/publish-results — moves exam from Published → ResultsPublished
  publishResults(id: number): Observable<ExamSummaryModel> {
    return this.http.post<ExamSummaryModel>(`${this.baseUrl}/${id}/publish-results`, {});
  }

  // POST /api/exams/{id}/unpublish — reverts Published → Draft (only when no attempts exist)
  unpublishExam(id: number): Observable<ExamSummaryModel> {
    return this.http.post<ExamSummaryModel>(`${this.baseUrl}/${id}/unpublish`, {});
  }
}
