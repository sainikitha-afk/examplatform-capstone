namespace ExamPlatform.API.DTOs.Attempt
{
    // Full result view returned by GET /api/attempts/{id}/result.
    // Only available when exam.ExamStatus == ResultsPublished.
    public class AttemptResultDto
    {
        public int AttemptId { get; set; }
        public int ExamId { get; set; }
        public string ExamTitle { get; set; } = null!;
        public decimal? Score { get; set; }
        public decimal PassingMarks { get; set; }
        public bool? IsPassed { get; set; }
        public int? TimeTakenSeconds { get; set; }
        // Per-question breakdown — which answer was chosen, which was correct,
        // and how many marks were awarded for each.
        public List<PerQuestionResultDto> Questions { get; set; } = new();
    }
}
