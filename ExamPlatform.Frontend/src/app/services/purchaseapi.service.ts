// PurchaseApiService — HTTP calls for /api/purchases (premium exam checkout
// + candidate purchase/billing history).

import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { ChargeRequestModel, CandidatePurchaseModel, PaymentModel } from '../models/subscription.model';

@Injectable({ providedIn: 'root' })
export class PurchaseApiService {
  private baseUrl = 'http://localhost:5014/api/purchases';

  constructor(private http: HttpClient) {}

  // GET /api/purchases/mine — candidate billing history
  loadMine(): Observable<CandidatePurchaseModel[]> {
    return this.http.get<CandidatePurchaseModel[]>(`${this.baseUrl}/mine`);
  }

  // GET /api/purchases/check/{examId} — has-access check
  checkAccess(examId: number): Observable<{ examId: number; hasAccess: boolean }> {
    return this.http.get<{ examId: number; hasAccess: boolean }>(`${this.baseUrl}/check/${examId}`);
  }

  // POST /api/purchases — candidate checkout for one premium exam
  purchase(request: ChargeRequestModel): Observable<PaymentModel> {
    const payload = {
      examId: request.examId,
      method: request.method,
      cardNumber: request.cardNumber,
      couponCode: request.couponCode,
      idempotencyKey: request.idempotencyKey,
      simulateNetworkFailure: request.simulateNetworkFailure,
      simulateTimeout: request.simulateTimeout
    };
    return this.http.post<PaymentModel>(this.baseUrl, payload);
  }

  // POST /api/purchases/{id}/refund — admin simulated refund
  refund(purchaseId: number): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/${purchaseId}/refund`, {});
  }
}
