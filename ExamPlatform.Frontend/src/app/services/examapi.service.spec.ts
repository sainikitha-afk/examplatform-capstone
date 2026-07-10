// examapi.service.spec.ts — unit tests for ExamApiService.
// Why it exists: proves every exam endpoint is called with the right method,
// URL, and body — ESPECIALLY the datetime-local → UTC ISO conversion that
// PostgreSQL requires (Kind=Unspecified timestamps are rejected by Npgsql).
import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { ExamApiService } from './examapi.service';
import { CreateExamModel, UpdateExamModel } from '../models/exam.model';

describe('ExamApiService', () => {
  let service: ExamApiService;
  let httpMock: HttpTestingController;
  const baseUrl = 'http://localhost:5014/api/exams';

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(ExamApiService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('loadPublishedExams sends page, pageSize and optional categoryId', () => {
    service.loadPublishedExams(3, 2).subscribe();

    const req = httpMock.expectOne(r => r.url === baseUrl);
    expect(req.request.method).toBe('GET');
    expect(req.request.params.get('page')).toBe('2');
    expect(req.request.params.get('pageSize')).toBe('10');
    expect(req.request.params.get('categoryId')).toBe('3');
    req.flush({ items: [], totalCount: 0 });
  });

  it('loadMyExams calls GET /mine', () => {
    service.loadMyExams().subscribe();
    const req = httpMock.expectOne(r => r.url === `${baseUrl}/mine`);
    expect(req.request.method).toBe('GET');
    req.flush({ items: [], totalCount: 0 });
  });

  it('createExam converts datetime-local strings to UTC ISO before sending', () => {
    const form = new CreateExamModel(1, 'Java Exam', '', 60, 100, 40,
      '2026-07-08T17:00',  // datetime-local value: no timezone info
      '2026-07-08T19:00');

    service.createExam(form).subscribe();

    const req = httpMock.expectOne(baseUrl);
    expect(req.request.method).toBe('POST');
    // Both dates must be full ISO strings ending in Z (UTC) — this is what
    // fixes the Postgres "Kind=Unspecified" 500 error.
    expect(req.request.body.startTime).toMatch(/Z$/);
    expect(req.request.body.endTime).toMatch(/Z$/);
    expect(new Date(req.request.body.startTime).getTime())
      .toBe(new Date('2026-07-08T17:00').getTime());
    req.flush({});
  });

  it('createExam sends endTime as null when left empty (backend defaults it)', () => {
    const form = new CreateExamModel(1, 'Java Exam', '', 60, 100, 40,
      '2026-07-08T17:00', '');   // end time deliberately blank — it is optional

    service.createExam(form).subscribe();

    const req = httpMock.expectOne(baseUrl);
    expect(req.request.body.endTime).toBeNull();
    req.flush({});
  });

  it('updateExam PUTs to /{id} with UTC dates', () => {
    const form = new UpdateExamModel('New Title', '', 90, 100, 50,
      '2026-07-08T17:00', '2026-07-10T17:00');

    service.updateExam(5, form).subscribe();

    const req = httpMock.expectOne(`${baseUrl}/5`);
    expect(req.request.method).toBe('PUT');
    expect(req.request.body.title).toBe('New Title');
    expect(req.request.body.startTime).toMatch(/Z$/);
    req.flush({});
  });

  it('deleteExam sends DELETE /{id}', () => {
    service.deleteExam(4).subscribe();
    const req = httpMock.expectOne(`${baseUrl}/4`);
    expect(req.request.method).toBe('DELETE');
    req.flush(null);
  });

  it('attachQuestions POSTs the question id list', () => {
    service.attachQuestions(2, [7, 8, 9]).subscribe();
    const req = httpMock.expectOne(`${baseUrl}/2/questions`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ questionIds: [7, 8, 9] });
    req.flush({});
  });

  it('detachQuestion sends DELETE /{examId}/questions/{questionId}', () => {
    service.detachQuestion(2, 7).subscribe();
    const req = httpMock.expectOne(`${baseUrl}/2/questions/7`);
    expect(req.request.method).toBe('DELETE');
    req.flush({});
  });

  it('publishExam and publishResults POST to their action endpoints', () => {
    service.publishExam(3).subscribe();
    httpMock.expectOne(`${baseUrl}/3/publish`).flush({});

    service.publishResults(3).subscribe();
    httpMock.expectOne(`${baseUrl}/3/publish-results`).flush({});
  });

  it('unpublishExam POSTs to /{id}/unpublish', () => {
    service.unpublishExam(3).subscribe();
    const req = httpMock.expectOne(`${baseUrl}/3/unpublish`);
    expect(req.request.method).toBe('POST');
    req.flush({});
  });

  it('loadExamAttempts GETs the monitoring list', () => {
    service.loadExamAttempts(3).subscribe();
    const req = httpMock.expectOne(`${baseUrl}/3/attempts`);
    expect(req.request.method).toBe('GET');
    req.flush([]);
  });
});
