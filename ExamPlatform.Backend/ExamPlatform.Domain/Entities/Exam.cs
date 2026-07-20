using ExamPlatform.Domain.Enums;

namespace ExamPlatform.Domain.Entities
{
    // Maps to "exams". TRANSACTIONAL — created when an examiner schedules
    // an exam, and progresses through the ExamStatus lifecycle.
    public class Exam
    {
        public int ExamId { get; set; }                       // PK

        // ───────────── FOREIGN KEYS ─────────────
        public int ExaminerId { get; set; }                   // FK → User (creator)
        public int CatId { get; set; }                        // FK → ExamCategory

        public string Title { get; set; } = null!;

        // Nullable — instructions are optional. Some exams don't need them.
        public string? Instructions { get; set; }

        // int (whole minutes is sufficient resolution for exam duration).
        public int DurationMinutes { get; set; }

        // decimal for exact arithmetic on marks (same reason as Question.Marks).
        public decimal TotalMarks { get; set; }
        public decimal PassingMarks { get; set; }

        // Scheduling window. Both in UTC.
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }

        // Strongly-typed lifecycle state (Draft → Published → ResultsPublished
        // → Archived). Drives business-layer guard clauses everywhere.
        public ExamStatus ExamStatus { get; set; }

        public DateTime CreatedAt { get; set; }               // UTC

        // ───────────── SUBSCRIPTION & PAYMENT (additive) ─────────────
        // Default false/null so every pre-existing exam and every free/practice
        // exam is completely unaffected. Only a Professional-tier (or
        // Institute-verified) examiner may set IsPremium=true — enforced
        // server-side in ExamService, never trusted from the client.
        public bool IsPremium { get; set; }
        public decimal? Price { get; set; }                   // only meaningful when IsPremium
        public bool IsFeatured { get; set; }                   // admin-curated "Featured" flag

        // How many completed attempts each candidate gets (1–10, examiner-set).
        // Only meaningful for non-premium exams: a premium certification exam is
        // ALWAYS exactly one attempt — enforced server-side in ExamService (forces
        // 1 on create/update) and AttemptService (treats premium as 1 regardless
        // of this value). Default 1 preserves the historic one-attempt rule for
        // every pre-existing exam.
        public int MaxAttempts { get; set; } = 1;

        // ───────────── NAVIGATION ─────────────
        // Parent objects this exam belongs to:
        public User Examiner { get; set; } = null!;
        public ExamCategory Category { get; set; } = null!;

        // Children this exam owns:
        public ICollection<ExamQuestion> ExamQuestions { get; set; } = new List<ExamQuestion>();
        public ICollection<Attempt> Attempts { get; set; } = new List<Attempt>();
        public ICollection<Certificate> Certificates { get; set; } = new List<Certificate>();
        public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
        public ICollection<CandidatePurchase> CandidatePurchases { get; set; } = new List<CandidatePurchase>();
    }
}