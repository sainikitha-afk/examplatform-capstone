namespace ExamPlatform.Domain.Entities
{
    // Maps to "coupon_redemptions". Join row recording that a specific user
    // redeemed a specific coupon on a specific payment. UNIQUE (CouponId,
    // UserId) is the DB-level coupon-abuse guard — one redemption per coupon
    // per user, enforced even under concurrent requests.
    public class CouponRedemption
    {
        public int RedemptionId { get; set; }           // PK

        public int CouponId { get; set; }                // FK -> Coupon
        public int UserId { get; set; }                  // FK -> User
        public int PaymentId { get; set; }               // FK -> Payment

        public DateTime RedeemedAt { get; set; }

        public Coupon Coupon { get; set; } = null!;
        public User User { get; set; } = null!;
        public Payment Payment { get; set; } = null!;
    }
}
