using Microsoft.EntityFrameworkCore;
using ExamPlatform.DAL.Contexts;
using ExamPlatform.DAL.Interfaces;
using ExamPlatform.Domain.Entities;

namespace ExamPlatform.DAL.Repositories
{
    public class CouponRedemptionRepository : AbstractRepository<int, CouponRedemption>, ICouponRedemptionRepository
    {
        public CouponRedemptionRepository(ExamPlatformContext context) : base(context) { }

        public async Task<bool> ExistsForCouponAndUserAsync(int couponId, int userId)
        {
            return await _dbSet.AnyAsync(r => r.CouponId == couponId && r.UserId == userId);
        }
    }
}
