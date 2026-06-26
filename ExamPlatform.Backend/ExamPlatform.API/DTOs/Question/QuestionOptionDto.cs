namespace ExamPlatform.API.DTOs.Question
{
    // Response DTO for a single MCQ option, including the IsCorrect flag.
    // Returned to Examiners and Admins who are allowed to see the answer key.
    // Candidates get ExamOptionForCandidateDto (in DTOs/Exam/) which omits IsCorrect.
    public class QuestionOptionDto
    {
        public int OptionId { get; set; }
        public string OptionText { get; set; } = null!;
        public bool IsCorrect { get; set; }   // exposes the correct-answer flag
    }
}
