// IssueReportsController exposes the candidate "flag / report a problem"
// workflow and the examiner triage inbox.
//   Candidate:  POST /api/issue-reports          (raise a report)
//   Examiner:   GET  /api/issue-reports          (their exams' reports)
//               GET  /api/issue-reports/open-count
//               POST /api/issue-reports/{id}/status  (review / resolve)
// Admins are intentionally NOT wired here — reports flow to the exam's examiner,
// who escalates to the admin by email if it is a genuine technical fault.

// CA1848: standard ILogger extension methods used deliberately — see CertificateService.cs
#pragma warning disable CA1848
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using ExamPlatform.API.DTOs.Common;
using ExamPlatform.API.DTOs.IssueReport;
using ExamPlatform.BLL.Exceptions;
using ExamPlatform.BLL.Interfaces;
using ExamPlatform.Domain.Entities;
using ExamPlatform.Domain.Enums;

namespace ExamPlatform.API.Controllers
{
    [ApiController]
    [Route("api/issue-reports")]
    [Authorize]
    public class IssueReportsController : ControllerBase
    {
        private readonly IIssueReportService _issueService;
        private readonly ILogger<IssueReportsController> _logger;

        public IssueReportsController(
            IIssueReportService issueService,
            ILogger<IssueReportsController> logger)
        {
            _issueService = issueService;
            _logger = logger;
        }

        // ════════════════════════════════════════════════════════════════
        //   POST /api/issue-reports  — candidate raises a report
        // ════════════════════════════════════════════════════════════════
        [HttpPost]
        [Authorize(Roles = "Candidate")]
        public async Task<IActionResult> Create([FromBody] CreateIssueReportDto dto)
        {
            var candidateId = ExtractUserId();
            _logger.LogInformation(
                "POST /api/issue-reports: candidate {UserId} flagging exam {ExamId} (question {QuestionId}).",
                candidateId, dto.ExamId, dto.QuestionId);

            var report = await _issueService.RaiseAsync(
                candidateId, dto.ExamId, dto.AttemptId, dto.QuestionId, dto.Message);

            return Ok(ToDto(report));
        }

        // ════════════════════════════════════════════════════════════════
        //   GET /api/issue-reports  — examiner inbox (their exams' reports)
        //   ?page=1 &pageSize=20 &openOnly=true
        // ════════════════════════════════════════════════════════════════
        [HttpGet]
        [Authorize(Roles = "Examiner,Admin")]
        public async Task<IActionResult> GetForExaminer(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] bool openOnly = false)
        {
            var examinerId = ExtractUserId();
            var paged = await _issueService.GetForExaminerPagedAsync(
                examinerId, page, pageSize, openOnly);

            return Ok(new PagedResultDto<IssueReportDto>
            {
                Items           = paged.Items.Select(ToDto),
                Page            = paged.Page,
                PageSize        = paged.PageSize,
                TotalCount      = paged.TotalCount,
                TotalPages      = paged.TotalPages,
                HasNextPage     = paged.HasNextPage,
                HasPreviousPage = paged.HasPreviousPage
            });
        }

        // ════════════════════════════════════════════════════════════════
        //   GET /api/issue-reports/open-count  — badge for the examiner
        // ════════════════════════════════════════════════════════════════
        [HttpGet("open-count")]
        [Authorize(Roles = "Examiner,Admin")]
        public async Task<IActionResult> GetOpenCount()
        {
            var examinerId = ExtractUserId();
            var count = await _issueService.OpenCountForExaminerAsync(examinerId);
            return Ok(new { count });
        }

        // ════════════════════════════════════════════════════════════════
        //   POST /api/issue-reports/{id}/status  — examiner reviews/resolves
        // ════════════════════════════════════════════════════════════════
        [HttpPost("{id:int}/status")]
        [Authorize(Roles = "Examiner,Admin")]
        public async Task<IActionResult> UpdateStatus(
            int id, [FromBody] UpdateIssueReportStatusDto dto)
        {
            // Parse the status string to the enum with a friendly failure.
            if (!Enum.TryParse<IssueStatus>(dto.Status, ignoreCase: true, out var status))
                throw new ValidationException(
                    "Status must be one of: Open, Reviewed, Resolved.");

            var examinerId = ExtractUserId();
            var report = await _issueService.UpdateStatusAsync(
                id, examinerId, status, dto.ResolutionNote);

            return Ok(ToDto(report));
        }

        // ───────────── PRIVATE HELPERS ─────────────

        private int ExtractUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                        ?? User.FindFirst("sub")?.Value
                        ?? User.FindFirst("nameid")?.Value;

            if (!int.TryParse(claim, out var id))
                throw new UnauthorizedAccessException("Invalid token: missing user id claim.");
            return id;
        }

        // Entity → DTO. CandidateName/QuestionText are only populated when the
        // navigation properties were Include-d (examiner list path).
        private static IssueReportDto ToDto(IssueReport r) => new()
        {
            IssueReportId  = r.IssueReportId,
            ExamId         = r.ExamId,
            ExamTitle      = r.Exam?.Title ?? string.Empty,
            AttemptId      = r.AttemptId,
            QuestionId     = r.QuestionId,
            CandidateName  = r.Candidate?.FullName,
            QuestionText   = r.Question?.QuestionText,
            Message        = r.Message,
            Status         = r.Status.ToString(),
            ResolutionNote = r.ResolutionNote,
            CreatedAt      = r.CreatedAt,
            ResolvedAt     = r.ResolvedAt
        };
    }
}
