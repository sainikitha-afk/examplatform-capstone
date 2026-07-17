using ExamPlatform.Domain.Entities;

namespace ExamPlatform.DAL.Interfaces
{
    public interface ICouponRepository : IRepository<int, Coupon>
    {
        Task<Coupon?> GetByCodeAsync(string code);

        // Atomically increments TimesRedeemed only if the coupon still has
        // room (MaxRedemptions is null, or TimesRedeemed < MaxRedemptions).
        // A single conditional UPDATE — not a load-then-save round trip — so
        // two concurrent redemptions can never both push a capped coupon past
        // its limit. Returns false if the cap had already been reached.
        Task<bool> TryIncrementRedemptionAsync(int couponId);
    }
}
