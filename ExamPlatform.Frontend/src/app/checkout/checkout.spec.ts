// checkout.spec.ts — context detection (subscription vs exam), submit
// validation, and that the correct API is called per context.
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { provideRouter, ActivatedRoute } from '@angular/router';
import { Checkout } from './checkout';

describe('Checkout', () => {
  let fixture: ComponentFixture<Checkout>;
  let component: Checkout;
  let httpMock: HttpTestingController;

  function setup(params: any, queryParams: any = {}) {
    TestBed.configureTestingModule({
      imports: [Checkout],
      providers: [
        provideHttpClient(), provideHttpClientTesting(), provideRouter([]),
        { provide: ActivatedRoute, useValue: { snapshot: { params, queryParams } } }
      ],
    });
    httpMock = TestBed.inject(HttpTestingController);
    fixture = TestBed.createComponent(Checkout);
    component = fixture.componentInstance;
    fixture.detectChanges();
  }

  afterEach(() => httpMock.verify());

  it('detects the subscription context with the annual query param', () => {
    setup({}, { annual: 'true' });

    expect(component.context).toBe('subscription');
    expect(component.annual).toBe(true);
  });

  it('detects the exam context and loads the exam', () => {
    setup({ examId: '7' });

    const req = httpMock.expectOne('http://localhost:5014/api/exams/7');
    req.flush({ examId: 7, title: 'Java Certification', price: 999, isPremium: true });

    expect(component.context).toBe('exam');
    expect(component.exam?.title).toBe('Java Certification');
  });

  it('canSubmit is false for a too-short card number', () => {
    setup({}, {});
    component.cardNumber = '123';
    expect(component.canSubmit).toBe(false);
  });

  it('canSubmit is true for a valid-length card number', () => {
    setup({}, {});
    component.cardNumber = '4242 4242 4242 4242';
    expect(component.canSubmit).toBe(true);
  });

  it('submits to the subscription upgrade endpoint in subscription context', () => {
    setup({}, {});
    component.cardNumber = '4242424242424242';

    component.handleSubmit();

    const req = httpMock.expectOne('http://localhost:5014/api/subscriptions/upgrade');
    expect(req.request.method).toBe('POST');
    req.flush({ paymentId: 1, status: 'Success' });
  });

  it('submits to the purchase endpoint in exam context', () => {
    setup({ examId: '7' });
    httpMock.expectOne('http://localhost:5014/api/exams/7').flush({ examId: 7, title: 'Java', price: 999 });
    component.cardNumber = '4242424242424242';

    component.handleSubmit();

    const req = httpMock.expectOne('http://localhost:5014/api/purchases');
    expect(req.request.body.examId).toBe(7);
    req.flush({ paymentId: 2, status: 'Success' });
  });

  it('navigates to payment-success on completion (success or failure alike)', () => {
    setup({}, {});
    const routerSpy = vi.spyOn((component as any).router, 'navigate');
    component.cardNumber = '4000000000000002'; // demo decline card

    component.handleSubmit();

    const req = httpMock.expectOne('http://localhost:5014/api/subscriptions/upgrade');
    req.flush({ paymentId: 3, status: 'Failed', failureReason: 'Card declined.' });

    expect(routerSpy).toHaveBeenCalledWith(['/payment-success', 3], expect.anything());
  });

  it('surfaces a friendly error when the charge request itself fails (e.g. blocked)', () => {
    setup({}, {});
    component.cardNumber = '4242424242424242';

    component.handleSubmit();

    httpMock.expectOne('http://localhost:5014/api/subscriptions/upgrade').flush(
      { error: 'You have already created 8 exams on your free trial.' },
      { status: 402, statusText: 'Payment Required' }
    );

    expect(component.errorMessage).toContain('already created 8 exams');
    expect(component.isProcessing).toBe(false);
  });
});
