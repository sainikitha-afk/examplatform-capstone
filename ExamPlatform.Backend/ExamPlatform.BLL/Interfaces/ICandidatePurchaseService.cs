using ExamPlatform.Domain.Entities;
using ExamPlatform.Domain.Enums;

namespace ExamPlatform.BLL.Interfaces
{
    // Owns the candidate-side purchase flow for premium exams (Part 4/5 of
    // the design): checkout -> simulated charge -> access grant -> invoice.
    public interface ICandidatePurchaseService
    {
        // The check AttemptService consults before allowing a premium exam
        // to start/resume.
        Task<bool> HasActivePurchaseAsync(int userId, int examId);

        // Charges the candidate (via the simulated gateway) and, on success,
        // grants access to the premium exam. idempotencyKey must be generated
        // once per checkout attempt by the caller and resent unchanged on
        // any retry of the SAME attempt.
        Task<Payment> PurchaseAsync(
            int userId, int examId, PaymentMethod method, string cardNumber,
            string? couponCode, string idempotencyKey,
            bool simulateNetworkFailure, bool simulateTimeout, DateTime? now = null);

        // A candidate's purchase/billing history, newest first.
        Task<IEnumerable<CandidatePurchase>> GetMyPurchasesAsync(int userId);

        // Simulated refund (admin action) — flips the purchase and its
        // payment to Refunded, revoking attempt access. The audit rows are
        // kept, never deleted (Part 10's refund-simulation requirement).
        Task RefundAsync(int purchaseId, DateTime? now = null);
    }
}
