// payment-success.spec.ts — reads payment from history.state, renders
// success vs. every failure label, and routes Continue/Try Again by role.
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { PaymentSuccess } from './payment-success';

describe('PaymentSuccess', () => {
  let fixture: ComponentFixture<PaymentSuccess>;
  let component: PaymentSuccess;

  function setup(payment: any) {
    history.pushState({ payment }, '', '/payment-success/1');
    TestBed.configureTestingModule({
      imports: [PaymentSuccess],
      providers: [provideRouter([])],
    });
    fixture = TestBed.createComponent(PaymentSuccess);
    component = fixture.componentInstance;
    fixture.detectChanges();
  }

  afterEach(() => sessionStorage.clear());

  it('renders the success state with transaction details', () => {
    setup({ paymentId: 1, status: 'Success', amount: 999, method: 'CreditCard',
      fakeTransactionId: 'TXN-1', fakeGatewayReference: 'GTW-1' });

    expect(component.isSuccess).toBe(true);
    expect(fixture.nativeElement.textContent).toContain('Payment Successful');
    expect(fixture.nativeElement.textContent).toContain('TXN-1');
  });

  it('renders a specific failure label for a declined card', () => {
    setup({ paymentId: 2, status: 'Failed', failureReason: 'Card declined.', amount: 999,
      fakeTransactionId: 'TXN-2', fakeGatewayReference: 'GTW-2' });

    expect(component.isSuccess).toBe(false);
    expect(component.failureLabel).toBe('Payment Declined');
    expect(fixture.nativeElement.textContent).toContain('Card declined.');
  });

  it('renders the invoice block when present', () => {
    setup({
      paymentId: 1, status: 'Success', amount: 999, fakeTransactionId: 'T', fakeGatewayReference: 'G',
      invoice: { invoiceNumber: 'INV-2026-000001', fakeGstNumber: '29AAAAA0000EXM1Z5',
        subtotal: 999, discountAmount: 0, taxAmount: 179.82, total: 1178.82 }
    });

    expect(fixture.nativeElement.textContent).toContain('INV-2026-000001');
  });

  it('shows a warning when no payment state is present', () => {
    history.pushState({}, '', '/payment-success/1');
    TestBed.configureTestingModule({
      imports: [PaymentSuccess],
      providers: [provideRouter([])],
    });
    fixture = TestBed.createComponent(PaymentSuccess);
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('No payment information found');
  });

  it('Continue routes an examiner to their subscription dashboard', () => {
    sessionStorage.setItem('role', 'Examiner');
    setup({ paymentId: 1, status: 'Success', amount: 1, fakeTransactionId: 'T', fakeGatewayReference: 'G' });
    const routerSpy = vi.spyOn((component as any).router, 'navigate');

    component.handleContinueClick();

    expect(routerSpy).toHaveBeenCalledWith(['/examiner/subscription']);
  });

  it('Continue routes a candidate to certificates', () => {
    sessionStorage.setItem('role', 'Candidate');
    setup({ paymentId: 1, status: 'Success', amount: 1, fakeTransactionId: 'T', fakeGatewayReference: 'G' });
    const routerSpy = vi.spyOn((component as any).router, 'navigate');

    component.handleContinueClick();

    expect(routerSpy).toHaveBeenCalledWith(['/my-certificates']);
  });
});
