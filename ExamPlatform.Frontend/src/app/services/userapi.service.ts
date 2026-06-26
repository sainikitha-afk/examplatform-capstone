// UserApiService — HTTP calls for /api/users endpoints (Admin operations).
// Why it exists: Admin screens (manage-users, manage-examiners) need typed user data.
// How Angular uses it: injected via constructor; returns Observables the component subscribes to.

import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { UserModel, CreateExaminerModel } from '../models/user.model';
import { PagedResultModel } from '../models/paged-result.model';

@Injectable({ providedIn: 'root' })
export class UserApiService {
  private baseUrl = 'http://localhost:5014/api/users';

  constructor(private http: HttpClient) {}

  // GET /api/users/me — current user profile
  loadMyProfile(): Observable<UserModel> {
    return this.http.get<UserModel>(`${this.baseUrl}/me`);
  }

  // GET /api/users?page=&pageSize= (Admin) — paginated user list
  loadUsers(page: number = 1, pageSize: number = 10): Observable<PagedResultModel<UserModel>> {
    const params = new HttpParams().set('page', page).set('pageSize', pageSize);
    return this.http.get<PagedResultModel<UserModel>>(this.baseUrl, { params });
  }

  // PATCH /api/users/{id}/active — toggle isActive (Admin)
  toggleUserActive(id: number, isActive: boolean): Observable<UserModel> {
    return this.http.patch<UserModel>(`${this.baseUrl}/${id}/active`, { isActive });
  }

  // POST /api/users/examiners (Admin) — create an examiner account
  createExaminer(data: CreateExaminerModel): Observable<UserModel> {
    return this.http.post<UserModel>(`${this.baseUrl}/examiners`, data);
  }
}
