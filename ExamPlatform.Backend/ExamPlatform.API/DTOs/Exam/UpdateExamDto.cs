using System.ComponentModel.DataAnnotations;

namespace ExamPlatform.API.DTOs.Exam
{
    // Request body for PUT /api/exams/{id}. Same fields as CreateExamDto.
    // CatId is excluded — the category of an exam cannot be changed after creation
    // (it would affect exam-question category relationships).
    public class UpdateExamDto
    {
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

        [Required]
        public DateTime StartTime { get; set; }

        // Optional — same defaulting rule as CreateExamDto (StartTime + 30 days).
        public DateTime? EndTime { get; set; }

        // Completed attempts allowed per candidate (practice exams only).
        // Ignored for premium exams — the server always pins those to 1.
        [Range(1, 10, ErrorMessage = "Attempts allowed must be between 1 and 10.")]
        public int MaxAttempts { get; set; } = 1;
    }
}
