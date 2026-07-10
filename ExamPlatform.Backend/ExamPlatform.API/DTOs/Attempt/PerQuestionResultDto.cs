namespace ExamPlatform.API.DTOs.Attempt
{
    // Per-question breakdown within AttemptResultDto.
    // Shows the candidate's selected answer, the correct answer, and the marks awarded.
    public class PerQuestionResultDto
    {
        public int QuestionId { get; set; }
        public string QuestionText { get; set; } = null!;
        // What the candidate chose (null if they skipped the question).
        public string? SelectedOptionText { get; set; }
        // The text of the option that was marked IsCorrect.
        public string? CorrectOptionText { get; set; }
        public decimal? MarksAwarded { get; set; }
        public bool? IsCorrect { get; set; }
        
        public string? AnalyticCategory { get; set; }
    }
}
