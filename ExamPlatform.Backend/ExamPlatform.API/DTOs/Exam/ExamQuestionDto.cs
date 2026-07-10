namespace ExamPlatform.API.DTOs.Exam
{
    // Represents one question within the exam detail view (examiner/admin).
    // Includes IsCorrect flags in its option list.
    public class ExamQuestionDto
    {
        public int ExamQuestionId { get; set; }
        public int QuestionId { get; set; }
        public int QuestionOrder { get; set; }
        public string QuestionText { get; set; } = null!;
        // Category of the underlying question — exams may mix categories, so
        // the examiner UI shows a per-question category and a breakdown.
        public int CatId { get; set; }
        public decimal Marks { get; set; }
        public string Difficulty { get; set; } = null!;
        public List<ExamOptionDto> Options { get; set; } = new();
    }
}
