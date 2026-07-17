using ExamPlatform.Domain.Entities;

namespace ExamPlatform.DAL.Interfaces
{
    // Contract for Payment persistence — the audit trail behind billing
    // history and the admin Revenue Dashboard.
    public interface IPaymentRepository : IRepository<int, Payment>
    {
        // The double-payment / replay guard: a retried checkout with the same
        // key must find its original outcome instead of charging again.
        Task<Payment?> GetByIdempotencyKeyAsync(string idempotencyKey);

        // Billing history for one user (examiner or candidate), newest first.
        Task<IEnumerable<Payment>> GetByUserIdAsync(int userId);

        // Full audit trail for RevenueAnalyticsService's aggregation — small
        // enough table (demo/simulated volume) that a single load is fine,
        // matching AnalyticsService's existing pure-C#-aggregation style.
        Task<IEnumerable<Payment>> GetAllForAnalyticsAsync();
    }
}
