// revenueapi.service.spec.ts — unit tests for RevenueApiService.
import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { RevenueApiService } from './revenueapi.service';

describe('RevenueApiService', () => {
  let service: RevenueApiService;
  let httpMock: HttpTestingController;
  const baseUrl = 'http://localhost:5014/api/revenue';

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(RevenueApiService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('loadSummary GETs the admin revenue summary', () => {
    service.loadSummary().subscribe();
    const req = httpMock.expectOne(`${baseUrl}/summary`);
    expect(req.request.method).toBe('GET');
    req.flush({});
  });
});
