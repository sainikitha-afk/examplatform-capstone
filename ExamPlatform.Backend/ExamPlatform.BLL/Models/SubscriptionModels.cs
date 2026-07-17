// Value objects for the Subscription & Payment System. Same role as
// CoachModels.cs/AnalyticsModels.cs: BLL-layer records the API maps to
// response DTOs. Every number here is backend-computed — nothing is ever
// asked of, or trusted from, the client or a third party (there is no
// third party; this whole system is a simulation).

using ExamPlatform.Domain.Enums;

namespace ExamPlatform.BLL.Models
{
    // Result of ExaminerSubscriptionService.CanCreateExamAsync — the single
    // gate ExamService.CreateExamAsync consults before every insert.
    public record ExamCreationEligibility(
        bool Allowed,
        ExaminerSubscriptionStatus Status,
        int TrialExamsUsed,
        int? MaxExamsLifetime,
        int UpgradeDeclineCount,
        DateTime? NextEligibleAt,
        bool CanCreatePremiumExams);

    // Input to the simulated gateway. CardNumber selects a deterministic
    // demo outcome (see PaymentGatewayService); everything else is
    // server-computed by the caller before this is built — never trusted
    // from the client.
    public record PaymentChargeRequest(
        decimal Amount,
        PaymentMethod Method,
        string CardNumber,
        bool SimulateNetworkFailure,
        bool SimulateTimeout);

    // Outcome of one simulated charge attempt.
    public record PaymentChargeOutcome(
        PaymentStatus Status,
        string? FailureReason);

    // Everything InvoiceService needs to generate one Invoice row for a
    // successful Payment.
    public record InvoiceRequest(
        int PaymentId,
        decimal Subtotal,
        decimal DiscountAmount,
        string BillingName,
        string BillingEmail,
        DateTime Now);

    // Outcome of CouponService.ValidateAsync — either usable (with the
    // discount percent) or not, with a specific reason either way.
    public record CouponValidationResult(
        bool IsValid,
        int? CouponId,
        int DiscountPercent,
        string? RejectionReason);

    // ───────────── ADMIN REVENUE DASHBOARD (Part 7) ─────────────
    // All pure aggregation over Payment/ExaminerSubscription rows — same
    // architectural style as AnalyticsService, no stored procs, no raw SQL.

    public record MonthlyRevenuePoint(string Month, decimal Revenue);       // Month e.g. "2026-07"
    public record PlanDistributionSlice(string Status, int Count);         // Status = ExaminerSubscriptionStatus name
    public record TopPurchasedExam(string ExamTitle, int PurchaseCount, decimal Revenue);
    public record RecentTransaction(DateTime Date, string PayerName, decimal Amount, string Status, string Purpose);

    public record RevenueSummary(
        decimal TotalRevenue,
        decimal SubscriptionRevenue,
        decimal ExamCertificationRevenue,
        int TotalSuccessfulPayments,
        int TotalFailedPayments,
        double PaymentFailureRate,
        int ActiveTrialCount,
        int ActiveProfessionalCount,
        int ActiveFreeForeverCount,
        double TrialToPaidConversionRate,
        int CouponRedemptions,
        IReadOnlyList<MonthlyRevenuePoint> MonthlyRevenue,
        IReadOnlyList<PlanDistributionSlice> PlanDistribution,
        IReadOnlyList<TopPurchasedExam> TopPurchasedExams,
        IReadOnlyList<RecentTransaction> RecentTransactions);
}
