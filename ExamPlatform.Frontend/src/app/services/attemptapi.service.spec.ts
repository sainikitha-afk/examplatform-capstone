// attemptapi.service.spec.ts — unit tests for AttemptApiService.
// Covers the exam-taking lifecycle: start, auto-save answer, submit, result.
import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { AttemptApiService } from './attemptapi.service';
import { SaveAnswerModel } from '../models/attempt.model';

describe('AttemptApiService', () => {
  let service: AttemptApiService;
  let httpMock: HttpTestingController;
  const baseUrl = 'http://localhost:5014/api/attempts';

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(AttemptApiService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('startAttempt POSTs the exam id to /start', () => {
    service.startAttempt(9).subscribe();
    const req = httpMock.expectOne(`${baseUrl}/start`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ examId: 9 });
    req.flush({});
  });

  it('loadAttemptById GETs /{id} (resume view)', () => {
    service.loadAttemptById(4).subscribe();
    const req = httpMock.expectOne(`${baseUrl}/4`);
    expect(req.request.method).toBe('GET');
    req.flush({});
  });

  it('saveAnswer PUTs the answer for auto-save', () => {
    service.saveAnswer(4, new SaveAnswerModel(16, 63)).subscribe();
    const req = httpMock.expectOne(`${baseUrl}/4/answers`);
    expect(req.request.method).toBe('PUT');
    expect(req.request.body.questionId).toBe(16);
    expect(req.request.body.selectedOptionId).toBe(63);
    req.flush(null);
  });

  it('submitAttempt POSTs to /{id}/submit', () => {
    service.submitAttempt(4).subscribe();
    const req = httpMock.expectOne(`${baseUrl}/4/submit`);
    expect(req.request.method).toBe('POST');
    req.flush({});
  });

  it('loadMyAttempts GETs /mine', () => {
    service.loadMyAttempts().subscribe();
    const req = httpMock.expectOne(r => r.url === `${baseUrl}/mine`);
    expect(req.request.method).toBe('GET');
    req.flush({ items: [], totalCount: 0 });
  });

  it('loadAttemptResult GETs /{id}/result', () => {
    service.loadAttemptResult(4).subscribe();
    const req = httpMock.expectOne(`${baseUrl}/4/result`);
    expect(req.request.method).toBe('GET');
    req.flush({});
  });
});
