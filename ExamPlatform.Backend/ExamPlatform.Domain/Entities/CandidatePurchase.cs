using ExamPlatform.Domain.Enums;

namespace ExamPlatform.Domain.Entities
{
    // Maps to "candidate_purchases". Grants a candidate attempt access to one
    // premium (IsPremium=true) exam. AttemptService.StartOrResumeAsync checks
    // for an Active row here before allowing a premium exam to start/resume.
    // UNIQUE (UserId, ExamId) — a candidate can purchase a given exam at most
    // once; a DB-level guard, not just an application check (race-safe).
    public class CandidatePurchase
    {
        public int PurchaseId { get; set; }             // PK

        public int UserId { get; set; }                  // FK -> User (candidate)
        public int ExamId { get; set; }                  // FK -> Exam (must be IsPremium)
        public int PaymentId { get; set; }               // FK -> Payment, unique

        public CandidatePurchaseStatus Status { get; set; }
        public DateTime PurchasedAt { get; set; }

        public User User { get; set; } = null!;
        public Exam Exam { get; set; } = null!;
        public Payment Payment { get; set; } = null!;
    }
}
