namespace ExamPlatform.API.DTOs.Question
{
    // Response DTO for GET /api/questions/stats.
    // Examiner: counts scoped to their own questions.
    // Admin: platform-wide counts.
    public class QuestionStatsDto
    {
        public int TotalQuestions { get; set; }
        public int Easy { get; set; }
        public int Medium { get; set; }
        public int Hard { get; set; }
        public List<CategoryQuestionCountDto> ByCategory { get; set; } = new();
    }
}
