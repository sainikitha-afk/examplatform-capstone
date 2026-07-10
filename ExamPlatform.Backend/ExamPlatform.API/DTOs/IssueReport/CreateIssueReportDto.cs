using System.ComponentModel.DataAnnotations;

namespace ExamPlatform.API.DTOs.IssueReport
{
    // Request body for POST /api/issue-reports — a candidate flags a problem.
    public class CreateIssueReportDto
    {
        // Which exam the report is about. Required.
        [Required]
        public int ExamId { get; set; }

        // Optional — the attempt in progress when the report was raised.
        public int? AttemptId { get; set; }

        // Optional — a specific question being flagged. Null = general issue.
        public int? QuestionId { get; set; }

        // The candidate's description. [Required]/[StringLength] give an
        // automatic 400 before the action runs; the service re-checks after trim.
        [Required(ErrorMessage = "Please describe the problem.")]
        [StringLength(1000, MinimumLength = 3,
            ErrorMessage = "Your message must be between 3 and 1000 characters.")]
        public string Message { get; set; } = null!;
    }
}
