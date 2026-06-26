using System.ComponentModel.DataAnnotations;

namespace ExamPlatform.API.DTOs.Question
{
    // Request body for POST /api/questions.
    // DataAnnotations validate basic field constraints at the ASP.NET model-binding
    // layer (before the controller action runs). MCQ-specific rules (exactly 4 options,
    // exactly 1 correct) are enforced in QuestionService.
    public class CreateQuestionDto
    {
        [Required(ErrorMessage = "Category id is required.")]
        public int CatId { get; set; }

        [Required(ErrorMessage = "Question text is required.")]
        [MaxLength(1000, ErrorMessage = "Question text cannot exceed 1000 characters.")]
        public string QuestionText { get; set; } = null!;

        [Range(0.01, double.MaxValue, ErrorMessage = "Marks must be greater than 0.")]
        public decimal Marks { get; set; }

        // Accepted values: "Easy", "Medium", "Hard" — enforced at service level.
        [Required(ErrorMessage = "Difficulty is required.")]
        [MaxLength(10, ErrorMessage = "Difficulty cannot exceed 10 characters.")]
        public string Difficulty { get; set; } = null!;

        // The 4 options are sent as a nested array in the same request body.
        // Example: "options": [{"optionText":"A","isCorrect":false}, ...]
        [Required(ErrorMessage = "Options are required.")]
        public List<CreateQuestionOptionDto> Options { get; set; } = new();
    }
}
