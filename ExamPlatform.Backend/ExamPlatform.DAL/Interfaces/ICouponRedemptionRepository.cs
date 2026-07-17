using ExamPlatform.Domain.Entities;

namespace ExamPlatform.DAL.Interfaces
{
    public interface ICouponRedemptionRepository : IRepository<int, CouponRedemption>
    {
        // Coupon-abuse pre-check (the DB unique index on (CouponId, UserId)
        // is the actual race-safe guard).
        Task<bool> ExistsForCouponAndUserAsync(int couponId, int userId);
    }
}
