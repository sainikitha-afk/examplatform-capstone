using Microsoft.EntityFrameworkCore;
using ExamPlatform.DAL.Contexts;
using ExamPlatform.DAL.Interfaces;
using ExamPlatform.Domain.Entities;

namespace ExamPlatform.DAL.Repositories
{
    public class ExaminerSubscriptionRepository : AbstractRepository<int, ExaminerSubscription>, IExaminerSubscriptionRepository
    {
        public ExaminerSubscriptionRepository(ExamPlatformContext context) : base(context) { }

        public async Task<ExaminerSubscription?> GetByUserIdAsync(int userId)
        {
            return await _dbSet
                .Include(s => s.Plan)
                .FirstOrDefaultAsync(s => s.UserId == userId);
        }
    }
}
