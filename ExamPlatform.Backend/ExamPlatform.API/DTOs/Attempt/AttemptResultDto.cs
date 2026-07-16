namespace ExamPlatform.API.DTOs.Attempt
{
    // Result view returned by GET /api/attempts/{id}/result.
    //
    // Score/pass-fail are available immediately after submission (scoring is automatic).
    // The per-question breakdown — which exposes correct answers — plus the topic chart
    // stay empty until the examiner publishes results; ResultsPublished tells the UI
    // which of the two states it is rendering.
    public class AttemptResultDto
    {
        public int AttemptId { get; set; }
        public int ExamId { get; set; }
        public string ExamTitle { get; set; } = null!;
        public decimal? Score { get; set; }
        public decimal PassingMarks { get; set; }
        public bool? IsPassed { get; set; }
        public int? TimeTakenSeconds { get; set; }

        // False = answer review and topic chart are still locked; the UI shows the
        // score with a "review available once results are published" notice.
        public bool ResultsPublished { get; set; }

        public string? TopicScoresUrl { get; set; }

        // Per-question breakdown with correct answers. EMPTY until ResultsPublished —
        // an early finisher must not be able to leak the answer key while other
        // candidates are still taking the exam.
        public List<PerQuestionResultDto> Questions { get; set; } = new();
    }
}
