// NotificationApiService — HTTP calls for /api/notifications endpoints.
// Why it exists: notifications screen and the navigation badge count both need this data.
// How Angular uses it: constructor-injected; returns Observables.
// CONCEPT DEMO: tap operator — logs each notification response to the console so the student
//   can see real API data flowing through the Observable chain without modifying the data.

import { Injectable } from '@angular/core';
import { environment } from '../../environments/environment';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { tap } from 'rxjs/operators';
import { NotificationModel, UnreadCountModel } from '../models/notification.model';
import { PagedResultModel } from '../models/paged-result.model';

@Injectable({ providedIn: 'root' })
export class NotificationApiService {
  private baseUrl = environment.apiBaseUrl + '/api/notifications';

  constructor(private http: HttpClient) {}

  // GET /api/notifications — paginated list (newest first)
  // CONCEPT DEMO: tap — logs the response without changing it (side-effect only).
  loadNotifications(page: number = 1): Observable<PagedResultModel<NotificationModel>> {
    return this.http.get<PagedResultModel<NotificationModel>>(this.baseUrl).pipe(
      tap(result => console.log(`[Notifications] Loaded ${result.items.length} items`))
    );
  }

  // GET /api/notifications/unread-count — badge number
  loadUnreadCount(): Observable<UnreadCountModel> {
    return this.http.get<UnreadCountModel>(`${this.baseUrl}/unread-count`);
  }

  // POST /api/notifications/{id}/read — mark one as read
  markAsRead(id: number): Observable<NotificationModel> {
    return this.http.post<NotificationModel>(`${this.baseUrl}/${id}/read`, {});
  }

  // POST /api/notifications/read-all → 204
  markAllAsRead(): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/read-all`, {});
  }
}
