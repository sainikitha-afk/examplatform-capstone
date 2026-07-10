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

        public string? TopicScoresUrl { get; set; }

        // Breakdown of each question in the exam and how the candidate answered
        public List<PerQuestionResultDto> Questions { get; set; } = new();
    }
}
