// ExamsController manages the full exam lifecycle from the HTTP layer.
// Examiner endpoints create and manage exams; the GET endpoints are accessible
// to any authenticated user (browse / detail view). The publish-results endpoint
// triggers certificate generation and notification fan-out inside the service.

// CA1848: standard ILogger extension methods used deliberately — see CertificateService.cs
#pragma warning disable CA1848
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using ExamPlatform.API.DTOs.Attempt;
using ExamPlatform.API.DTOs.Common;
using ExamPlatform.API.DTOs.Exam;
using ExamPlatform.BLL.Interfaces;
using ExamPlatform.Domain.Entities;

namespace ExamPlatform.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // all endpoints require a valid JWT
    public class ExamsController : ControllerBase
    {
        private readonly IExamService _examService;
        private readonly IAttemptService _attemptService;
        private readonly ILogger<ExamsController> _logger;

        public ExamsController(
            IExamService examService,
            IAttemptService attemptService,
            ILogger<ExamsController> logger)
        {
            _examService = examService;
            _attemptService = attemptService;
            _logger = logger;
        }

        // ════════════════════════════════════════════════════════════════
        //   GET /api/exams  — paginated browse of published exams
        //   Supports: ?page=1 &pageSize=20 &categoryId=N
        //   pageSize clamped to [1, 100] by the service.
        // ════════════════════════════════════════════════════════════════
        [HttpGet]
        public async Task<IActionResult> GetPublished(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] int? categoryId = null)
        {
            var paged = await _examService.GetPublishedPagedAsync(categoryId, page, pageSize);

            return Ok(new PagedResultDto<ExamSummaryDto>
            {
                Items           = paged.Items.Select(ToSummaryDto),
                Page            = paged.Page,
                PageSize        = paged.PageSize,
                TotalCount      = paged.TotalCount,
                TotalPages      = paged.TotalPages,
                HasNextPage     = paged.HasNextPage,
                HasPreviousPage = paged.HasPreviousPage
            });
        }

        // ════════════════════════════════════════════════════════════════
        //   GET /api/exams/mine  — examiner's own exams (all statuses), paginated
        //   MUST be declared before {id:int} to avoid routing conflicts.
        //   Supports: ?page=1 &pageSize=20
        // ════════════════════════════════════════════════════════════════
        [HttpGet("mine")]
        [Authorize(Roles = "Examiner")]
        public async Task<IActionResult> GetMine(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            var userId = ExtractUserId();
            var paged = await _examService.GetMinePagedAsync(userId, page, pageSize);

            return Ok(new PagedResultDto<ExamSummaryDto>
            {
                Items           = paged.Items.Select(ToSummaryDto),
                Page            = paged.Page,
                PageSize        = paged.PageSize,
                TotalCount      = paged.TotalCount,
                TotalPages      = paged.TotalPages,
                HasNextPage     = paged.HasNextPage,
                HasPreviousPage = paged.HasPreviousPage
            });
        }

        // ════════════════════════════════════════════════════════════════
        //   GET /api/exams/{id}  — exam detail
        //   Owning examiner → full DTO with IsCorrect.
        //   Everyone else → candidate DTO without IsCorrect.
        // ════════════════════════════════════════════════════════════════
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var userId = ExtractUserId();
            var exam = await _examService.GetByIdWithQuestionsAndOptionsAsync(id);

            // Security boundary: answer keys (IsCorrect) are only visible to:
            //   1. The examiner who owns this exam — they authored the questions.
            //   2. Admin — platform-wide oversight and content moderation.
            // Everyone else (candidates, other examiners) gets the candidate DTO
            // which omits IsCorrect entirely.
            var isOwningExaminer = User.IsInRole("Examiner") && exam.ExaminerId == userId;
            var isAdmin = User.IsInRole("Admin");

            if (isOwningExaminer || isAdmin)
                return Ok(ToDetailDto(exam));            // includes IsCorrect
            else
                return Ok(ToDetailForCandidateDto(exam)); // omits IsCorrect
        }

        // ════════════════════════════════════════════════════════════════
        //   GET /api/exams/{id}/attempts  — examiner attempt monitoring
        //   Every attempt on this exam (all statuses): who took it, live
        //   progress, disconnect signals (resumes/auto-submits), and scores.
        //   Examiner must own the exam; Admin can view any exam.
        // ════════════════════════════════════════════════════════════════
        [HttpGet("{id:int}/attempts")]
        [Authorize(Roles = "Examiner,Admin")]
        public async Task<IActionResult> GetAttempts(int id)
        {
            var userId = ExtractUserId();
            var isAdmin = User.IsInRole("Admin");

            var attempts = await _attemptService.GetAllByExamForExaminerAsync(id, userId, isAdmin);

            return Ok(attempts.Select(a => new ExamAttemptMonitorDto
            {
                AttemptId = a.AttemptId,
                UserId = a.UserId,
                CandidateName = a.User?.FullName ?? string.Empty,
                CandidateEmail = a.User?.Email ?? string.Empty,
                StartedAt = a.StartedAt,
                SubmittedAt = a.SubmittedAt,
                AttemptStatus = a.AttemptStatus.ToString(),
                Score = a.Score,
                IsPassed = a.IsPassed,
                TimeTakenSeconds = a.TimeTakenSeconds,
                AnsweredCount = a.AttemptAnswers?.Count(aa => aa.SelectedOptionId != null) ?? 0,
                TopicScoresUrl = a.TopicScoresUrl
            }));
        }

        // ════════════════════════════════════════════════════════════════
        //   POST /api/exams  — create a Draft exam
        // ════════════════════════════════════════════════════════════════
        [HttpPost]
        [Authorize(Roles = "Examiner")]
        public async Task<IActionResult> Create([FromBody] CreateExamDto dto)
        {
            var userId = ExtractUserId();

            var exam = await _examService.CreateAsync(
                userId, dto.CatId, dto.Title, dto.Instructions,
                dto.DurationMinutes, dto.TotalMarks, dto.PassingMarks,
                // End time is optional — default to a 30-day window from the start.
                dto.StartTime, dto.EndTime ?? dto.StartTime.AddDays(30));

            return CreatedAtAction(nameof(GetById), new { id = exam.ExamId }, ToSummaryDto(exam));
        }

        // ════════════════════════════════════════════════════════════════
        //   PUT /api/exams/{id}  — update a Draft exam
        // ════════════════════════════════════════════════════════════════
        [HttpPut("{id:int}")]
        [Authorize(Roles = "Examiner")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateExamDto dto)
        {
            var userId = ExtractUserId();

            var exam = await _examService.UpdateAsync(
                id, userId, dto.Title, dto.Instructions,
                dto.DurationMinutes, dto.TotalMarks, dto.PassingMarks,
                // End time is optional — default to a 30-day window from the start.
                dto.StartTime, dto.EndTime ?? dto.StartTime.AddDays(30));

            return Ok(ToSummaryDto(exam));
        }

        // ════════════════════════════════════════════════════════════════
        //   DELETE /api/exams/{id}  — delete a Draft exam
        // ════════════════════════════════════════════════════════════════
        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Examiner")]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = ExtractUserId();
            await _examService.DeleteAsync(id, userId);
            return NoContent();
        }

        // ════════════════════════════════════════════════════════════════
        //   POST /api/exams/{id}/questions  — attach questions (bulk)
        // ════════════════════════════════════════════════════════════════
        [HttpPost("{id:int}/questions")]
        [Authorize(Roles = "Examiner")]
        public async Task<IActionResult> AttachQuestions(int id, [FromBody] AttachQuestionsDto dto)
        {
            var userId = ExtractUserId();
            var exam = await _examService.AttachQuestionsAsync(id, dto.QuestionIds, userId);
            return Ok(ToDetailDto(exam));
        }

        // ════════════════════════════════════════════════════════════════
        //   DELETE /api/exams/{id}/questions/{qid}  — detach one question
        // ════════════════════════════════════════════════════════════════
        [HttpDelete("{id:int}/questions/{qid:int}")]
        [Authorize(Roles = "Examiner")]
        public async Task<IActionResult> DetachQuestion(int id, int qid)
        {
            var userId = ExtractUserId();
            var exam = await _examService.DetachQuestionAsync(id, qid, userId);
            return Ok(ToDetailDto(exam));
        }

        // ════════════════════════════════════════════════════════════════
        //   POST /api/exams/{id}/publish  — Draft → Published
        // ════════════════════════════════════════════════════════════════
        [HttpPost("{id:int}/publish")]
        [Authorize(Roles = "Examiner")]
        public async Task<IActionResult> Publish(int id)
        {
            var userId = ExtractUserId();
            var exam = await _examService.PublishAsync(id, userId);
            return Ok(ToSummaryDto(exam));
        }

        // ════════════════════════════════════════════════════════════════
        //   POST /api/exams/{id}/publish-results  — Published → ResultsPublished
        // ════════════════════════════════════════════════════════════════
        [HttpPost("{id:int}/publish-results")]
        [Authorize(Roles = "Examiner")]
        public async Task<IActionResult> PublishResults(int id)
        {
            var userId = ExtractUserId();
            _logger.LogInformation(
                "POST /api/exams/{ExamId}/publish-results by examiner {UserId}.", id, userId);
            var exam = await _examService.PublishResultsAsync(id, userId);
            return Ok(ToSummaryDto(exam));
        }

        // ════════════════════════════════════════════════════════════════
        //   POST /api/exams/{id}/unpublish  — Published → Draft (rollback)
        //
        //   Roles: Examiner (must own the exam) or Admin (any exam).
        //   Blocked if: any attempt exists, or status is not Published,
        //   or results have already been published.
        //
        //   Returns the updated exam summary on success so the caller can
        //   confirm the status field is now "Draft".
        // ════════════════════════════════════════════════════════════════
        /// <summary>
        /// Reverts a Published exam back to Draft for administrative correction.
        /// Rejected (409) if the exam has any existing attempts.
        /// Rejected (409) if the exam is already in ResultsPublished status.
        /// Rejected (404) if the caller is an Examiner and does not own the exam.
        /// </summary>
        [HttpPost("{id:int}/unpublish")]
        [Authorize(Roles = "Examiner,Admin")] // Examiner (owns it) OR Admin (any exam)
        public async Task<IActionResult> Unpublish(int id)
        {
            var userId = ExtractUserId();

            // Admins can act on any exam; examiners are restricted to their own.
            // We pass the flag down so the service can apply the correct ownership rule.
            var isAdmin = User.IsInRole("Admin");

            var exam = await _examService.UnpublishAsync(id, userId, isAdmin);

            // Return the updated summary so the client can confirm ExamStatus == "Draft".
            return Ok(new
            {
                message = "Exam reverted to Draft successfully.",
                exam = ToSummaryDto(exam)
            });
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

        private static ExamSummaryDto ToSummaryDto(Exam e) => new()
        {
            ExamId = e.ExamId,
            Title = e.Title,
            CategoryId = e.CatId,
            ExaminerId = e.ExaminerId,
            DurationMinutes = e.DurationMinutes,
            TotalMarks = e.TotalMarks,
            PassingMarks = e.PassingMarks,
            StartTime = e.StartTime,
            EndTime = e.EndTime,
            ExamStatus = e.ExamStatus.ToString()
        };

        private static ExamDetailDto ToDetailDto(Exam e) => new()
        {
            ExamId = e.ExamId,
            Title = e.Title,
            Instructions = e.Instructions,
            CategoryId = e.CatId,
            ExaminerId = e.ExaminerId,
            DurationMinutes = e.DurationMinutes,
            TotalMarks = e.TotalMarks,
            PassingMarks = e.PassingMarks,
            StartTime = e.StartTime,
            EndTime = e.EndTime,
            ExamStatus = e.ExamStatus.ToString(),
            CreatedAt = e.CreatedAt,
            Questions = e.ExamQuestions?
                .OrderBy(eq => eq.QuestionOrder)
                .Select(eq => new ExamQuestionDto
                {
                    ExamQuestionId = eq.ExamQuestionId,
                    QuestionId = eq.QuestionId,
                    QuestionOrder = eq.QuestionOrder,
                    QuestionText = eq.Question?.QuestionText ?? string.Empty,
                    Marks = eq.Question?.Marks ?? 0,
                    Difficulty = eq.Question?.Difficulty ?? string.Empty,
                    Options = eq.Question?.Options?.Select(o => new ExamOptionDto
                    {
                        OptionId = o.OptionId,
                        OptionText = o.OptionText,
                        IsCorrect = o.IsCorrect  // answer key included
                    }).ToList() ?? new()
                }).ToList() ?? new()
        };

        private static ExamDetailForCandidateDto ToDetailForCandidateDto(Exam e) => new()
        {
            ExamId = e.ExamId,
            Title = e.Title,
            Instructions = e.Instructions,
            CategoryId = e.CatId,
            ExaminerId = e.ExaminerId,
            DurationMinutes = e.DurationMinutes,
            TotalMarks = e.TotalMarks,
            PassingMarks = e.PassingMarks,
            StartTime = e.StartTime,
            EndTime = e.EndTime,
            ExamStatus = e.ExamStatus.ToString(),
            CreatedAt = e.CreatedAt,
            Questions = e.ExamQuestions?
                .OrderBy(eq => eq.QuestionOrder)
                .Select(eq => new ExamQuestionForCandidateDto
                {
                    ExamQuestionId = eq.ExamQuestionId,
                    QuestionId = eq.QuestionId,
                    QuestionOrder = eq.QuestionOrder,
                    QuestionText = eq.Question?.QuestionText ?? string.Empty,
                    Marks = eq.Question?.Marks ?? 0,
                    Difficulty = eq.Question?.Difficulty ?? string.Empty,
                    Options = eq.Question?.Options?.Select(o => new ExamOptionForCandidateDto
                    {
                        OptionId = o.OptionId,
                        OptionText = o.OptionText
                        // IsCorrect deliberately omitted
                    }).ToList() ?? new()
                }).ToList() ?? new()
        };
    }
}
