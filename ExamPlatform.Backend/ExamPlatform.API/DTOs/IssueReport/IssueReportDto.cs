namespace ExamPlatform.API.DTOs.IssueReport
{
    // Response DTO for a candidate-raised issue report.
    // Candidate-facing views see their own reports; examiner views additionally
    // get the candidate name and question text for context.
    public class IssueReportDto
    {
        public int IssueReportId { get; set; }
        public int ExamId { get; set; }
        public string ExamTitle { get; set; } = string.Empty;
        public int? AttemptId { get; set; }
        public int? QuestionId { get; set; }

        // Populated for examiner views (null for the candidate's own list).
        public string? CandidateName { get; set; }
        public string? QuestionText { get; set; }

        public string Message { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;   // enum as string
        public string? ResolutionNote { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ResolvedAt { get; set; }
    }
}
