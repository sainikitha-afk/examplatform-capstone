// notificationapi.service.spec.ts — unit tests for NotificationApiService.
import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { NotificationApiService } from './notificationapi.service';

describe('NotificationApiService', () => {
  let service: NotificationApiService;
  let httpMock: HttpTestingController;
  const baseUrl = 'http://localhost:5014/api/notifications';

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(NotificationApiService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('loadNotifications GETs the inbox', () => {
    service.loadNotifications().subscribe();
    const req = httpMock.expectOne(r => r.url === baseUrl);
    expect(req.request.method).toBe('GET');
    req.flush({ items: [], totalCount: 0 });
  });

  it('loadUnreadCount GETs /unread-count (drives the navbar badge)', () => {
    let count = -1;
    service.loadUnreadCount().subscribe(r => count = r.count);
    const req = httpMock.expectOne(`${baseUrl}/unread-count`);
    expect(req.request.method).toBe('GET');
    req.flush({ count: 4 });
    expect(count).toBe(4);
  });

  it('markAsRead POSTs to /{id}/read', () => {
    service.markAsRead(11).subscribe();
    const req = httpMock.expectOne(`${baseUrl}/11/read`);
    expect(req.request.method).toBe('POST');
    req.flush({});
  });

  it('markAllAsRead POSTs to /read-all', () => {
    service.markAllAsRead().subscribe();
    const req = httpMock.expectOne(`${baseUrl}/read-all`);
    expect(req.request.method).toBe('POST');
    req.flush(null);
  });
});
