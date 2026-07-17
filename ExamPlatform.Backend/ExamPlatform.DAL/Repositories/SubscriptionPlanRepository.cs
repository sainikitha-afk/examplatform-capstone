using Microsoft.EntityFrameworkCore;
using ExamPlatform.DAL.Contexts;
using ExamPlatform.DAL.Interfaces;
using ExamPlatform.Domain.Entities;
using ExamPlatform.Domain.Enums;

namespace ExamPlatform.DAL.Repositories
{
    public class SubscriptionPlanRepository : AbstractRepository<int, SubscriptionPlan>, ISubscriptionPlanRepository
    {
        public SubscriptionPlanRepository(ExamPlatformContext context) : base(context) { }

        public async Task<SubscriptionPlan?> GetByTierAsync(SubscriptionTier tier)
        {
            return await _dbSet.FirstOrDefaultAsync(p => p.Tier == tier && p.IsActive);
        }
    }
}
