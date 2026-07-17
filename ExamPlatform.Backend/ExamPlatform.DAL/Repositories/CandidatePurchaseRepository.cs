using Microsoft.EntityFrameworkCore;
using ExamPlatform.DAL.Contexts;
using ExamPlatform.DAL.Interfaces;
using ExamPlatform.Domain.Entities;

namespace ExamPlatform.DAL.Repositories
{
    public class CandidatePurchaseRepository : AbstractRepository<int, CandidatePurchase>, ICandidatePurchaseRepository
    {
        public CandidatePurchaseRepository(ExamPlatformContext context) : base(context) { }

        public async Task<CandidatePurchase?> GetByUserAndExamAsync(int userId, int examId)
        {
            return await _dbSet.FirstOrDefaultAsync(cp => cp.UserId == userId && cp.ExamId == examId);
        }

        public async Task<IEnumerable<CandidatePurchase>> GetByUserIdAsync(int userId)
        {
            return await _dbSet
                .Include(cp => cp.Exam)
                .Include(cp => cp.Payment)
                    .ThenInclude(p => p.Invoice)
                .Where(cp => cp.UserId == userId)
                .OrderByDescending(cp => cp.PurchasedAt)
                .ToListAsync();
        }
    }
}
