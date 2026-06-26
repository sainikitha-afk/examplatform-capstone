namespace ExamPlatform.API.DTOs.Attempt
{
    // Lightweight attempt response used in the history list (GET /api/attempts/mine).
    // No per-question breakdown — that is in AttemptResultDto.
    public class AttemptSummaryDto
    {
        public int AttemptId { get; set; }
        public int ExamId { get; set; }
        public string? ExamTitle { get; set; }     // from Include(Exam) in repo
        public DateTime StartedAt { get; set; }
        public DateTime? SubmittedAt { get; set; }
        public string AttemptStatus { get; set; } = null!;
        public decimal? Score { get; set; }        // null until evaluated
        public bool? IsPassed { get; set; }        // null until evaluated
    }
}
