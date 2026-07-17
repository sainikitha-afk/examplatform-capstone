// examiner-subscription.spec.ts — status/billing/institution loading, the
// upgrade/decline dialog wiring, and institution submission.
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { ExaminerSubscription } from './examiner-subscription';

describe('ExaminerSubscription', () => {
  let fixture: ComponentFixture<ExaminerSubscription>;
  let component: ExaminerSubscription;
  let httpMock: HttpTestingController;
  const subUrl = 'http://localhost:5014/api/subscriptions';
  const instUrl = 'http://localhost:5014/api/institutions';

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [ExaminerSubscription],
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])],
    });
    httpMock = TestBed.inject(HttpTestingController);
    fixture = TestBed.createComponent(ExaminerSubscription);
    component = fixture.componentInstance;
  });

  afterEach(() => httpMock.verify());

  function flushInit(status: any, institution: any = null) {
    fixture.detectChanges();
    httpMock.expectOne(`${subUrl}/mine`).flush(status);
    httpMock.expectOne(`${subUrl}/billing-history`).flush([]);
    httpMock.expectOne(`${instUrl}/mine`).flush(institution);
    fixture.detectChanges();
  }

  it('loads status, billing history, and institution on init', () => {
    flushInit({ status: 'TrialActive', planName: 'Free Trial', trialExamsUsed: 3, maxExamsLifetime: 8, upgradeDialog: 'none' });

    expect(component.status.planName).toBe('Free Trial');
    expect(fixture.nativeElement.textContent).toContain('TrialActive');
  });

  it('shows the upgrade dialog when upgradeDialog is "upgrade"', () => {
    flushInit({ status: 'TrialExhausted', planName: 'Free Trial', trialExamsUsed: 8, maxExamsLifetime: 8, upgradeDeclineCount: 0, upgradeDialog: 'upgrade' });

    expect(fixture.nativeElement.textContent).toContain("You've successfully created 8 exams");
  });

  it('shows the 25%-off dialog when upgradeDialog is "discount"', () => {
    flushInit({ status: 'TrialExhausted', planName: 'Free Trial', trialExamsUsed: 8, maxExamsLifetime: 8, upgradeDeclineCount: 1, upgradeDialog: 'discount' });

    expect(fixture.nativeElement.textContent).toContain('25% off Professional');
  });

  it('handleUpgradeClick navigates to checkout with the annual query param', () => {
    flushInit({ status: 'TrialActive', planName: 'Free Trial', upgradeDialog: 'none' });
    const routerSpy = vi.spyOn((component as any).router, 'navigate');

    component.handleUpgradeClick(true);

    expect(routerSpy).toHaveBeenCalledWith(['/checkout/subscription'], { queryParams: { annual: true } });
  });

  it('handleDeclineClick posts and reloads status', () => {
    flushInit({ status: 'TrialExhausted', planName: 'Free Trial', upgradeDeclineCount: 0, upgradeDialog: 'upgrade' });

    component.handleDeclineClick();
    httpMock.expectOne(`${subUrl}/decline-upgrade`).flush({ upgradeDeclineCount: 1, nextStep: 'discount' });
    httpMock.expectOne(`${subUrl}/mine`).flush({ status: 'TrialExhausted', planName: 'Free Trial', upgradeDeclineCount: 1, upgradeDialog: 'discount' });

    expect(component.status.upgradeDialog).toBe('discount');
  });

  it('submits an institution and shows the pending status', () => {
    flushInit({ status: 'ProfessionalActive', planName: 'Professional', canCreatePremiumExams: true, upgradeDialog: 'none' }, null);

    component.institutionName = 'Acme Institute';
    component.handleSubmitInstitution();

    const req = httpMock.expectOne(instUrl);
    expect(req.request.body).toEqual({ name: 'Acme Institute', logoUrl: null });
    req.flush({ institutionId: 1, name: 'Acme Institute', verificationStatus: 'Pending' });

    expect(component.institution?.verificationStatus).toBe('Pending');
  });
});
