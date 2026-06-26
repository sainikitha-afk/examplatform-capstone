using System.ComponentModel.DataAnnotations;

namespace ExamPlatform.API.DTOs.Question
{
    // Represents one MCQ option within the CreateQuestionDto request body.
    // 4 of these are required; exactly 1 must have IsCorrect = true.
    // Validation of those MCQ rules happens in QuestionService, not here,
    // because DataAnnotations can't express cross-item cardinality constraints.
    public class CreateQuestionOptionDto
    {
        [Required(ErrorMessage = "Option text is required.")]
        [MaxLength(500, ErrorMessage = "Option text cannot exceed 500 characters.")]
        public string OptionText { get; set; } = null!;

        // No [Required] on bool — false is a valid, intentional value.
        public bool IsCorrect { get; set; }
    }
}
