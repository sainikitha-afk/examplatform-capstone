using System.ComponentModel.DataAnnotations;

namespace ExamPlatform.API.DTOs.Attempt
{
    // Request body for PUT /api/attempts/{id}/answers (auto-save).
    // selectedOptionId is nullable — null means the candidate cleared/skipped
    // the answer for this question.
    public class AnswerDto
    {
        [Required(ErrorMessage = "Question id is required.")]
        public int QuestionId { get; set; }

        // No [Required] — intentionally nullable so candidates can un-select an answer.
        public int? SelectedOptionId { get; set; }
    }
}
