// admin-institution-verifications.spec.ts — loads the pending queue, and
// approve/reject both reload the list afterward.
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { AdminInstitutionVerifications } from './admin-institution-verifications';

describe('AdminInstitutionVerifications', () => {
  let fixture: ComponentFixture<AdminInstitutionVerifications>;
  let component: AdminInstitutionVerifications;
  let httpMock: HttpTestingController;
  const url = 'http://localhost:5014/api/institutions/pending';

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [AdminInstitutionVerifications],
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    httpMock = TestBed.inject(HttpTestingController);
    fixture = TestBed.createComponent(AdminInstitutionVerifications);
    component = fixture.componentInstance;
  });

  afterEach(() => httpMock.verify());

  it('shows the empty state when there are no pending requests', () => {
    fixture.detectChanges();
    httpMock.expectOne(url).flush([]);
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('No pending institute verification requests');
  });

  it('renders a pending request and approves it', () => {
    fixture.detectChanges();
    httpMock.expectOne(url).flush([
      { institutionId: 5, name: 'Acme Institute', verificationStatus: 'Pending', requestedAt: '2026-07-01T00:00:00Z', ownerName: 'Prof Raju' }
    ]);
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('Acme Institute');

    component.handleApproveClick(5);
    const approveReq = httpMock.expectOne('http://localhost:5014/api/institutions/5/approve');
    approveReq.flush({});
    httpMock.expectOne(url).flush([]); // reload after approve

    expect(component.processingId).toBeNull();
  });

  it('rejects a request', () => {
    fixture.detectChanges();
    httpMock.expectOne(url).flush([
      { institutionId: 5, name: 'Acme Institute', verificationStatus: 'Pending', requestedAt: '2026-07-01T00:00:00Z', ownerName: 'Prof Raju' }
    ]);
    fixture.detectChanges();

    component.handleRejectClick(5);
    httpMock.expectOne('http://localhost:5014/api/institutions/5/reject').flush({});
    httpMock.expectOne(url).flush([]);

    expect(component.processingId).toBeNull();
  });
});
