// CoachApiService — HTTP calls for /api/coach (the AI Learning Coach).
// Why it exists: the /ai-coach screen must never talk to Gemini directly. Every AI request
//   goes through our backend, which holds the API key, enforces authorization, controls the
//   prompt, and rate-limits the caller. This service is the browser's only door to it.
// How Angular uses it: constructor-injected; returns Observables. The JWT is attached
//   automatically by authInterceptor.
// CONCEPT DEMO: catchError — turns HTTP failures into messages a candidate can act on,
//   so no raw backend error or provider payload is ever rendered.

import { Injectable } from '@angular/core';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { CoachAnswerModel, CoachSnapshotModel, CoachTurn } from '../models/coach.model';

@Injectable({ providedIn: 'root' })
export class CoachApiService {
  // Same convention as every other service in this app (no environments/ folder exists).
  private baseUrl = 'http://localhost:5014/api/coach';

  constructor(private http: HttpClient) {}

  // GET /api/coach/snapshot (Candidate) — deterministic performance summary.
  // No AI call happens server-side, so this is cheap and always safe to load on entry.
  loadSnapshot(): Observable<CoachSnapshotModel> {
    return this.http.get<CoachSnapshotModel>(`${this.baseUrl}/snapshot`).pipe(
      catchError((err: HttpErrorResponse) => this.toFriendlyError(err))
    );
  }

  // POST /api/coach/ask (Candidate) — personalized coaching from the candidate's own results.
  ask(message: string, history: CoachTurn[] = []): Observable<CoachAnswerModel> {
    return this.http.post<CoachAnswerModel>(`${this.baseUrl}/ask`, { message, history }).pipe(
      catchError((err: HttpErrorResponse) => this.toFriendlyError(err))
    );
  }

  // POST /api/coach/help (any authenticated role) — general platform guidance.
  // Examiners and Admins use this; it touches no candidate data.
  help(message: string, history: CoachTurn[] = []): Observable<CoachAnswerModel> {
    return this.http.post<CoachAnswerModel>(`${this.baseUrl}/help`, { message, history }).pipe(
      catchError((err: HttpErrorResponse) => this.toFriendlyError(err))
    );
  }

  // Maps every failure to a sentence the candidate can act on. The backend already returns
  // { status, error } with safe text (no stack traces, no Gemini payloads, no API keys) —
  // this adds the network case and keeps the wording consistent across the screen.
  private toFriendlyError(err: HttpErrorResponse): Observable<never> {
    let message: string;

    if (err.status === 0) {
      message = 'Cannot reach the server. Make sure the backend is running.';
    } else if (err.status === 400) {
      // Validation text from the backend is written for users ("Your question is too long").
      message = err.error?.error ?? 'Please check your question and try again.';
    } else if (err.status === 403) {
      message = err.error?.error
        ?? 'Personalized learning coaching is available to candidates only.';
    } else if (err.status === 429) {
      // The interceptor also shows a global "slow down" alert; this is the inline copy.
      message = 'You are sending questions too quickly. Please wait a moment and try again.';
    } else if (err.status === 503) {
      // Gemini is down / misconfigured. Reassure: nothing else in ExamHub is affected.
      message = err.error?.error
        ?? 'The AI Learning Coach is temporarily unavailable. Your exam data and existing analytics are unaffected.';
    } else {
      message = 'Something went wrong while contacting the AI Learning Coach. Please try again.';
    }

    return throwError(() => new Error(message));
  }
}
