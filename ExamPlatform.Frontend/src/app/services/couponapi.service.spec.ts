// couponapi.service.spec.ts — unit tests for CouponApiService.
import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { CouponApiService } from './couponapi.service';

describe('CouponApiService', () => {
  let service: CouponApiService;
  let httpMock: HttpTestingController;
  const baseUrl = 'http://localhost:5014/api/coupons';

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(CouponApiService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('validate POSTs the code and scope', () => {
    service.validate('SAVE25', 'ExamPurchaseOnly').subscribe();

    const req = httpMock.expectOne(`${baseUrl}/validate`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ code: 'SAVE25', scope: 'ExamPurchaseOnly' });
    req.flush({});
  });
});
