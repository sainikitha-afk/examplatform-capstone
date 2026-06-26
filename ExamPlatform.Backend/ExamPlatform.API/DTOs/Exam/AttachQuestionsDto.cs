using System.ComponentModel.DataAnnotations;

namespace ExamPlatform.API.DTOs.Exam
{
    // Request body for POST /api/exams/{id}/questions.
    // Sends a list of questionIds to attach in bulk to the exam.
    public class AttachQuestionsDto
    {
        [Required(ErrorMessage = "At least one question id is required.")]
        [MinLength(1, ErrorMessage = "At least one question id is required.")]
        public List<int> QuestionIds { get; set; } = new();
    }
}
