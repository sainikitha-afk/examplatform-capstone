// institutionapi.service.spec.ts — unit tests for InstitutionApiService.
import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { InstitutionApiService } from './institutionapi.service';

describe('InstitutionApiService', () => {
  let service: InstitutionApiService;
  let httpMock: HttpTestingController;
  const baseUrl = 'http://localhost:5014/api/institutions';

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(InstitutionApiService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('submit POSTs the name and logo url', () => {
    service.submit('Acme Institute', 'https://cdn/logo.png').subscribe();

    const req = httpMock.expectOne(baseUrl);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ name: 'Acme Institute', logoUrl: 'https://cdn/logo.png' });
    req.flush({});
  });

  it('loadMine GETs the examiner own status', () => {
    service.loadMine().subscribe();
    const req = httpMock.expectOne(`${baseUrl}/mine`);
    expect(req.request.method).toBe('GET');
    req.flush(null);
  });

  it('loadPending GETs the admin queue', () => {
    service.loadPending().subscribe();
    const req = httpMock.expectOne(`${baseUrl}/pending`);
    expect(req.request.method).toBe('GET');
    req.flush([]);
  });

  it('approve POSTs to /{id}/approve', () => {
    service.approve(5).subscribe();
    const req = httpMock.expectOne(`${baseUrl}/5/approve`);
    expect(req.request.method).toBe('POST');
    req.flush({});
  });

  it('reject POSTs to /{id}/reject', () => {
    service.reject(5).subscribe();
    const req = httpMock.expectOne(`${baseUrl}/5/reject`);
    expect(req.request.method).toBe('POST');
    req.flush({});
  });
});
