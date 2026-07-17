// subscriptionapi.service.spec.ts — unit tests for SubscriptionApiService.
import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { SubscriptionApiService } from './subscriptionapi.service';
import { ChargeRequestModel } from '../models/subscription.model';

describe('SubscriptionApiService', () => {
  let service: SubscriptionApiService;
  let httpMock: HttpTestingController;
  const baseUrl = 'http://localhost:5014/api/subscriptions';

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(SubscriptionApiService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('loadPlans GETs the pricing page data', () => {
    service.loadPlans().subscribe();
    const req = httpMock.expectOne(`${baseUrl}/plans`);
    expect(req.request.method).toBe('GET');
    req.flush([]);
  });

  it('loadMine GETs the examiner dashboard status', () => {
    service.loadMine().subscribe();
    const req = httpMock.expectOne(`${baseUrl}/mine`);
    expect(req.request.method).toBe('GET');
    req.flush({});
  });

  it('declineUpgrade POSTs with an empty body', () => {
    service.declineUpgrade().subscribe();
    const req = httpMock.expectOne(`${baseUrl}/decline-upgrade`);
    expect(req.request.method).toBe('POST');
    req.flush({});
  });

  it('loadBillingHistory GETs the examiner billing history', () => {
    service.loadBillingHistory().subscribe();
    const req = httpMock.expectOne(`${baseUrl}/billing-history`);
    expect(req.request.method).toBe('GET');
    req.flush([]);
  });

  it('upgrade POSTs the charge payload without a leaked idempotency key mismatch', () => {
    const request = new ChargeRequestModel('CreditCard', '4242424242424242', 'abc-123', true, 0, 'SAVE25');
    service.upgrade(request).subscribe();

    const req = httpMock.expectOne(`${baseUrl}/upgrade`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({
      method: 'CreditCard',
      cardNumber: '4242424242424242',
      annual: true,
      couponCode: 'SAVE25',
      idempotencyKey: 'abc-123',
      simulateNetworkFailure: false,
      simulateTimeout: false
    });
    req.flush({});
  });
});
