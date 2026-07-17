// RevenueApiService — HTTP calls for /api/revenue (Admin Revenue Dashboard).

import { Injectable } from '@angular/core';
import { environment } from '../../environments/environment';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { RevenueSummaryModel } from '../models/subscription.model';

@Injectable({ providedIn: 'root' })
export class RevenueApiService {
  private baseUrl = environment.apiBaseUrl + '/api/revenue';

  constructor(private http: HttpClient) {}

  // GET /api/revenue/summary — admin only
  loadSummary(): Observable<RevenueSummaryModel> {
    return this.http.get<RevenueSummaryModel>(`${this.baseUrl}/summary`);
  }
}
