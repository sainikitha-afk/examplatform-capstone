using System.ComponentModel.DataAnnotations;

namespace ExamPlatform.API.DTOs.Question
{
    // Request body for PUT /api/questions/{id}.
    // Identical to CreateQuestionDto but without CatId — the category of a
    // question cannot be changed after creation (it would affect exam-category
    // consistency). Options are replaced in full (all 4 required every time).
    public class UpdateQuestionDto
    {
        [Required(ErrorMessage = "Question text is required.")]
        [MaxLength(1000, ErrorMessage = "Question text cannot exceed 1000 characters.")]
        public string QuestionText { get; set; } = null!;

        [Range(0.01, double.MaxValue, ErrorMessage = "Marks must be greater than 0.")]
        public decimal Marks { get; set; }

        [Required(ErrorMessage = "Difficulty is required.")]
        [MaxLength(10, ErrorMessage = "Difficulty cannot exceed 10 characters.")]
        public string Difficulty { get; set; } = null!;

        // All 4 options must be sent even for minor text edits — the service
        // deletes and re-inserts the full set atomically.
        [Required(ErrorMessage = "Options are required.")]
        public List<CreateQuestionOptionDto> Options { get; set; } = new();
    }
}
