// certificateapi.service.spec.ts — unit tests for CertificateApiService.
import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { CertificateApiService } from './certificateapi.service';

describe('CertificateApiService', () => {
  let service: CertificateApiService;
  let httpMock: HttpTestingController;
  const baseUrl = 'http://localhost:5014/api/certificates';

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(CertificateApiService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('loadMyCertificates GETs /mine', () => {
    service.loadMyCertificates().subscribe();
    const req = httpMock.expectOne(`${baseUrl}/mine`);
    expect(req.request.method).toBe('GET');
    req.flush([]);
  });

  it('downloadCertificate GETs /{id}/download', () => {
    service.downloadCertificate(3).subscribe();
    const req = httpMock.expectOne(`${baseUrl}/3/download`);
    expect(req.request.method).toBe('GET');
    req.flush({});
  });

  it('revokeCertificate POSTs to /{id}/revoke', () => {
    service.revokeCertificate(3).subscribe();
    const req = httpMock.expectOne(`${baseUrl}/3/revoke`);
    expect(req.request.method).toBe('POST');
    req.flush(null);
  });
});
