namespace ExamPlatform.API.DTOs.Exam
{
    // Lightweight exam response used in list views (GET /api/exams, GET /api/exams/mine).
    // No nested question data — prevents sending large payloads for list browsing.
    public class ExamSummaryDto
    {
        public int ExamId { get; set; }
        public string Title { get; set; } = null!;
        public int CategoryId { get; set; }
        public int ExaminerId { get; set; }
        public int DurationMinutes { get; set; }
        public decimal TotalMarks { get; set; }
        public decimal PassingMarks { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string ExamStatus { get; set; } = null!; // enum stored as string for readability

        // Subscription & Payment System (additive) — premium/featured badges.
        public bool IsPremium { get; set; }
        public decimal? Price { get; set; }
        public bool IsFeatured { get; set; }

        // Completed attempts allowed per candidate (always 1 for premium exams).
        public int MaxAttempts { get; set; } = 1;

        // Institute badge (live-derived): populated only for premium exams whose
        // examiner has an Admin-verified institution, so browse cards can show the
        // institute name + logo. Null/false for everything else.
        public bool IsInstituteVerified { get; set; }
        public string? InstituteName { get; set; }
        public string? InstituteLogoUrl { get; set; }
    }
}
