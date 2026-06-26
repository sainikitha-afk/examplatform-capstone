namespace ExamPlatform.API.DTOs.Exam
{
    // Full exam detail for Examiners (their own exam) and Admins.
    // Includes the correct-answer flags in the nested option list.
    public class ExamDetailDto
    {
        public int ExamId { get; set; }
        public string Title { get; set; } = null!;
        public string? Instructions { get; set; }
        public int CategoryId { get; set; }
        public int ExaminerId { get; set; }
        public int DurationMinutes { get; set; }
        public decimal TotalMarks { get; set; }
        public decimal PassingMarks { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string ExamStatus { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
        // Questions ordered by QuestionOrder, with full option detail including IsCorrect.
        public List<ExamQuestionDto> Questions { get; set; } = new();
    }
}
