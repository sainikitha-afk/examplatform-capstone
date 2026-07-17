using Microsoft.EntityFrameworkCore;
using ExamPlatform.DAL.Contexts;
using ExamPlatform.DAL.Interfaces;
using ExamPlatform.Domain.Entities;

namespace ExamPlatform.DAL.Repositories
{
    public class CouponRepository : AbstractRepository<int, Coupon>, ICouponRepository
    {
        public CouponRepository(ExamPlatformContext context) : base(context) { }

        public async Task<Coupon?> GetByCodeAsync(string code)
        {
            return await _dbSet.FirstOrDefaultAsync(c => c.Code == code);
        }

        public async Task<bool> TryIncrementRedemptionAsync(int couponId)
        {
            // ExecuteUpdateAsync issues one UPDATE ... WHERE ... statement —
            // the cap check and the increment happen atomically in the DB, so
            // two concurrent redemptions of a MaxRedemptions=1 coupon cannot
            // both succeed (the second's WHERE no longer matches once the
            // first commits its increment).
            var rowsAffected = await _dbSet
                .Where(c => c.CouponId == couponId &&
                            (c.MaxRedemptions == null || c.TimesRedeemed < c.MaxRedemptions.Value))
                .ExecuteUpdateAsync(setters =>
                    setters.SetProperty(c => c.TimesRedeemed, c => c.TimesRedeemed + 1));

            return rowsAffected == 1;
        }
    }
}
