using ExamPlatform.Domain.Entities;
using ExamPlatform.Domain.Enums;

namespace ExamPlatform.DAL.Interfaces
{
    // Contract for SubscriptionPlan persistence — admin-managed, seeded
    // reference data (Free Trial / Professional / Free Forever).
    public interface ISubscriptionPlanRepository : IRepository<int, SubscriptionPlan>
    {
        // Looks up the single active plan row for a tier. Used instead of
        // hardcoding PlanId anywhere in business logic.
        Task<SubscriptionPlan?> GetByTierAsync(SubscriptionTier tier);
    }
}
