using ExamPlatform.BLL.Models;
using ExamPlatform.Domain.Enums;

namespace ExamPlatform.BLL.Interfaces
{
    public interface ICouponService
    {
        // Non-throwing check — every rejection reason from Part 9/10's coupon
        // validation matrix (not found, inactive, expired, scope mismatch,
        // redemption limit reached, already redeemed by this user) is
        // returned, not thrown, so callers decide how to surface it.
        Task<CouponValidationResult> ValidateAsync(string code, int userId, CouponScope requiredScope, DateTime now);

        // Records a redemption and increments the coupon's counter. Callers
        // must have just validated the coupon; the DB unique index on
        // (CouponId, UserId) is still the actual race-safe guard.
        Task RedeemAsync(int couponId, int userId, int paymentId, DateTime now);
    }
}
