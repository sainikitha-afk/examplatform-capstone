// RevenueAnalyticsService — Part 7 of the design (Admin Revenue Dashboard).
// Same architectural style as AnalyticsService: pure C# aggregation over
// repository data, no stored procedures, no raw SQL.

using ExamPlatform.BLL.Interfaces;
using ExamPlatform.BLL.Models;
using ExamPlatform.DAL.Interfaces;
using ExamPlatform.Domain.Enums;

namespace ExamPlatform.BLL.Services
{
    public class RevenueAnalyticsService : IRevenueAnalyticsService
    {
        // Payment states that count as "terminal failure" for the failure-rate
        // metric. Pending/Processing/Retrying are in-flight, not failures;
        // Refunded was originally a Success and is excluded from both buckets
        // to avoid double-counting revenue that was later reversed.
        private static readonly HashSet<PaymentStatus> TerminalFailureStatuses = new()
        {
            PaymentStatus.Failed, PaymentStatus.Cancelled, PaymentStatus.Expired,
            PaymentStatus.NetworkFailure, PaymentStatus.Timeout
        };

        private readonly IPaymentRepository _paymentRepository;
        private readonly IExaminerSubscriptionRepository _subscriptionRepository;

        public RevenueAnalyticsService(
            IPaymentRepository paymentRepository,
            IExaminerSubscriptionRepository subscriptionRepository)
        {
            _paymentRepository = paymentRepository;
            _subscriptionRepository = subscriptionRepository;
        }

        public async Task<RevenueSummary> GetSummaryAsync()
        {
            var payments = (await _paymentRepository.GetAllForAnalyticsAsync()).ToList();
            var subscriptions = (await _subscriptionRepository.GetAllAsync()).ToList();

            var successful = payments.Where(p => p.Status == PaymentStatus.Success).ToList();
            var failed = payments.Where(p => TerminalFailureStatuses.Contains(p.Status)).ToList();

            var totalRevenue = successful.Sum(p => p.Amount);
            var subscriptionRevenue = successful
                .Where(p => p.Purpose is PaymentPurpose.SubscriptionPurchase or PaymentPurpose.SubscriptionRenewal)
                .Sum(p => p.Amount);
            var examRevenue = successful
                .Where(p => p.Purpose == PaymentPurpose.ExamCertificationPurchase)
                .Sum(p => p.Amount);

            var decidedCount = successful.Count + failed.Count;
            var failureRate = decidedCount == 0 ? 0d : (double)failed.Count / decidedCount;

            var monthlyRevenue = successful
                .GroupBy(p => (p.CompletedAt ?? p.InitiatedAt).ToString("yyyy-MM"))
                .OrderBy(g => g.Key)
                .Select(g => new MonthlyRevenuePoint(g.Key, g.Sum(p => p.Amount)))
                .ToList();

            var planDistribution = subscriptions
                .GroupBy(s => s.Status)
                .Select(g => new PlanDistributionSlice(g.Key.ToString(), g.Count()))
                .ToList();

            var topExams = successful
                .Where(p => p.Purpose == PaymentPurpose.ExamCertificationPurchase && p.CandidatePurchase?.Exam != null)
                .GroupBy(p => p.CandidatePurchase!.Exam.Title)
                .Select(g => new TopPurchasedExam(g.Key, g.Count(), g.Sum(p => p.Amount)))
                .OrderByDescending(t => t.PurchaseCount)
                .Take(5)
                .ToList();

            var recentTransactions = payments
                .OrderByDescending(p => p.InitiatedAt)
                .Take(10)
                .Select(p => new RecentTransaction(
                    p.InitiatedAt, p.User?.FullName ?? p.User?.Username ?? "Unknown",
                    p.Amount, p.Status.ToString(), p.Purpose.ToString()))
                .ToList();

            var trialCount = subscriptions.Count(s => s.Status == ExaminerSubscriptionStatus.TrialActive);
            var professionalCount = subscriptions.Count(s =>
                s.Status is ExaminerSubscriptionStatus.ProfessionalActive or ExaminerSubscriptionStatus.GracePeriod);
            var freeForeverCount = subscriptions.Count(s => s.Status == ExaminerSubscriptionStatus.FreeForever);

            // Conversion cohort: everyone who has reached the upgrade decision
            // point (currently Professional/GracePeriod, or settled on Free
            // Forever after declining). TrialActive/TrialExhausted examiners
            // haven't decided yet and are excluded from the denominator.
            var decisionCohort = professionalCount + freeForeverCount;
            var conversionRate = decisionCohort == 0 ? 0d : (double)professionalCount / decisionCohort;

            var couponRedemptions = successful.Count(p => p.CouponId.HasValue);

            return new RevenueSummary(
                totalRevenue, subscriptionRevenue, examRevenue,
                successful.Count, failed.Count, failureRate,
                trialCount, professionalCount, freeForeverCount, conversionRate,
                couponRedemptions, monthlyRevenue, planDistribution, topExams, recentTransactions);
        }
    }
}
