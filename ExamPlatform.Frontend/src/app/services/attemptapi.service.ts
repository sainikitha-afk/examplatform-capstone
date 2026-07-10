// AttemptApiService — HTTP calls for /api/attempts endpoints (Candidate only).
// Why it exists: the attempt-exam screen (and NgRx effects) call these methods.
// How Angular uses it: NgRx effects inject this service via constructor to make HTTP calls
//   in response to dispatched actions.

import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { AttemptResumeModel, AttemptSummaryModel, SaveAnswerModel } from '../models/attempt.model';
import { AttemptResultModel } from '../models/attempt-result.model';
import { PagedResultModel } from '../models/paged-result.model';

@Injectable({ providedIn: 'root' })
export class AttemptApiService {
  private baseUrl = 'http://localhost:5014/api/attempts';

  constructor(private http: HttpClient) {}

  // POST /api/attempts/start — starts a new attempt OR resumes an existing one.
  // Request body: { examId }. Backend decides start vs resume.
  startAttempt(examId: number): Observable<AttemptResumeModel> {
    return this.http.post<AttemptResumeModel>(`${this.baseUrl}/start`, { examId });
  }

  // GET /api/attempts/{id} — fetch an existing attempt for resume display
  loadAttemptById(id: number): Observable<AttemptResumeModel> {
    return this.http.get<AttemptResumeModel>(`${this.baseUrl}/${id}`);
  }

  // PUT /api/attempts/{id}/answers — auto-save a single answer. Returns 204.
  // SelectedOptionId may be null (candidate cleared/skipped the answer).
  saveAnswer(attemptId: number, answer: SaveAnswerModel): Observable<void> {
    return this.http.put<void>(`${this.baseUrl}/${attemptId}/answers`, answer);
  }

  // POST /api/attempts/{id}/submit — finalize the attempt.
  submitAttempt(id: number): Observable<AttemptSummaryModel> {
    return this.http.post<AttemptSummaryModel>(`${this.baseUrl}/${id}/submit`, {});
  }

  // POST /api/attempts/{id}/tab-switch — proctoring signal.
  // Called when the candidate leaves the exam tab/window mid-attempt.
  // Returns the running count so the UI can show "Warning N of 3".
  recordTabSwitch(id: number): Observable<{ tabSwitchCount: number }> {
    return this.http.post<{ tabSwitchCount: number }>(`${this.baseUrl}/${id}/tab-switch`, {});
  }

  // GET /api/attempts/mine — candidate's attempt history (paginated)
  loadMyAttempts(page: number = 1): Observable<PagedResultModel<AttemptSummaryModel>> {
    return this.http.get<PagedResultModel<AttemptSummaryModel>>(`${this.baseUrl}/mine`);
  }

  // GET /api/attempts/{id}/result — full result with per-question breakdown
  loadAttemptResult(id: number): Observable<AttemptResultModel> {
    return this.http.get<AttemptResultModel>(`${this.baseUrl}/${id}/result`);
  }
}
