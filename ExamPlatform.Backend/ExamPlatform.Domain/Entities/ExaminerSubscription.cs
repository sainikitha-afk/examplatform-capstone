using ExamPlatform.Domain.Enums;

namespace ExamPlatform.Domain.Entities
{
    // Maps to "examiner_subscriptions". One row per examiner (UserId is unique),
    // created automatically the moment an examiner is approved (TrialActive,
    // PlanId -> the seeded Trial plan). Mutates through the state machine
    // documented in ExaminerSubscriptionService: TrialActive -> TrialExhausted
    // -> (ProfessionalActive | FreeForever), with ProfessionalActive able to
    // lapse through GracePeriod back to FreeForever on a missed renewal.
    public class ExaminerSubscription
    {
        public int SubscriptionId { get; set; }        // PK

        public int UserId { get; set; }                 // FK -> User (examiner), unique
        public int PlanId { get; set; }                 // FK -> SubscriptionPlan (current plan)

        public ExaminerSubscriptionStatus Status { get; set; }

        // Lifetime counter — never resets, even after upgrading, so a
        // downgrade-then-re-upgrade can never re-trigger the trial.
        public int TrialExamsUsed { get; set; }

        // 0 = never declined, 1 = declined the full-price offer (25%-off shown
        // next), 2 = declined the discount too (now on FreeForever). Drives
        // which dialog CanCreateExam's deny response asks the frontend to show.
        public int UpgradeDeclineCount { get; set; }

        public DateTime SubscriptionStartDate { get; set; }
        public DateTime? SubscriptionEndDate { get; set; }
        public DateTime? NextRenewalDate { get; set; }
        public bool AutoRenew { get; set; }

        // Set once, when the 25%-off retention offer is accepted. Null otherwise.
        public int? DiscountAppliedPercent { get; set; }

        // Drives the FreeForever "1 exam / 3 days" cooldown. Null until the
        // first FreeForever exam is created.
        public DateTime? LastFreeExamCreatedAt { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public User User { get; set; } = null!;
        public SubscriptionPlan Plan { get; set; } = null!;
        public ICollection<Payment> Payments { get; set; } = new List<Payment>();
    }
}
