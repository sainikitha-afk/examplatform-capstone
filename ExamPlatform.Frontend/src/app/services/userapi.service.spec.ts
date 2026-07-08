// userapi.service.spec.ts — unit tests for UserApiService (admin user management).
import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { UserApiService } from './userapi.service';
import { CreateExaminerModel } from '../models/user.model';

describe('UserApiService', () => {
  let service: UserApiService;
  let httpMock: HttpTestingController;
  const baseUrl = 'http://localhost:5014/api/users';

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(UserApiService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('loadMyProfile GETs /me', () => {
    service.loadMyProfile().subscribe();
    const req = httpMock.expectOne(`${baseUrl}/me`);
    expect(req.request.method).toBe('GET');
    req.flush({});
  });

  it('loadUsers GETs the paged list with page parameters', () => {
    service.loadUsers(2).subscribe();
    const req = httpMock.expectOne(r => r.url === baseUrl);
    expect(req.request.method).toBe('GET');
    expect(req.request.params.get('page')).toBe('2');
    req.flush({ items: [], totalCount: 0 });
  });

  it('toggleUserActive PATCHes /{id}/active with the new status', () => {
    service.toggleUserActive(5, false).subscribe();
    const req = httpMock.expectOne(`${baseUrl}/5/active`);
    expect(req.request.method).toBe('PATCH');
    expect(req.request.body).toEqual({ isActive: false });
    req.flush({});
  });

  it('createExaminer POSTs to /examiners', () => {
    service.createExaminer(new CreateExaminerModel('Bob', 'bob', 'bob@t.com', 'secret1')).subscribe();
    const req = httpMock.expectOne(`${baseUrl}/examiners`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body.username).toBe('bob');
    req.flush({});
  });
});
