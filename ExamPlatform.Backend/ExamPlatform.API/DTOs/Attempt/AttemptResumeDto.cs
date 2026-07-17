namespace ExamPlatform.API.DTOs.Attempt
{
    // Returned by POST /api/attempts/start and GET /api/attempts/{id}.
    // Contains everything the frontend needs to render the exam interface:
    // attempt metadata, the time remaining, and the question list with
    // the candidate's current answers pre-populated.
    public class AttemptResumeDto
    {
        public int AttemptId { get; set; }
        public int ExamId { get; set; }
        public string ExamTitle { get; set; } = null!;
        public DateTime StartedAt { get; set; }
        // Server-computed remaining seconds. 0 means time is up (auto-submitted).
        public int RemainingSeconds { get; set; }
        public string AttemptStatus { get; set; } = null!;
        // Institute badge (live-derived) for the exam-taking screen: set only when
        // this is a premium exam whose examiner holds an Admin-verified institution,
        // so the candidate sees the certifying institute's name + logo while sitting.
        public bool IsInstituteVerified { get; set; }
        public string? InstituteName { get; set; }
        public string? InstituteLogoUrl { get; set; }
        // All questions ordered by QuestionOrder, each with the candidate's
        // current answer (if any) and the options without correct-answer flags.
        public List<QuestionInAttemptDto> Questions { get; set; } = new();
    }
}
