namespace ExamPlatform.API.DTOs.Exam
{
    // Lightweight exam response used in list views (GET /api/exams, GET /api/exams/mine).
    // No nested question data — prevents sending large payloads for list browsing.
    public class ExamSummaryDto
    {
        public int ExamId { get; set; }
        public string Title { get; set; } = null!;
        public int CategoryId { get; set; }
        public int ExaminerId { get; set; }
        public int DurationMinutes { get; set; }
        public decimal TotalMarks { get; set; }
        public decimal PassingMarks { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string ExamStatus { get; set; } = null!; // enum stored as string for readability
    }
}
