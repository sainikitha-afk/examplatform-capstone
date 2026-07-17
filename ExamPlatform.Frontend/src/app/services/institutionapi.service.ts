// InstitutionApiService — HTTP calls for /api/institutions (Institute
// Verification badge: examiner submit, admin approve/reject).

import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { InstitutionModel } from '../models/subscription.model';

@Injectable({ providedIn: 'root' })
export class InstitutionApiService {
  private baseUrl = 'http://localhost:5014/api/institutions';

  constructor(private http: HttpClient) {}

  // POST /api/institutions — examiner submit
  submit(name: string, logoUrl: string | null): Observable<InstitutionModel> {
    return this.http.post<InstitutionModel>(this.baseUrl, { name, logoUrl });
  }

  // GET /api/institutions/mine — examiner's own request/status
  loadMine(): Observable<InstitutionModel | null> {
    return this.http.get<InstitutionModel | null>(`${this.baseUrl}/mine`);
  }

  // GET /api/institutions/pending — admin action queue
  loadPending(): Observable<InstitutionModel[]> {
    return this.http.get<InstitutionModel[]>(`${this.baseUrl}/pending`);
  }

  // POST /api/institutions/{id}/approve — admin
  approve(id: number): Observable<InstitutionModel> {
    return this.http.post<InstitutionModel>(`${this.baseUrl}/${id}/approve`, {});
  }

  // POST /api/institutions/{id}/reject — admin
  reject(id: number): Observable<InstitutionModel> {
    return this.http.post<InstitutionModel>(`${this.baseUrl}/${id}/reject`, {});
  }
}
