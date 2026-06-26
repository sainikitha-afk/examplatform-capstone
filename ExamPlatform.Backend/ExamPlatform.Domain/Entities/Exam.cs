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

        // ───────────── NAVIGATION ─────────────
        // Parent objects this exam belongs to:
        public User Examiner { get; set; } = null!;
        public ExamCategory Category { get; set; } = null!;

        // Children this exam owns:
        public ICollection<ExamQuestion> ExamQuestions { get; set; } = new List<ExamQuestion>();
        public ICollection<Attempt> Attempts { get; set; } = new List<Attempt>();
        public ICollection<Certificate> Certificates { get; set; } = new List<Certificate>();
        public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
    }
}