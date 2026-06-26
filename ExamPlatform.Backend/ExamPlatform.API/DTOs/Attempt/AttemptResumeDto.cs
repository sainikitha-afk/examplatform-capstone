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
        // All questions ordered by QuestionOrder, each with the candidate's
        // current answer (if any) and the options without correct-answer flags.
        public List<QuestionInAttemptDto> Questions { get; set; } = new();
    }
}
