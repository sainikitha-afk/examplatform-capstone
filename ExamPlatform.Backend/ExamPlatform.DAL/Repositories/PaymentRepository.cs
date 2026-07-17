using Microsoft.EntityFrameworkCore;
using ExamPlatform.DAL.Contexts;
using ExamPlatform.DAL.Interfaces;
using ExamPlatform.Domain.Entities;

namespace ExamPlatform.DAL.Repositories
{
    public class PaymentRepository : AbstractRepository<int, Payment>, IPaymentRepository
    {
        public PaymentRepository(ExamPlatformContext context) : base(context) { }

        public async Task<Payment?> GetByIdempotencyKeyAsync(string idempotencyKey)
        {
            return await _dbSet.FirstOrDefaultAsync(p => p.IdempotencyKey == idempotencyKey);
        }

        public async Task<IEnumerable<Payment>> GetByUserIdAsync(int userId)
        {
            return await _dbSet
                .Include(p => p.Invoice)
                .Where(p => p.UserId == userId)
                .OrderByDescending(p => p.InitiatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Payment>> GetAllForAnalyticsAsync()
        {
            return await _dbSet
                .Include(p => p.User)
                .Include(p => p.CandidatePurchase)
                    .ThenInclude(cp => cp!.Exam)
                .Include(p => p.Subscription)
                .AsNoTracking()
                .ToListAsync();
        }
    }
}
