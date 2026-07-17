// SubscriptionModel — frontend twins of the Subscription & Payment System DTOs.
// Why it exists: pricing, checkout, examiner-subscription, candidate-purchases,
//   revenue dashboard, and institution-verification screens all need typed
//   objects for these API responses.

export class SubscriptionPlanModel {
  constructor(
    public planId: number = 0,
    public name: string = "",
    public tier: string = "",             // "Trial" | "Professional" | "FreeForever"
    public priceMonthly: number = 0,
    public priceYearly: number = 0,
    public maxExamsLifetime: number | null = null,
    public examCooldownDays: number | null = null
  ) {}
}

export class SubscriptionStatusModel {
  constructor(
    public status: string = "",           // ExaminerSubscriptionStatus
    public planName: string = "",
    public trialExamsUsed: number = 0,
    public maxExamsLifetime: number | null = null,
    public upgradeDeclineCount: number = 0,
    public nextEligibleAt: string | null = null,
    public subscriptionEndDate: string | null = null,
    public nextRenewalDate: string | null = null,
    public autoRenew: boolean = false,
    public discountAppliedPercent: number | null = null,
    public canCreatePremiumExams: boolean = false,
    public upgradeDialog: string = "none" // "none" | "upgrade" | "discount" | "free-forever"
  ) {}
}

// Request body for POST /api/subscriptions/upgrade and POST /api/purchases.
export class ChargeRequestModel {
  constructor(
    public method: string = "CreditCard",
    public cardNumber: string = "",
    public idempotencyKey: string = "",
    public annual: boolean = false,       // subscription upgrade only
    public examId: number = 0,            // exam purchase only
    public couponCode: string | null = null,
    public simulateNetworkFailure: boolean = false,
    public simulateTimeout: boolean = false
  ) {}
}

export class InvoiceModel {
  constructor(
    public invoiceNumber: string = "",
    public fakeGstNumber: string = "",
    public billingName: string = "",
    public billingEmail: string = "",
    public subtotal: number = 0,
    public discountAmount: number = 0,
    public taxAmount: number = 0,
    public total: number = 0,
    public issuedAt: string = ""
  ) {}
}

export class PaymentModel {
  constructor(
    public paymentId: number = 0,
    public amount: number = 0,
    public currency: string = "INR",
    public method: string = "",
    public status: string = "",           // PaymentStatus
    public fakeTransactionId: string = "",
    public fakeGatewayReference: string = "",
    public failureReason: string | null = null,
    public initiatedAt: string = "",
    public completedAt: string | null = null,
    public invoice: InvoiceModel | null = null
  ) {}
}

export class DeclineUpgradeResponseModel {
  constructor(
    public upgradeDeclineCount: number = 0,
    public nextStep: string = ""          // "discount" | "free-forever"
  ) {}
}

export class CandidatePurchaseModel {
  constructor(
    public purchaseId: number = 0,
    public examId: number = 0,
    public examTitle: string = "",
    public status: string = "",
    public purchasedAt: string = "",
    public amount: number = 0,
    public invoiceNumber: string | null = null
  ) {}
}

export class CouponValidationResultModel {
  constructor(
    public isValid: boolean = false,
    public discountPercent: number = 0,
    public rejectionReason: string | null = null
  ) {}
}

export class InstitutionModel {
  constructor(
    public institutionId: number = 0,
    public name: string = "",
    public logoUrl: string | null = null,
    public verificationStatus: string = "",
    public requestedAt: string = "",
    public verifiedAt: string | null = null,
    public ownerUserId: number | null = null,
    public ownerName: string | null = null
  ) {}
}

// Demo card numbers the fake gateway recognizes (mirrors PaymentGatewayService's
// DemoCards table) — shown on the checkout screen so anyone can drive every
// outcome deliberately instead of relying only on the weighted-random path.
export const DEMO_CARDS = [
  { number: "4242 4242 4242 4242", outcome: "Success" },
  { number: "4000 0000 0000 0002", outcome: "Declined" },
  { number: "4000 0000 0000 9995", outcome: "Insufficient funds" },
  { number: "4000 0000 0000 0069", outcome: "Expired card" },
  { number: "4000 0000 0000 0119", outcome: "Processing error" },
];

// ───────────── ADMIN REVENUE DASHBOARD ─────────────

export class MonthlyRevenuePointModel {
  constructor(public month: string = "", public revenue: number = 0) {}
}
export class PlanDistributionSliceModel {
  constructor(public status: string = "", public count: number = 0) {}
}
export class TopPurchasedExamModel {
  constructor(
    public examTitle: string = "",
    public purchaseCount: number = 0,
    public revenue: number = 0
  ) {}
}
export class RecentTransactionModel {
  constructor(
    public date: string = "",
    public payerName: string = "",
    public amount: number = 0,
    public status: string = "",
    public purpose: string = ""
  ) {}
}

export class RevenueSummaryModel {
  constructor(
    public totalRevenue: number = 0,
    public subscriptionRevenue: number = 0,
    public examCertificationRevenue: number = 0,
    public totalSuccessfulPayments: number = 0,
    public totalFailedPayments: number = 0,
    public paymentFailureRate: number = 0,
    public activeTrialCount: number = 0,
    public activeProfessionalCount: number = 0,
    public activeFreeForeverCount: number = 0,
    public trialToPaidConversionRate: number = 0,
    public couponRedemptions: number = 0,
    public monthlyRevenue: MonthlyRevenuePointModel[] = [],
    public planDistribution: PlanDistributionSliceModel[] = [],
    public topPurchasedExams: TopPurchasedExamModel[] = [],
    public recentTransactions: RecentTransactionModel[] = []
  ) {}
}
