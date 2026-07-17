using ExamPlatform.Domain.Enums;

namespace ExamPlatform.Domain.Entities
{
    // Maps to "subscription_plans". Admin-managed reference data, seeded once
    // (Free Trial / Professional / Free Forever) — not created through normal
    // application traffic. ExaminerSubscription.PlanId points at whichever row
    // currently applies to that examiner.
    public class SubscriptionPlan
    {
        public int PlanId { get; set; }               // PK

        public string Name { get; set; } = null!;

        public SubscriptionTier Tier { get; set; }

        // Simulated INR pricing. Both populated even for Trial/FreeForever
        // (0m) so the pricing page can render every plan from one table.
        public decimal PriceMonthly { get; set; }
        public decimal PriceYearly { get; set; }

        // null = unlimited (Professional). 8 for Trial. null for FreeForever
        // (FreeForever's limit is the rolling cooldown below, not a lifetime cap).
        public int? MaxExamsLifetime { get; set; }

        // null = no cooldown (Trial, Professional). 3 for FreeForever.
        public int? ExamCooldownDays { get; set; }

        public bool IsActive { get; set; } = true;

        public ICollection<ExaminerSubscription> Subscriptions { get; set; } = new List<ExaminerSubscription>();
    }
}
