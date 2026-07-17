// AnalyticsApiService — HTTP calls for /api/analytics endpoints.
// Why it exists: candidate-analytics and admin-dashboard screens need platform/personal stats.
// How Angular uses it: constructor-injected; returns Observables.
// CONCEPT DEMO: tap operator — logs analytics responses for debugging without altering the data.

import { Injectable } from '@angular/core';
import { environment } from '../../environments/environment';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { tap } from 'rxjs/operators';
import { CandidateDashboardModel, PlatformStatsModel } from '../models/analytics.model';

@Injectable({ providedIn: 'root' })
export class AnalyticsApiService {
  private baseUrl = environment.apiBaseUrl + '/api/analytics';

  constructor(private http: HttpClient) {}

  // GET /api/analytics/me (Candidate) — personal dashboard stats
  // CONCEPT DEMO: tap logs the received data without changing it.
  loadCandidateDashboard(): Observable<CandidateDashboardModel> {
    return this.http.get<CandidateDashboardModel>(`${this.baseUrl}/me`).pipe(
      tap(data => console.log('[Analytics] Candidate dashboard:', data))
    );
  }

  // GET /api/analytics/platform (Admin) — platform-wide statistics
  loadPlatformStats(): Observable<PlatformStatsModel> {
    return this.http.get<PlatformStatsModel>(`${this.baseUrl}/platform`).pipe(
      tap(data => console.log('[Analytics] Platform stats:', data))
    );
  }
}
