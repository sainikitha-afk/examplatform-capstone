// admin-revenue-dashboard.spec.ts — loads the summary and renders the
// chart-backing data / stat cards / tables correctly.
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { AdminRevenueDashboard } from './admin-revenue-dashboard';

describe('AdminRevenueDashboard', () => {
  let fixture: ComponentFixture<AdminRevenueDashboard>;
  let component: AdminRevenueDashboard;
  let httpMock: HttpTestingController;
  const url = 'http://localhost:5014/api/revenue/summary';

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [AdminRevenueDashboard],
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    httpMock = TestBed.inject(HttpTestingController);
    fixture = TestBed.createComponent(AdminRevenueDashboard);
    component = fixture.componentInstance;
  });

  afterEach(() => httpMock.verify());

  it('loads the summary and builds both charts', () => {
    fixture.detectChanges();
    httpMock.expectOne(url).flush({
      totalRevenue: 5000, subscriptionRevenue: 3000, examCertificationRevenue: 2000,
      totalSuccessfulPayments: 10, totalFailedPayments: 2, paymentFailureRate: 0.1667,
      activeTrialCount: 3, activeProfessionalCount: 2, activeFreeForeverCount: 1,
      trialToPaidConversionRate: 0.667, couponRedemptions: 1,
      monthlyRevenue: [{ month: '2026-06', revenue: 2500 }, { month: '2026-07', revenue: 2500 }],
      planDistribution: [{ status: 'TrialActive', count: 3 }, { status: 'ProfessionalActive', count: 2 }],
      topPurchasedExams: [{ examTitle: 'Java Certification', purchaseCount: 3, revenue: 1500 }],
      recentTransactions: [{ date: '2026-07-15T00:00:00Z', payerName: 'John Candidate', amount: 999, status: 'Success', purpose: 'ExamCertificationPurchase' }]
    });
    fixture.detectChanges();

    expect(component.summary.totalRevenue).toBe(5000);
    expect(component.monthlyChartData.labels).toEqual(['2026-06', '2026-07']);
    expect(component.planChartData.datasets[0].data).toEqual([3, 2]);
    expect(fixture.nativeElement.textContent).toContain('Java Certification');
    expect(fixture.nativeElement.textContent).toContain('John Candidate');
  });

  it('shows an error message on load failure', () => {
    fixture.detectChanges();
    httpMock.expectOne(url).flush('err', { status: 500, statusText: 'Server Error' });
    fixture.detectChanges();

    expect(component.errorMessage).toContain('Could not load');
  });
});
