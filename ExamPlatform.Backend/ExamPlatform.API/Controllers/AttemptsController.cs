// AttemptsController handles the candidate exam-taking workflow:
// start/resume, answer auto-save, manual submit, history, and result view.
// All endpoints are restricted to Candidates via [Authorize(Roles="Candidate")].

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
    [Authorize] // base-level JWT check; each action specifies Candidate role
    public class AttemptsController : ControllerBase
    {
        private readonly IAttemptService _attemptService;
        private readonly IInstitutionRepository _institutionRepository;
        private readonly ILogger<AttemptsController> _logger;

        public AttemptsController(
            IAttemptService attemptService,
            IInstitutionRepository institutionRepository,
            ILogger<AttemptsController> logger)
        {
            _attemptService = attemptService;
            _institutionRepository = institutionRepository;
            _logger = logger;
        }

        /// <summary>
        /// Resolves the exam examiner's Verified institution for the exam-taking
        /// screen — but only for a premium exam. Live-derived (never snapshotted),
        /// so a revoked verification immediately stops the in-progress badge.
        /// Returns null for non-premium exams or an unverified/absent institution.
        /// </summary>
        private async Task<Institution?> ResolveVerifiedInstituteAsync(Attempt attempt)
        {
            if (attempt.Exam is not { IsPremium: true } exam) return null;
            var institution = await _institutionRepository.GetByOwnerUserIdAsync(exam.ExaminerId);
            return institution?.VerificationStatus == InstitutionVerificationStatus.Verified
                ? institution
                : null;
        }

        // ════════════════════════════════════════════════════════════════
        //   POST /api/attempts/start  — start or resume an attempt
        // ════════════════════════════════════════════════════════════════
        [HttpPost("start")]
        [Authorize(Roles = "Candidate")]
        public async Task<IActionResult> Start([FromBody] StartAttemptDto dto)
        {
            var userId = ExtractUserId();
            _logger.LogInformation(
                "POST /api/attempts/start: user {UserId} for exam {ExamId}.", userId, dto.ExamId);
            var attempt = await _attemptService.StartOrResumeAsync(userId, dto.ExamId);
            return Ok(ToResumeDto(attempt, await ResolveVerifiedInstituteAsync(attempt)));
        }

        // ════════════════════════════════════════════════════════════════
        //   GET /api/attempts/mine  — candidate's attempt history, paginated
        //   MUST be declared BEFORE {id:int} to avoid routing ambiguity.
        //   ASP.NET route matching: literal segment "mine" beats the
        //   integer-constrained "{id:int}" template when both could match.
        //   Supports: ?page=1 &pageSize=20
        //   pageSize clamped to [1, 100] by the service.
        // ════════════════════════════════════════════════════════════════
        [HttpGet("mine")]
        [Authorize(Roles = "Candidate")]
        public async Task<IActionResult> GetMine(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            var userId = ExtractUserId();
            var paged = await _attemptService.GetByUserPagedAsync(userId, page, pageSize);

            return Ok(new PagedResultDto<AttemptSummaryDto>
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
        //   GET /api/attempts/{id}  — resume view (remaining time + questions)
        // ════════════════════════════════════════════════════════════════
        [HttpGet("{id:int}")]
        [Authorize(Roles = "Candidate")]
        public async Task<IActionResult> GetById(int id)
        {
            var userId = ExtractUserId();
            var attempt = await _attemptService.GetByIdAsync(id, userId);
            return Ok(ToResumeDto(attempt, await ResolveVerifiedInstituteAsync(attempt)));
        }

        // ════════════════════════════════════════════════════════════════
        //   PUT /api/attempts/{id}/answers  — auto-save one answer
        // ════════════════════════════════════════════════════════════════
        [HttpPut("{id:int}/answers")]
        [Authorize(Roles = "Candidate")]
        public async Task<IActionResult> SaveAnswer(int id, [FromBody] AnswerDto dto)
        {
            var userId = ExtractUserId();
            await _attemptService.SaveAnswerAsync(id, dto.QuestionId, dto.SelectedOptionId, userId);
            // 204 No Content — answer saved; nothing to return.
            return NoContent();
        }

        // ════════════════════════════════════════════════════════════════
        //   POST /api/attempts/{id}/submit  — manual submission
        // ════════════════════════════════════════════════════════════════
        [HttpPost("{id:int}/submit")]
        [Authorize(Roles = "Candidate")]
        public async Task<IActionResult> Submit(int id)
        {
            var userId = ExtractUserId();
            _logger.LogInformation(
                "POST /api/attempts/{AttemptId}/submit: user {UserId}.", id, userId);
            var attempt = await _attemptService.SubmitAsync(id, userId);
            return Ok(ToSummaryDto(attempt));
        }

        // ════════════════════════════════════════════════════════════════
        //   POST /api/attempts/{id}/tab-switch  — proctoring signal
        //   The browser calls this when the candidate leaves the exam tab or
        //   window during an InProgress attempt. Server records the count and
        //   alerts the examiner once the threshold is crossed. Returns the new
        //   total so the UI can show "Warning 2 of 3".
        // ════════════════════════════════════════════════════════════════
        [HttpPost("{id:int}/tab-switch")]
        [Authorize(Roles = "Candidate")]
        public async Task<IActionResult> RecordTabSwitch(int id)
        {
            var userId = ExtractUserId();
            var count = await _attemptService.RecordTabSwitchAsync(id, userId);
            return Ok(new { tabSwitchCount = count });
        }

        // ════════════════════════════════════════════════════════════════
        //   GET /api/attempts/{id}/result  — full result with per-question breakdown
        // ════════════════════════════════════════════════════════════════
        [HttpGet("{id:int}/result")]
        [Authorize(Roles = "Candidate")]
        public async Task<IActionResult> GetResult(int id)
        {
            var userId = ExtractUserId();
            var attempt = await _attemptService.GetResultAsync(id, userId);
            return Ok(ToResultDto(attempt));
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

        /// <summary>Maps to the resume DTO including server remaining time and questions.</summary>
        private static AttemptResumeDto ToResumeDto(Attempt a, Institution? institute = null)
        {
            var exam = a.Exam;

            // Re-compute remaining seconds for the response. The service may have
            // auto-submitted already; if so, this returns 0 (or the attempt is no
            // longer InProgress so the frontend hides the timer).
            int remainingSeconds = 0;
            if (exam != null && a.AttemptStatus == Domain.Enums.AttemptStatus.InProgress)
            {
                var durationSecs = exam.DurationMinutes * 60.0;
                var elapsed = (DateTime.UtcNow - a.StartedAt).TotalSeconds;
                remainingSeconds = (int)Math.Max(0, durationSecs - elapsed);
            }

            // Shuffle questions and options deterministically based on the attempt ID.
            // This ensures every candidate gets a unique question/option order, but it
            // remains consistent for them if they refresh or resume the attempt.
            var questionRnd = new Random(a.AttemptId);

            // ExamQuestions may be null if the exam was loaded without Include —
            // guard with null-coalescing to return an empty list gracefully.
            var questions = exam?.ExamQuestions?
                .OrderBy(eq => questionRnd.Next())
                .Select(eq =>
                {
                    // Find the candidate's currently saved answer for this question.
                    var savedAnswer = a.AttemptAnswers?
                        .FirstOrDefault(aa => aa.QuestionId == eq.QuestionId);

                    // Seed unique to this attempt + question to shuffle options consistently
                    var optionRnd = new Random(a.AttemptId + eq.QuestionId);

                    return new QuestionInAttemptDto
                    {
                        QuestionId = eq.QuestionId,
                        QuestionText = eq.Question?.QuestionText ?? string.Empty,
                        Marks = eq.Question?.Marks ?? 0,
                        Difficulty = eq.Question?.Difficulty ?? string.Empty,
                        SelectedOptionId = savedAnswer?.SelectedOptionId,
                        // Options without IsCorrect — candidate must not see answers.
                        Options = eq.Question?.Options?
                            .OrderBy(o => optionRnd.Next())
                            .Select(o => new ExamOptionForCandidateDto
                            {
                                OptionId = o.OptionId,
                                OptionText = o.OptionText
                            }).ToList() ?? new()
                    };
                }).ToList() ?? new();

            return new AttemptResumeDto
            {
                AttemptId = a.AttemptId,
                ExamId = a.ExamId,
                ExamTitle = exam?.Title ?? string.Empty,
                StartedAt = a.StartedAt,
                RemainingSeconds = remainingSeconds,
                AttemptStatus = a.AttemptStatus.ToString(),
                IsInstituteVerified = institute != null,
                InstituteName = institute?.Name,
                InstituteLogoUrl = institute?.LogoUrl,
                Questions = questions
            };
        }

        /// <summary>Maps to the lightweight summary DTO for the history list.</summary>
        private static AttemptSummaryDto ToSummaryDto(Attempt a)
        {
            // Business rule (revised): the score and pass/fail are visible as soon as the
            // attempt is submitted — MCQ scoring is automatic, so there is nothing to wait
            // for. Only the topic chart stays gated on the examiner publishing results,
            // because it belongs to the analytics suite, which reports official published
            // results only (as do the dashboard and the AI coach).
            var resultsOut = a.Exam?.ExamStatus == Domain.Enums.ExamStatus.ResultsPublished;

            return new()
            {
                AttemptId = a.AttemptId,
                ExamId = a.ExamId,
                ExamTitle = a.Exam?.Title,   // populated if GetByUserAsync includes Exam
                StartedAt = a.StartedAt,
                SubmittedAt = a.SubmittedAt,
                AttemptStatus = a.AttemptStatus.ToString(),
                Score = a.Score,
                IsPassed = a.IsPassed,
                TopicScoresUrl = resultsOut && a.TopicScoresUrl != null
                    ? $"/api/analytics/attempts/{a.AttemptId}/topic-scores" : null
            };
        }

        /// <summary>Maps to the result DTO. The per-question breakdown (which contains
        /// correct answers) and the topic chart are included only after the examiner has
        /// published results; the score itself is always present once submitted.</summary>
        private static AttemptResultDto ToResultDto(Attempt a)
        {
            var exam = a.Exam!;
            var resultsOut = exam.ExamStatus == Domain.Enums.ExamStatus.ResultsPublished;

            // Correct answers must not leave the server before results are published —
            // an early finisher could otherwise hand the answer key to candidates whose
            // exam window is still open. So the breakdown is not merely hidden by the
            // UI; it is never serialized.
            var questionResults = new List<PerQuestionResultDto>();
            if (resultsOut)
            {
                var answers = a.AttemptAnswers.ToList();

                questionResults = exam.ExamQuestions
                    .OrderBy(eq => eq.QuestionOrder)
                    .Select(eq =>
                    {
                        var question = eq.Question;
                        var answer = answers.FirstOrDefault(aa => aa.QuestionId == question.QuestionId);

                        // The correct option text — find the option with IsCorrect = true.
                        var correctOption = question.Options.FirstOrDefault(o => o.IsCorrect);

                        return new PerQuestionResultDto
                        {
                            QuestionId = question.QuestionId,
                            QuestionText = question.QuestionText,
                            // SelectedOptionText is the text the candidate actually chose.
                            // answer?.SelectedOption comes from the Include chain in the repo.
                            SelectedOptionText = answer?.SelectedOption?.OptionText,
                            CorrectOptionText = correctOption?.OptionText,
                            MarksAwarded = answer?.MarksAwarded,
                            IsCorrect = answer?.IsCorrect,
                            AnalyticCategory = eq.AnalyticCategory
                        };
                    }).ToList();
            }

            var resultDto = new AttemptResultDto
            {
                AttemptId = a.AttemptId,
                ExamId = a.ExamId,
                ExamTitle = exam.Title,
                Score = a.Score,
                PassingMarks = exam.PassingMarks,
                IsPassed = a.IsPassed,
                TimeTakenSeconds = a.TimeTakenSeconds,
                ResultsPublished = resultsOut,
                TopicScoresUrl = resultsOut && a.TopicScoresUrl != null
                    ? $"/api/analytics/attempts/{a.AttemptId}/topic-scores" : null,
                Questions = questionResults
            };
            return resultDto;
        }
    }
}
