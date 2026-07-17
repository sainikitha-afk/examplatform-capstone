// SubscriptionApiService — HTTP calls for /api/subscriptions (examiner
// subscription dashboard, upgrade checkout, pricing page).

import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import {
  SubscriptionPlanModel, SubscriptionStatusModel, ChargeRequestModel,
  PaymentModel, DeclineUpgradeResponseModel
} from '../models/subscription.model';

@Injectable({ providedIn: 'root' })
export class SubscriptionApiService {
  private baseUrl = 'http://localhost:5014/api/subscriptions';

  constructor(private http: HttpClient) {}

  // GET /api/subscriptions/plans — public pricing page
  loadPlans(): Observable<SubscriptionPlanModel[]> {
    return this.http.get<SubscriptionPlanModel[]>(`${this.baseUrl}/plans`);
  }

  // GET /api/subscriptions/mine — examiner dashboard
  loadMine(): Observable<SubscriptionStatusModel> {
    return this.http.get<SubscriptionStatusModel>(`${this.baseUrl}/mine`);
  }

  // POST /api/subscriptions/decline-upgrade
  declineUpgrade(): Observable<DeclineUpgradeResponseModel> {
    return this.http.post<DeclineUpgradeResponseModel>(`${this.baseUrl}/decline-upgrade`, {});
  }

  // GET /api/subscriptions/billing-history — examiner billing history
  loadBillingHistory(): Observable<PaymentModel[]> {
    return this.http.get<PaymentModel[]>(`${this.baseUrl}/billing-history`);
  }

  // POST /api/subscriptions/upgrade — charge + activate Professional
  upgrade(request: ChargeRequestModel): Observable<PaymentModel> {
    const payload = {
      method: request.method,
      cardNumber: request.cardNumber,
      annual: request.annual,
      couponCode: request.couponCode,
      idempotencyKey: request.idempotencyKey,
      simulateNetworkFailure: request.simulateNetworkFailure,
      simulateTimeout: request.simulateTimeout
    };
    return this.http.post<PaymentModel>(`${this.baseUrl}/upgrade`, payload);
  }
}
