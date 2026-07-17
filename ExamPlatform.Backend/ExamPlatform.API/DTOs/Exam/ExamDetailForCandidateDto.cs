namespace ExamPlatform.API.DTOs.Exam
{
    // Exam detail for candidates (and any non-owning-examiner caller).
    // Structurally identical to ExamDetailDto EXCEPT the options nested list
    // uses ExamOptionForCandidateDto (no IsCorrect field).
    // Having two separate DTOs is intentional: it makes the type-system enforce
    // the security boundary at compile time rather than relying on runtime if-checks.
    public class ExamDetailForCandidateDto
    {
        public int ExamId { get; set; }
        public string Title { get; set; } = null!;
        public string? Instructions { get; set; }
        public int CategoryId { get; set; }
        public int ExaminerId { get; set; }
        public int DurationMinutes { get; set; }
        public decimal TotalMarks { get; set; }
        public decimal PassingMarks { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string ExamStatus { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
        public bool IsPremium { get; set; }
        public decimal? Price { get; set; }
        // True when the candidate already holds an Active purchase for this
        // exam (or the exam isn't premium at all) — the frontend uses this,
        // not IsPremium alone, to decide whether to show the attempt button
        // or the "Purchase to Unlock" CTA.
        public bool HasAccess { get; set; }

        // Institute branding (Part 3/7) — populated live only when the owning
        // examiner has an Admin-Verified institution. Lets the candidate see
        // which institute is certifying this exam, on the details screen and
        // while taking it.
        public bool IsInstituteVerified { get; set; }
        public string? InstituteName { get; set; }
        public string? InstituteLogoUrl { get; set; }
        // Same structure as ExamDetailDto but questions use candidate-safe option DTO.
        public List<ExamQuestionForCandidateDto> Questions { get; set; } = new();
    }

    // Nested DTO for a question in the candidate view — no IsCorrect.
    public class ExamQuestionForCandidateDto
    {
        public int ExamQuestionId { get; set; }
        public int QuestionId { get; set; }
        public int QuestionOrder { get; set; }
        public string QuestionText { get; set; } = null!;
        public decimal Marks { get; set; }
        public string Difficulty { get; set; } = null!;
        public List<ExamOptionForCandidateDto> Options { get; set; } = new();
    }
}
