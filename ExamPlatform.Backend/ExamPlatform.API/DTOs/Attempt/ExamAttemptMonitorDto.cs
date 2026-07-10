namespace ExamPlatform.API.DTOs.Attempt
{
    // ExamAttemptMonitorDto — one row in the examiner's attempt-monitoring view.
    // Shows who took the exam, live progress for in-progress attempts, and how
    // each attempt ended (submitted / auto-submitted on timeout / still running).
    public class ExamAttemptMonitorDto
    {
        public int AttemptId { get; set; }
        public int UserId { get; set; }
        public string CandidateName { get; set; } = string.Empty;
        public string CandidateEmail { get; set; } = string.Empty;
        public DateTime StartedAt { get; set; }
        public DateTime? SubmittedAt { get; set; }

        // "InProgress" | "Submitted" | "AutoSubmitted"
        // AutoSubmitted = the server timer closed the attempt — usually means the
        // candidate disconnected and never came back before time expired.
        public string AttemptStatus { get; set; } = string.Empty;

        public decimal? Score { get; set; }
        public bool? IsPassed { get; set; }
        public int? TimeTakenSeconds { get; set; }

        // How many questions have a saved answer — lets the examiner see live
        // progress on in-progress attempts (and partial work on abandoned ones).
        public int AnsweredCount { get; set; }

        public string? TopicScoresUrl { get; set; }
    }
}
