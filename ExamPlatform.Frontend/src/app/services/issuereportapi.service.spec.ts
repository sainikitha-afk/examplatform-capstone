// issuereportapi.service.spec.ts — unit tests for IssueReportApiService.
import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { IssueReportApiService } from './issuereportapi.service';
import { CreateIssueReportModel } from '../models/issue-report.model';

describe('IssueReportApiService', () => {
  let service: IssueReportApiService;
  let httpMock: HttpTestingController;
  const baseUrl = 'http://localhost:5014/api/issue-reports';

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(IssueReportApiService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('raiseIssue POSTs the report payload', () => {
    const payload = new CreateIssueReportModel(5, 12, 7, 'Two options look correct');
    service.raiseIssue(payload).subscribe();
    const req = httpMock.expectOne(baseUrl);
    expect(req.request.method).toBe('POST');
    expect(req.request.body.examId).toBe(5);
    expect(req.request.body.questionId).toBe(7);
    req.flush({});
  });

  it('loadForExaminer GETs with paging + openOnly filter', () => {
    service.loadForExaminer(2, true).subscribe();
    const req = httpMock.expectOne(`${baseUrl}?page=2&pageSize=20&openOnly=true`);
    expect(req.request.method).toBe('GET');
    req.flush({ items: [], totalCount: 0 });
  });

  it('loadOpenCount GETs /open-count (drives the badge)', () => {
    let count = -1;
    service.loadOpenCount().subscribe(r => count = r.count);
    const req = httpMock.expectOne(`${baseUrl}/open-count`);
    expect(req.request.method).toBe('GET');
    req.flush({ count: 3 });
    expect(count).toBe(3);
  });

  it('updateStatus POSTs to /{id}/status with status + note', () => {
    service.updateStatus(9, 'Resolved', 'Fixed the typo').subscribe();
    const req = httpMock.expectOne(`${baseUrl}/9/status`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body.status).toBe('Resolved');
    expect(req.request.body.resolutionNote).toBe('Fixed the typo');
    req.flush({});
  });
});
