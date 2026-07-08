// analyticsapi.service.spec.ts — unit tests for AnalyticsApiService.
import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { AnalyticsApiService } from './analyticsapi.service';

describe('AnalyticsApiService', () => {
  let service: AnalyticsApiService;
  let httpMock: HttpTestingController;
  const baseUrl = 'http://localhost:5014/api/analytics';

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(AnalyticsApiService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('loadCandidateDashboard GETs /me', () => {
    service.loadCandidateDashboard().subscribe();
    const req = httpMock.expectOne(`${baseUrl}/me`);
    expect(req.request.method).toBe('GET');
    req.flush({});
  });

  it('loadPlatformStats GETs /platform (admin)', () => {
    service.loadPlatformStats().subscribe();
    const req = httpMock.expectOne(`${baseUrl}/platform`);
    expect(req.request.method).toBe('GET');
    req.flush({});
  });
});
