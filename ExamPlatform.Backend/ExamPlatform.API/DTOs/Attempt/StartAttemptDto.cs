using System.ComponentModel.DataAnnotations;

namespace ExamPlatform.API.DTOs.Attempt
{
    // Request body for POST /api/attempts/start.
    // Identifies which exam the candidate wants to attempt.
    public class StartAttemptDto
    {
        [Required(ErrorMessage = "Exam id is required.")]
        public int ExamId { get; set; }
    }
}
