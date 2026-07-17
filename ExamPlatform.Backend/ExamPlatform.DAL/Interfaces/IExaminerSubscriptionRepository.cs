using ExamPlatform.Domain.Entities;

namespace ExamPlatform.DAL.Interfaces
{
    // Contract for ExaminerSubscription persistence — one row per examiner.
    public interface IExaminerSubscriptionRepository : IRepository<int, ExaminerSubscription>
    {
        // Core lookup used by every enforcement/state-machine check. Eagerly
        // includes Plan so callers get MaxExamsLifetime/ExamCooldownDays
        // without a second round-trip.
        Task<ExaminerSubscription?> GetByUserIdAsync(int userId);
    }
}
