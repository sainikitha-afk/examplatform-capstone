// purchaseapi.service.spec.ts — unit tests for PurchaseApiService.
import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { PurchaseApiService } from './purchaseapi.service';
import { ChargeRequestModel } from '../models/subscription.model';

describe('PurchaseApiService', () => {
  let service: PurchaseApiService;
  let httpMock: HttpTestingController;
  const baseUrl = 'http://localhost:5014/api/purchases';

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(PurchaseApiService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('loadMine GETs the candidate billing history', () => {
    service.loadMine().subscribe();
    const req = httpMock.expectOne(`${baseUrl}/mine`);
    expect(req.request.method).toBe('GET');
    req.flush([]);
  });

  it('checkAccess GETs the has-access flag for one exam', () => {
    service.checkAccess(7).subscribe();
    const req = httpMock.expectOne(`${baseUrl}/check/7`);
    expect(req.request.method).toBe('GET');
    req.flush({ examId: 7, hasAccess: false });
  });

  it('purchase POSTs the exam id and charge details', () => {
    const request = new ChargeRequestModel('UPI', '4242424242424242', 'key-1', false, 7, null);
    service.purchase(request).subscribe();

    const req = httpMock.expectOne(baseUrl);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({
      examId: 7,
      method: 'UPI',
      cardNumber: '4242424242424242',
      couponCode: null,
      idempotencyKey: 'key-1',
      simulateNetworkFailure: false,
      simulateTimeout: false
    });
    req.flush({});
  });

  it('refund POSTs to /{id}/refund', () => {
    service.refund(3).subscribe();
    const req = httpMock.expectOne(`${baseUrl}/3/refund`);
    expect(req.request.method).toBe('POST');
    req.flush({});
  });
});
