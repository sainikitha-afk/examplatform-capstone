using ExamPlatform.Domain.Enums;

namespace ExamPlatform.Domain.Entities
{
    // Maps to "coupons". Admin-created discount codes; also the mechanism
    // behind the automatic "25% off" retention offer in the trial-decline flow
    // (a reserved system coupon, not something a candidate/examiner types in).
    public class Coupon
    {
        public int CouponId { get; set; }               // PK

        public string Code { get; set; } = null!;        // unique

        public int DiscountPercent { get; set; }

        // null = unlimited redemptions overall; still bounded to one-per-user
        // via CouponRedemption's unique (CouponId, UserId) index.
        public int? MaxRedemptions { get; set; }
        public int TimesRedeemed { get; set; }

        public DateTime ValidFrom { get; set; }
        public DateTime ValidTo { get; set; }
        public bool IsActive { get; set; } = true;

        public CouponScope AppliesTo { get; set; }

        public ICollection<Payment> Payments { get; set; } = new List<Payment>();
        public ICollection<CouponRedemption> Redemptions { get; set; } = new List<CouponRedemption>();
    }
}
