using System.ComponentModel.DataAnnotations;

namespace ExamPlatform.API.DTOs.IssueReport
{
    // Request body for POST /api/issue-reports/{id}/status — an examiner triages.
    public class UpdateIssueReportStatusDto
    {
        // "Open" | "Reviewed" | "Resolved". Parsed to the IssueStatus enum in the
        // controller; an invalid value yields a friendly 400.
        [Required]
        public string Status { get; set; } = null!;

        // Optional note shown to the candidate when the report is resolved.
        [StringLength(1000, ErrorMessage = "The note is too long (max 1000 characters).")]
        public string? ResolutionNote { get; set; }
    }
}
