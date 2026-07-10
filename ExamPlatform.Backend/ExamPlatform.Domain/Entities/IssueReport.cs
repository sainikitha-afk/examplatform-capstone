using ExamPlatform.Domain.Enums;

namespace ExamPlatform.Domain.Entities
{
    // Maps to "issue_reports". Raised by a candidate DURING (or just after) an
    // attempt to flag a problem — either with a specific question ("this question
    // has two correct answers") or with the exam experience in general ("the timer
    // jumped", "I got logged out"). Each report immediately notifies the exam's
    // examiner via the in-app notification inbox so nothing is silently lost.
    //
    // This is deliberately lightweight: it is a support/audit trail, NOT part of
    // scoring. It never blocks or changes an attempt.
    public class IssueReport
    {
        public int IssueReportId { get; set; }                // PK

        public int CandidateId { get; set; }                  // FK → User (who reported)
        public int ExamId { get; set; }                       // FK → Exam (which exam)

        // Optional — the attempt the candidate was taking when they reported.
        // Null if raised outside an active attempt (rare, but allowed).
        public int? AttemptId { get; set; }

        // Optional — the specific question being flagged. Null = a general
        // experience/technical issue not tied to one question.
        public int? QuestionId { get; set; }

        // The candidate's free-text description of the problem. Required, capped.
        public string Message { get; set; } = null!;

        public IssueStatus Status { get; set; } = IssueStatus.Open;

        public DateTime CreatedAt { get; set; }               // UTC

        // Set when an examiner marks the report reviewed/resolved.
        public DateTime? ResolvedAt { get; set; }

        // Optional examiner note added when resolving (e.g. "confirmed typo, fixed").
        public string? ResolutionNote { get; set; }

        // ───────────── NAVIGATION (no inverse collections — keeps other
        //   entities untouched; EF uses these FKs for referential integrity) ──
        public User Candidate { get; set; } = null!;
        public Exam Exam { get; set; } = null!;
        public Question? Question { get; set; }
    }
}
