using System.ComponentModel.DataAnnotations;

namespace ExamPlatform.API.DTOs.Exam
{
    // Request body for POST /api/exams. Creates a Draft exam.
    // Cross-field rules (passingMarks ≤ totalMarks, startTime < endTime)
    // are enforced in ExamService since DataAnnotations can't express them.
    public class CreateExamDto
    {
        [Required(ErrorMessage = "Category id is required.")]
        public int CatId { get; set; }

        [Required(ErrorMessage = "Exam title is required.")]
        [MaxLength(200, ErrorMessage = "Title cannot exceed 200 characters.")]
        public string Title { get; set; } = null!;

        [MaxLength(2000, ErrorMessage = "Instructions cannot exceed 2000 characters.")]
        public string? Instructions { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Duration must be at least 1 minute.")]
        public int DurationMinutes { get; set; }

        [Range(0.01, double.MaxValue, ErrorMessage = "Total marks must be greater than 0.")]
        public decimal TotalMarks { get; set; }

        [Range(0.01, double.MaxValue, ErrorMessage = "Passing marks must be greater than 0.")]
        public decimal PassingMarks { get; set; }

        [Required(ErrorMessage = "Start time is required.")]
        public DateTime StartTime { get; set; }

        [Required(ErrorMessage = "End time is required.")]
        public DateTime EndTime { get; set; }
    }
}
