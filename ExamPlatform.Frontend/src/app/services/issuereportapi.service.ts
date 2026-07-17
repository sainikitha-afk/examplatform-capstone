// IssueReportApiService — HTTP calls for /api/issue-reports.
// Candidates raise reports (flag a question / report a problem); examiners list
// and triage them. Mirrors the other *ApiService files: injectable singleton,
// constructor-injected HttpClient, one method per endpoint.

import { Injectable } from '@angular/core';
import { environment } from '../../environments/environment';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { IssueReportModel, CreateIssueReportModel } from '../models/issue-report.model';
import { PagedResultModel } from '../models/paged-result.model';

@Injectable({ providedIn: 'root' })
export class IssueReportApiService {
  private baseUrl = environment.apiBaseUrl + '/api/issue-reports';

  constructor(private http: HttpClient) {}

  // POST /api/issue-reports — candidate raises a report.
  raiseIssue(payload: CreateIssueReportModel): Observable<IssueReportModel> {
    return this.http.post<IssueReportModel>(this.baseUrl, payload);
  }

  // GET /api/issue-reports — examiner inbox (their exams' reports).
  loadForExaminer(page: number = 1, openOnly: boolean = false)
    : Observable<PagedResultModel<IssueReportModel>> {
    return this.http.get<PagedResultModel<IssueReportModel>>(
      `${this.baseUrl}?page=${page}&pageSize=20&openOnly=${openOnly}`);
  }

  // GET /api/issue-reports/open-count — badge count of open reports.
  loadOpenCount(): Observable<{ count: number }> {
    return this.http.get<{ count: number }>(`${this.baseUrl}/open-count`);
  }

  // POST /api/issue-reports/{id}/status — examiner reviews/resolves a report.
  updateStatus(id: number, status: string, resolutionNote: string | null = null)
    : Observable<IssueReportModel> {
    return this.http.post<IssueReportModel>(
      `${this.baseUrl}/${id}/status`, { status, resolutionNote });
  }
}
