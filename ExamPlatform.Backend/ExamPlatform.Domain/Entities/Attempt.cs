using ExamPlatform.Domain.Enums;

namespace ExamPlatform.Domain.Entities
{
    // Maps to "attempts". TRANSACTIONAL — one row per attempt a candidate
    // makes on an exam. Multiple rows per (UserId, ExamId) are allowed since
    // we permit unlimited attempts with best score counted.
    public class Attempt
    {
        public int AttemptId { get; set; }                    // PK

        public int UserId { get; set; }                       // FK → User (candidate)
        public int ExamId { get; set; }                       // FK → Exam

        // When the attempt started — set the moment the candidate clicks
        // "Start". The server-side timer is computed from this (business
        // rule 10/11: server time is authoritative).
        public DateTime StartedAt { get; set; }               // UTC, non-nullable

        // Nullable because while attempt is InProgress, it isn't submitted yet.
        // Set on Submit / AutoSubmit.
        public DateTime? SubmittedAt { get; set; }

        public AttemptStatus AttemptStatus { get; set; }

        // Nullable: filled in during evaluation (on submit). Null while
        // InProgress. decimal for exact score arithmetic.
        public decimal? Score { get; set; }

        // Nullable: bool? is "tri-state" — true (passed), false (failed),
        // or null (not yet evaluated). Encodes the difference between
        // "evaluated and failed" vs "not evaluated yet."
        public bool? IsPassed { get; set; }

        // Nullable: filled on submit. Used in analytics ("time taken"
        // displayed on user dashboard).
        public int? TimeTakenSeconds { get; set; }

        // Local URL path to the JSON file containing the topic scores.
        public string? TopicScoresUrl { get; set; }


        // ───────────── NAVIGATION ─────────────
        public User User { get; set; } = null!;
        public Exam Exam { get; set; } = null!;

        // Each attempt has many answer rows (one per question in the exam).
        public ICollection<AttemptAnswer> AttemptAnswers { get; set; } = new List<AttemptAnswer>();

        // 1-to-1 with Certificate (when this attempt earns one). Nullable
        // because most attempts don't earn certificates (the attempt either
        // failed, or it wasn't the best-scoring passing attempt).
        public Certificate? Certificate { get; set; }
    }
}