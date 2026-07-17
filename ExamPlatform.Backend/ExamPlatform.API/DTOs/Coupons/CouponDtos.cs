using System.ComponentModel.DataAnnotations;

namespace ExamPlatform.API.DTOs.Coupons
{
    public class CreateCouponDto
    {
        [Required]
        [StringLength(30, MinimumLength = 3)]
        public string Code { get; set; } = string.Empty;

        [Range(1, 100)]
        public int DiscountPercent { get; set; }

        public int? MaxRedemptions { get; set; }

        [Required]
        public DateTime ValidFrom { get; set; }

        [Required]
        public DateTime ValidTo { get; set; }

        // Parsed to CouponScope in the controller (string-DTO convention).
        [Required]
        public string AppliesTo { get; set; } = string.Empty;
    }

    public class CouponDto
    {
        public int CouponId { get; set; }
        public string Code { get; set; } = string.Empty;
        public int DiscountPercent { get; set; }
        public int? MaxRedemptions { get; set; }
        public int TimesRedeemed { get; set; }
        public DateTime ValidFrom { get; set; }
        public DateTime ValidTo { get; set; }
        public bool IsActive { get; set; }
        public string AppliesTo { get; set; } = string.Empty;
    }

    public class ValidateCouponRequestDto
    {
        [Required]
        public string Code { get; set; } = string.Empty;

        // Parsed to CouponScope in the controller (string-DTO convention).
        [Required]
        public string Scope { get; set; } = string.Empty;
    }

    public class ValidateCouponResponseDto
    {
        public bool IsValid { get; set; }
        public int DiscountPercent { get; set; }
        public string? RejectionReason { get; set; }
    }
}
