// candidate-purchases.spec.ts — loads purchase history, renders empty and
// populated states.
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { CandidatePurchases } from './candidate-purchases';

describe('CandidatePurchases', () => {
  let fixture: ComponentFixture<CandidatePurchases>;
  let component: CandidatePurchases;
  let httpMock: HttpTestingController;
  const url = 'http://localhost:5014/api/purchases/mine';

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [CandidatePurchases],
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])],
    });
    httpMock = TestBed.inject(HttpTestingController);
    fixture = TestBed.createComponent(CandidatePurchases);
    component = fixture.componentInstance;
  });

  afterEach(() => httpMock.verify());

  it('shows the empty state when there are no purchases', () => {
    fixture.detectChanges();
    httpMock.expectOne(url).flush([]);
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain("haven't purchased");
  });

  it('renders a purchase row', () => {
    fixture.detectChanges();
    httpMock.expectOne(url).flush([
      { purchaseId: 1, examId: 5, examTitle: 'Java Certification', status: 'Active',
        purchasedAt: '2026-07-01T00:00:00Z', amount: 999, invoiceNumber: 'INV-2026-000001' }
    ]);
    fixture.detectChanges();

    expect(component.purchases.length).toBe(1);
    expect(fixture.nativeElement.textContent).toContain('Java Certification');
    expect(fixture.nativeElement.textContent).toContain('INV-2026-000001');
  });

  it('shows an error message on load failure', () => {
    fixture.detectChanges();
    httpMock.expectOne(url).flush('err', { status: 500, statusText: 'Server Error' });
    fixture.detectChanges();

    expect(component.errorMessage).toContain('Could not load');
  });
});
