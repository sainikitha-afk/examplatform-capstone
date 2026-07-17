// CouponApiService — HTTP calls for /api/coupons (checkout discount preview
// + admin coupon management).

import { Injectable } from '@angular/core';
import { environment } from '../../environments/environment';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { CouponValidationResultModel } from '../models/subscription.model';

@Injectable({ providedIn: 'root' })
export class CouponApiService {
  private baseUrl = environment.apiBaseUrl + '/api/coupons';

  constructor(private http: HttpClient) {}

  // POST /api/coupons/validate — checkout discount preview (any authenticated role)
  validate(code: string, scope: string): Observable<CouponValidationResultModel> {
    return this.http.post<CouponValidationResultModel>(`${this.baseUrl}/validate`, { code, scope });
  }
}
