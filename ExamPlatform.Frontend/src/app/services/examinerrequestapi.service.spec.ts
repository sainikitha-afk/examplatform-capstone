// examinerrequestapi.service.spec.ts — unit tests for ExaminerRequestApiService.
// Covers the public "apply as examiner" form and the admin approve/reject flow.
import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { ExaminerRequestApiService } from './examinerrequestapi.service';
import { SubmitExaminerRequestModel } from '../models/examiner-request.model';

describe('ExaminerRequestApiService', () => {
  let service: ExaminerRequestApiService;
  let httpMock: HttpTestingController;
  const baseUrl = 'http://localhost:5014/api/examiner-requests';

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(ExaminerRequestApiService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('submit POSTs the public application', () => {
    const model = new SubmitExaminerRequestModel();
    model.fullName = 'Riya Sharma';
    model.email = 'riya@test.com';
    model.purpose = 'I teach Java and want to create assessments.';

    service.submit(model).subscribe();

    const req = httpMock.expectOne(baseUrl);
    expect(req.request.method).toBe('POST');
    expect(req.request.body.email).toBe('riya@test.com');
    req.flush({});
  });

  it('getAll and getPending GET the admin lists', () => {
    service.getAll().subscribe();
    httpMock.expectOne(baseUrl).flush([]);

    service.getPending().subscribe();
    httpMock.expectOne(`${baseUrl}/pending`).flush([]);
  });

  it('approve POSTs to /{id}/approve', () => {
    service.approve(2).subscribe();
    const req = httpMock.expectOne(`${baseUrl}/2/approve`);
    expect(req.request.method).toBe('POST');
    req.flush({});
  });

  it('reject POSTs the admin note to /{id}/reject', () => {
    service.reject(2, 'Insufficient details').subscribe();
    const req = httpMock.expectOne(`${baseUrl}/2/reject`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ adminNote: 'Insufficient details' });
    req.flush({});
  });
});
