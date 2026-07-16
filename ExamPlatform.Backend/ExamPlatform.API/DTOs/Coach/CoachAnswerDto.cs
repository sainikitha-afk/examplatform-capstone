namespace ExamPlatform.API.DTOs.Coach
{
    // Response for POST /api/coach/ask and POST /api/coach/help.
    //
    // Structured rather than a bare string on purpose: the UI renders strengths, focus areas
    // and the study plan as cards, and structure is also what lets the backend GROUND the
    // model — a free-form paragraph could smuggle in an invented score, whereas these fields
    // are populated from the backend's own computation.
    //
    // Carries no internal identifiers and no file paths — no attempt ids, no exam ids, no
    // TopicScoresUrl.
    public class CoachAnswerDto
    {
        // The coaching answer itself, plain text (may contain newlines).
        public string Answer { get; set; } = string.Empty;

        // One-line "do this next".
        public string Summary { get; set; } = string.Empty;

        // Backend-computed. Empty when no topic clears the strength threshold — we do not
        // dress up a weak score as a strength.
        public List<CoachTopicDto> StrongTopics { get; set; } = new();

        // Backend-computed topic + score + priority; only the reason is the model's prose.
        public List<CoachFocusAreaDto> FocusAreas { get; set; } = new();

        // Present when the candidate asked for a plan (or one is the natural answer).
        public List<CoachStudyDayDto> StudyPlan { get; set; } = new();

        // Follow-ups the UI offers as one-tap chips.
        public List<string> SuggestedQuestions { get; set; } = new();

        // False when the candidate has no published results yet (the no-data state) or when
        // the answer came from general platform guidance. The UI uses this to decide whether
        // to show the performance cards at all.
        public bool HasPerformanceData { get; set; }

        public DateTime GeneratedAt { get; set; }
    }

    // A topic and the percentage the backend computed for it.
    public class CoachTopicDto
    {
        public string Topic { get; set; } = null!;
        public decimal Score { get; set; }   // percentage, e.g. 85
    }

    // A weak topic worth attention.
    public class CoachFocusAreaDto
    {
        public string Topic { get; set; } = null!;
        public decimal Score { get; set; }              // percentage, backend-computed
        public string Priority { get; set; } = null!;   // High | Medium | Low, from score bands
        public string Reason { get; set; } = null!;
    }

    // One day of the study plan. Day numbers are assigned by the backend, starting at 1.
    public class CoachStudyDayDto
    {
        public int Day { get; set; }
        public string Focus { get; set; } = null!;
        public List<string> Tasks { get; set; } = new();
    }
}
