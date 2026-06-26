namespace ExamPlatform.API.DTOs.Question
{
    // Full question response including the answer key (IsCorrect).
    // Returned only to Examiners (their own questions) and Admins.
    public class QuestionDto
    {
        public int QuestionId { get; set; }
        public int ExaminerId { get; set; }
        public int CatId { get; set; }
        public string QuestionText { get; set; } = null!;
        public decimal Marks { get; set; }
        public string Difficulty { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
        public bool IsActive { get; set; }
        // Nested options list — populated from the Include in QuestionRepository.
        public List<QuestionOptionDto> Options { get; set; } = new();
    }
}
