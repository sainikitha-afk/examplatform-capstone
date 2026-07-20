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
using ExamPlatform.DAL.Interfaces;
using ExamPlatform.Domain.Entities;
using ExamPlatform.Domain.Enums;

namespace ExamPlatform.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // all endpoints require a valid JWT
    public class ExamsController : ControllerBase
    {
        private readonly IExamService _examService;
        private readonly IAttemptService _attemptService;
        // Category service: resolves the special "Mixed" category by name when
        // an examiner creates a multi-category exam (catId <= 0 in the request).
        private readonly IExamCategoryService _categoryService;

        // Premium-exam access check for the candidate detail view — see
        // ToDetailForCandidateDto's HasAccess field.
        private readonly ICandidatePurchaseService _purchaseService;

        // Institute branding: on the single-exam detail view we surface the
        // owning examiner's Admin-Verified institution (name + logo) so the
        // "Institute Verified" badge can render. One lookup per detail request.
        private readonly IInstitutionRepository _institutionRepository;
        private readonly ILogger<ExamsController> _logger;

        public ExamsController(
            IExamService examService,
            IAttemptService attemptService,
            IExamCategoryService categoryService,
            ICandidatePurchaseService purchaseService,
            IInstitutionRepository institutionRepository,
            ILogger<ExamsController> logger)
        {
            _examService = examService;
            _attemptService = attemptService;
            _categoryService = categoryService;
            _purchaseService = purchaseService;
            _institutionRepository = institutionRepository;
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
            // Candidates only see exams they can still sit: one attempt per candidate,
            // so an exam they have completed is filtered out server-side (listing it
            // could only ever produce a 409 on Start). Examiners/Admins browsing this
            // endpoint keep the unfiltered published list — they don't take exams.
            var paged = User.IsInRole("Candidate")
                ? await _examService.GetAvailableForCandidatePagedAsync(
                    ExtractUserId(), categoryId, page, pageSize)
                : await _examService.GetPublishedPagedAsync(categoryId, page, pageSize);

            return Ok(new PagedResultDto<ExamSummaryDto>
            {
                Items           = await MapSummariesWithInstituteAsync(paged.Items),
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
                Items           = await MapSummariesWithInstituteAsync(paged.Items),
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

            // Resolve the owning examiner's Verified institution once (null when
            // they have none or it isn't approved yet) so the detail view can
            // show the "Institute Verified" badge with the institute's name/logo.
            var institute = await GetVerifiedInstituteAsync(exam.ExaminerId);

            if (isOwningExaminer || isAdmin)
                return Ok(ToDetailDto(exam, institute));            // includes IsCorrect

            var hasAccess = !exam.IsPremium
                || (User.IsInRole("Candidate") && await _purchaseService.HasActivePurchaseAsync(userId, id));

            // Attempt allowance for THIS candidate — the details page shows
            // "Attempt X of N" and hides Start once the limit is spent.
            var attemptsUsed = User.IsInRole("Candidate")
                ? await _attemptService.CountCompletedAttemptsAsync(userId, id)
                : 0;

            return Ok(ToDetailForCandidateDto(exam, hasAccess, institute, attemptsUsed)); // omits IsCorrect
        }

        /// <summary>
        /// Returns the examiner's institution only if it exists and an Admin has
        /// Verified it; otherwise null. Live-derived (never snapshotted here) so a
        /// later revocation immediately stops the badge from showing on exams.
        /// </summary>
        private async Task<Institution?> GetVerifiedInstituteAsync(int examinerId)
        {
            var institution = await _institutionRepository.GetByOwnerUserIdAsync(examinerId);
            return institution?.VerificationStatus == InstitutionVerificationStatus.Verified
                ? institution
                : null;
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
                TabSwitchCount = a.TabSwitchCount,
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

            // catId <= 0 means "Mixed" — a multi-category exam. The Mixed
            // category row is resolved (or created on first use) by name.
            var catId = dto.CatId;
            if (catId <= 0)
                catId = (await _categoryService.GetOrCreateMixedAsync()).CatId;

            var exam = await _examService.CreateAsync(
                userId, catId, dto.Title, dto.Instructions,
                dto.DurationMinutes, dto.TotalMarks, dto.PassingMarks,
                // End time is optional — default to a 30-day window from the start.
                dto.StartTime, dto.EndTime ?? dto.StartTime.AddDays(30),
                dto.IsPremium, dto.Price, dto.MaxAttempts);

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
                dto.StartTime, dto.EndTime ?? dto.StartTime.AddDays(30),
                dto.MaxAttempts);

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

        private static ExamSummaryDto ToSummaryDto(Exam e, Institution? institute = null) => new()
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
            ExamStatus = e.ExamStatus.ToString(),
            IsPremium = e.IsPremium,
            Price = e.Price,
            IsFeatured = e.IsFeatured,
            MaxAttempts = e.IsPremium ? 1 : e.MaxAttempts,
            // Institute badge only makes sense for premium exams (the certification
            // product) — a verified institute passed in here attaches its branding.
            IsInstituteVerified = e.IsPremium && institute != null,
            InstituteName = e.IsPremium ? institute?.Name : null,
            InstituteLogoUrl = e.IsPremium ? institute?.LogoUrl : null
        };

        /// <summary>
        /// Attaches the live "Institute Verified" badge to a page of exam summaries
        /// with a single batched institution lookup (no N+1). Only premium exams are
        /// considered — a non-premium exam never carries institute branding.
        /// </summary>
        private async Task<List<ExamSummaryDto>> MapSummariesWithInstituteAsync(IEnumerable<Exam> exams)
        {
            var list = exams.ToList();

            var premiumExaminerIds = list
                .Where(e => e.IsPremium)
                .Select(e => e.ExaminerId)
                .Distinct()
                .ToList();

            // One query for all examiners on this page; empty input short-circuits.
            var verified = premiumExaminerIds.Count == 0
                ? new List<Institution>()
                : (await _institutionRepository.GetVerifiedByOwnerIdsAsync(premiumExaminerIds)).ToList();

            var byOwner = verified
                .GroupBy(i => i.OwnerUserId)
                .ToDictionary(g => g.Key, g => g.First());

            return list
                .Select(e => ToSummaryDto(
                    e,
                    byOwner.TryGetValue(e.ExaminerId, out var inst) ? inst : null))
                .ToList();
        }

        private static ExamDetailDto ToDetailDto(Exam e, Institution? institute = null) => new()
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
            IsPremium = e.IsPremium,
            Price = e.Price,
            IsFeatured = e.IsFeatured,
            MaxAttempts = e.IsPremium ? 1 : e.MaxAttempts,
            IsInstituteVerified = institute != null,
            InstituteName = institute?.Name,
            InstituteLogoUrl = institute?.LogoUrl,
            Questions = e.ExamQuestions?
                .OrderBy(eq => eq.QuestionOrder)
                .Select(eq => new ExamQuestionDto
                {
                    ExamQuestionId = eq.ExamQuestionId,
                    QuestionId = eq.QuestionId,
                    QuestionOrder = eq.QuestionOrder,
                    QuestionText = eq.Question?.QuestionText ?? string.Empty,
                    CatId = eq.Question?.CatId ?? 0,
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

        private static ExamDetailForCandidateDto ToDetailForCandidateDto(
            Exam e, bool hasAccess = true, Institution? institute = null,
            int attemptsUsed = 0) => new()
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
            IsPremium = e.IsPremium,
            Price = e.Price,
            HasAccess = hasAccess,
            MaxAttempts = e.IsPremium ? 1 : e.MaxAttempts,
            AttemptsUsed = attemptsUsed,
            IsInstituteVerified = institute != null,
            InstituteName = institute?.Name,
            InstituteLogoUrl = institute?.LogoUrl,
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
