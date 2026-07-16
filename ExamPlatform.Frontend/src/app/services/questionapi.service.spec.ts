// questionapi.service.spec.ts — unit tests for QuestionApiService.
// Covers question CRUD, server-side filters, and the Excel import upload.
import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { QuestionApiService } from './questionapi.service';
import { CreateQuestionModel } from '../models/question.model';

describe('QuestionApiService', () => {
  let service: QuestionApiService;
  let httpMock: HttpTestingController;
  const baseUrl = 'http://localhost:5014/api/questions';

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(QuestionApiService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('loadQuestions passes filters as query parameters', () => {
    service.loadQuestions(2, 'Easy', 'linq', 1).subscribe();

    const req = httpMock.expectOne(r => r.url === baseUrl);
    expect(req.request.method).toBe('GET');
    expect(req.request.params.get('categoryId')).toBe('2');
    expect(req.request.params.get('difficulty')).toBe('Easy');
    expect(req.request.params.get('search')).toBe('linq');
    req.flush({ items: [], totalCount: 0 });
  });

  it('omits categoryId entirely when no category is selected (null)', () => {
    // Regression: "All Categories" must NOT send ?categoryId=null (→ HTTP 400).
    service.loadQuestions(null, '', '', 1, 10).subscribe();
    const req = httpMock.expectOne(r => r.url === baseUrl);
    expect(req.request.params.has('categoryId')).toBe(false);
    expect(req.request.params.get('page')).toBe('1');
    expect(req.request.params.get('pageSize')).toBe('10');
    req.flush({ items: [], totalCount: 0 });
  });

  it('omits categoryId when handed the string "null" (defensive)', () => {
    // A native <select> bound with [value] can yield the string "null".
    service.loadQuestions('null' as any, '', '', 1, 10).subscribe();
    const req = httpMock.expectOne(r => r.url === baseUrl);
    expect(req.request.params.has('categoryId')).toBe(false);
    req.flush({ items: [], totalCount: 0 });
  });

  it('sends a numeric categoryId when a category is selected', () => {
    service.loadQuestions(7, '', '', 1, 10).subscribe();
    const req = httpMock.expectOne(r => r.url === baseUrl);
    expect(req.request.params.get('categoryId')).toBe('7');
    req.flush({ items: [], totalCount: 0 });
  });

  it('omits difficulty and search when cleared, but sends them when set', () => {
    service.loadQuestions(null, '', '', 1, 10).subscribe();
    const req1 = httpMock.expectOne(r => r.url === baseUrl);
    expect(req1.request.params.has('difficulty')).toBe(false);
    expect(req1.request.params.has('search')).toBe(false);
    req1.flush({ items: [], totalCount: 0 });

    service.loadQuestions(null, 'Hard', 'sql', 1, 10).subscribe();
    const req2 = httpMock.expectOne(r => r.url === baseUrl);
    expect(req2.request.params.get('difficulty')).toBe('Hard');
    expect(req2.request.params.get('search')).toBe('sql');
    req2.flush({ items: [], totalCount: 0 });
  });

  it('createQuestion POSTs the MCQ payload', () => {
    const q = new CreateQuestionModel(1, 'What is DI?', 10, 'Medium');
    service.createQuestion(q).subscribe();

    const req = httpMock.expectOne(baseUrl);
    expect(req.request.method).toBe('POST');
    expect(req.request.body.questionText).toBe('What is DI?');
    req.flush({});
  });

  it('updateQuestion PUTs to /{id}', () => {
    service.updateQuestion(7, new CreateQuestionModel(1, 'Updated?', 10, 'Hard')).subscribe();
    const req = httpMock.expectOne(`${baseUrl}/7`);
    expect(req.request.method).toBe('PUT');
    req.flush({});
  });

  it('deleteQuestion sends DELETE /{id}', () => {
    service.deleteQuestion(7).subscribe();
    const req = httpMock.expectOne(`${baseUrl}/7`);
    expect(req.request.method).toBe('DELETE');
    req.flush(null);
  });

  it('importExcel uploads the file as multipart FormData', () => {
    const file = new File(['dummy'], 'questions.xlsx');
    service.importExcel(file).subscribe();

    const req = httpMock.expectOne(`${baseUrl}/import`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body instanceof FormData).toBe(true);
    req.flush({ successCount: 0, failureCount: 0, errors: [] });
  });

  it('downloadImportTemplate requests a blob', () => {
    service.downloadImportTemplate().subscribe();
    const req = httpMock.expectOne(`${baseUrl}/import-template`);
    expect(req.request.responseType).toBe('blob');
    req.flush(new Blob());
  });
});
