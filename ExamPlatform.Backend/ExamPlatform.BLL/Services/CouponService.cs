using Microsoft.Extensions.Logging;
using ExamPlatform.BLL.Interfaces;
using ExamPlatform.BLL.Models;
using ExamPlatform.DAL.Interfaces;
using ExamPlatform.Domain.Entities;
using ExamPlatform.Domain.Enums;

namespace ExamPlatform.BLL.Services
{
    // Admin-created discount codes. Every rejection path here is one row of
    // Part 10's coupon validation matrix (not found / inactive / expired /
    // wrong scope / redemption limit / already redeemed by this user).
    //
    // NOTE: the automatic 25%-off retention offer in the examiner trial-decline
    // flow (Part 2) is NOT modeled as a Coupon — it is a one-time field on
    // ExaminerSubscription (DiscountAppliedPercent), since it is never typed
    // in by a user and is already guarded by UpgradeDeclineCount. Keeping the
    // two discount mechanisms separate avoids conflating "codes anyone can
    // redeem once" with "one automatic system offer per examiner."
    public class CouponService : ICouponService
    {
        private readonly ICouponRepository _couponRepository;
        private readonly ICouponRedemptionRepository _redemptionRepository;
        private readonly ILogger<CouponService> _logger;

        public CouponService(
            ICouponRepository couponRepository,
            ICouponRedemptionRepository redemptionRepository,
            ILogger<CouponService> logger)
        {
            _couponRepository = couponRepository;
            _redemptionRepository = redemptionRepository;
            _logger = logger;
        }

        public async Task<CouponValidationResult> ValidateAsync(
            string code, int userId, CouponScope requiredScope, DateTime now)
        {
            var normalized = (code ?? string.Empty).Trim().ToUpperInvariant();
            var coupon = await _couponRepository.GetByCodeAsync(normalized);

            if (coupon == null)
                return new CouponValidationResult(false, null, 0, "Coupon code not found.");

            if (!coupon.IsActive)
                return new CouponValidationResult(false, null, 0, "This coupon is no longer active.");

            if (now < coupon.ValidFrom || now > coupon.ValidTo)
                return new CouponValidationResult(false, null, 0, "This coupon has expired.");

            if (coupon.AppliesTo != CouponScope.Both && coupon.AppliesTo != requiredScope)
                return new CouponValidationResult(false, null, 0, "This coupon cannot be used for this type of purchase.");

            if (coupon.MaxRedemptions.HasValue && coupon.TimesRedeemed >= coupon.MaxRedemptions.Value)
                return new CouponValidationResult(false, null, 0, "This coupon has reached its redemption limit.");

            if (await _redemptionRepository.ExistsForCouponAndUserAsync(coupon.CouponId, userId))
                return new CouponValidationResult(false, null, 0, "You have already used this coupon.");

            return new CouponValidationResult(true, coupon.CouponId, coupon.DiscountPercent, null);
        }

        public async Task RedeemAsync(int couponId, int userId, int paymentId, DateTime now)
        {
            // Called AFTER the payment has already been charged (Part 9's
            // ordering) — so this must never throw. A capped coupon that lost
            // a redemption race is a bookkeeping detail, not a reason to strand
            // a candidate who already paid without their purchase/subscription
            // row. TryIncrementRedemptionAsync is one atomic UPDATE ... WHERE
            // statement, so it — unlike a load/increment/save round trip —
            // cannot let two concurrent redemptions both push TimesRedeemed
            // past MaxRedemptions.
            var incremented = await _couponRepository.TryIncrementRedemptionAsync(couponId);
            if (!incremented)
            {
                _logger.LogWarning(
                    "Coupon {CouponId} redemption count NOT incremented for payment {PaymentId} — " +
                    "its redemption limit was already reached by a concurrent request. The payment " +
                    "itself is unaffected; only the coupon's usage count is capped.",
                    couponId, paymentId);
            }

            // Still recorded even if the aggregate cap was already hit — this
            // is what stops THIS user from redeeming the same code twice.
            await _redemptionRepository.AddAsync(new CouponRedemption
            {
                CouponId = couponId,
                UserId = userId,
                PaymentId = paymentId,
                RedeemedAt = now
            });
            await _redemptionRepository.SaveChangesAsync();
        }
    }
}
