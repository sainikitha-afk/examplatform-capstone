using ExamPlatform.BLL.Models;
using ExamPlatform.Domain.Entities;
using ExamPlatform.Domain.Enums;

namespace ExamPlatform.BLL.Interfaces
{
    // Owns the examiner-side subscription state machine (Part 2 of the design):
    // TrialActive -> TrialExhausted -> (ProfessionalActive | FreeForever), with
    // ProfessionalActive able to lapse through GracePeriod back to FreeForever.
    public interface IExaminerSubscriptionService
    {
        // The single entry point for "does this examiner have a subscription
        // row" — self-heals for examiners who predate this feature (see
        // ExaminerSubscriptionService for the grandfathering rule) and seeds
        // the 3 reference SubscriptionPlan rows on first use.
        Task<ExaminerSubscription> GetOrCreateAsync(int userId, DateTime? now = null);

        // Read-only eligibility check. ExamService.CreateExamAsync calls this
        // before every insert; never mutates state.
        Task<ExamCreationEligibility> CanCreateExamAsync(int userId, DateTime? now = null);

        // Called by ExamService immediately after a successful exam creation.
        // Increments the relevant counter and flips TrialActive -> TrialExhausted
        // the moment the lifetime cap is reached.
        Task RecordExamCreatedAsync(int userId, DateTime? now = null);

        // Declines the current upgrade/discount offer. Returns the new
        // UpgradeDeclineCount so the caller knows which dialog to show next
        // (1 = show the 25%-off retention offer, 2 = now on Free Forever).
        Task<int> DeclineUpgradeAsync(int userId, DateTime? now = null);

        // Charges the examiner (via the simulated gateway) and, on success,
        // activates Professional. The automatic 25%-off retention offer is
        // applied when UpgradeDeclineCount == 1 at the time of upgrade;
        // otherwise an optional real Coupon code may be supplied instead.
        // idempotencyKey must be generated once per checkout attempt by the
        // caller and resent unchanged on any retry of the SAME attempt.
        Task<Payment> UpgradeAsync(
            int userId, PaymentMethod method, string cardNumber, bool annual,
            string? couponCode, string idempotencyKey,
            bool simulateNetworkFailure, bool simulateTimeout, DateTime? now = null);

        // Periodic sweep: ProfessionalActive past SubscriptionEndDate ->
        // GracePeriod; GracePeriod past its 3-day grace -> FreeForever.
        // Returns the number of subscriptions transitioned.
        Task<int> SweepExpiredSubscriptionsAsync(DateTime? now = null);

        // The 3 seeded plan rows, for the public pricing page. Seeds them on
        // first call if they don't exist yet.
        Task<IEnumerable<SubscriptionPlan>> GetAllPlansAsync();

        // This examiner's billing history (subscription payments + invoices),
        // newest first — Part 6's examiner subscription dashboard.
        Task<IEnumerable<Payment>> GetBillingHistoryAsync(int userId);
    }
}
