using System.ComponentModel.DataAnnotations;

namespace ExamPlatform.API.DTOs.Exam
{
    // Request body for POST /api/exams. Creates a Draft exam.
    // Cross-field rules (passingMarks ≤ totalMarks, startTime < endTime)
    // are enforced in ExamService since DataAnnotations can't express them.
    public class CreateExamDto
    {
        // The exam's category label. Send an existing category id, or any
        // value <= 0 to label the exam "Mixed" (questions from multiple
        // categories) — the backend resolves/creates that category by name.
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

        // Optional: when omitted the exam window defaults to StartTime + 30 days.
        // Candidates who START before EndTime always get the full duration,
        // even if they finish after the window closes.
        public DateTime? EndTime { get; set; }

        // Subscription & Payment System (additive): requesting a premium exam
        // is only honoured server-side if the examiner's current tier allows
        // it — ExamService re-checks this from the database, never trusting
        // these two fields alone.
        public bool IsPremium { get; set; }
        public decimal? Price { get; set; }

        // Completed attempts allowed per candidate (practice exams only).
        // Ignored for premium exams — the server always pins those to 1.
        [Range(1, 10, ErrorMessage = "Attempts allowed must be between 1 and 10.")]
        public int MaxAttempts { get; set; } = 1;
    }
}
