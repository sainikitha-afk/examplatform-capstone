using System.ComponentModel.DataAnnotations;

namespace ExamPlatform.API.DTOs.Subscriptions
{
    // Response for GET /api/subscriptions/mine and the outcome of every
    // state-changing action below — the examiner subscription dashboard's
    // single source of truth.
    public class SubscriptionStatusDto
    {
        public string Status { get; set; } = string.Empty;
        public string PlanName { get; set; } = string.Empty;
        public int TrialExamsUsed { get; set; }
        public int? MaxExamsLifetime { get; set; }
        public int UpgradeDeclineCount { get; set; }
        public DateTime? NextEligibleAt { get; set; }
        public DateTime? SubscriptionEndDate { get; set; }
        public DateTime? NextRenewalDate { get; set; }
        public bool AutoRenew { get; set; }
        public int? DiscountAppliedPercent { get; set; }
        public bool CanCreatePremiumExams { get; set; }

        // Which dialog the frontend should show right now, if any:
        // "none" | "upgrade" (first decline not yet made) | "discount" (25%-off,
        // shown after the first decline) | "free-forever" (already settled).
        public string UpgradeDialog { get; set; } = "none";
    }

    public class SubscriptionPlanDto
    {
        public int PlanId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Tier { get; set; } = string.Empty;
        public decimal PriceMonthly { get; set; }
        public decimal PriceYearly { get; set; }
        public int? MaxExamsLifetime { get; set; }
        public int? ExamCooldownDays { get; set; }
    }

    // Request body for POST /api/subscriptions/upgrade. Amount is never
    // accepted from the client — it is always computed server-side from the
    // seeded Professional plan price.
    public class UpgradeSubscriptionDto
    {
        // Parsed to PaymentMethod in the controller (same string-DTO,
        // parse-server-side convention as every other enum field in this API —
        // see IssueReportsController.UpdateStatus).
        [Required]
        public string Method { get; set; } = string.Empty;

        [Required]
        [StringLength(19, MinimumLength = 12, ErrorMessage = "Enter a valid card number.")]
        public string CardNumber { get; set; } = string.Empty;

        public bool Annual { get; set; }

        [StringLength(30)]
        public string? CouponCode { get; set; }

        // Generated once per checkout attempt by the frontend and resent
        // unchanged on any retry of the SAME attempt — the double-charge guard.
        [Required]
        public string IdempotencyKey { get; set; } = string.Empty;

        public bool SimulateNetworkFailure { get; set; }
        public bool SimulateTimeout { get; set; }
    }

    public class DeclineUpgradeResponseDto
    {
        public int UpgradeDeclineCount { get; set; }

        // "discount" if the frontend should now show the 25%-off offer,
        // "free-forever" if the examiner has just moved to the fallback plan.
        public string NextStep { get; set; } = string.Empty;
    }
}
