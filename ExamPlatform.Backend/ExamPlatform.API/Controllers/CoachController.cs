// CoachController — the ExamHub AI Learning Coach.
//
// AUTHORIZATION SHAPE (deliberate, and not uniform across the actions):
//
//   [Authorize] at class level          → every endpoint requires a valid JWT. Nothing here
//                                         is anonymous: an unauthenticated visitor cannot burn
//                                         Gemini quota, and rate limits can key on a user id.
//
//   /ask, /snapshot → Candidate only    → these read candidate performance data, so they carry
//                                         [Authorize(Roles = "Candidate")]. LearningCoachService
//                                         re-checks the role as well (defence in depth).
//
//   /help → any authenticated role      → general platform guidance touches NO user data; it is
//                                         answered from a curated knowledge file. Examiners and
//                                         Admins get real help instead of a locked door, without
//                                         being handed candidate analytics.
//
// The user id ALWAYS comes from the JWT (ExtractUserId, same helper pattern as every other
// controller here). No action accepts a user id, role, attempt id or file path from the body.

// CA1848: standard ILogger extension methods used deliberately — see CertificateService.cs
#pragma warning disable CA1848
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Logging;
using ExamPlatform.API.DTOs.Coach;
using ExamPlatform.BLL.Interfaces;
using ExamPlatform.BLL.Models;

namespace ExamPlatform.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]                          // base-level JWT check; roles are set per action
    [EnableRateLimiting(RateLimitPolicies.AiCoach)]   // applies to THIS controller only
    public class CoachController : ControllerBase
    {
        private readonly ILearningCoachService _coachService;
        private readonly ILogger<CoachController> _logger;

        public CoachController(
            ILearningCoachService coachService,
            ILogger<CoachController> logger)
        {
            _coachService = coachService;
            _logger = logger;
        }

        // ════════════════════════════════════════════════════════════════
        //   GET /api/coach/snapshot  — deterministic summary, no AI call
        // ════════════════════════════════════════════════════════════════
        [HttpGet("snapshot")]
        [Authorize(Roles = "Candidate")]
        public async Task<IActionResult> GetSnapshot(CancellationToken cancellationToken)
        {
            var userId = ExtractUserId();
            _logger.LogInformation("GET /api/coach/snapshot: candidate {UserId}.", userId);

            var snapshot = await _coachService.GetSnapshotAsync(userId, ExtractRole(), cancellationToken);
            return Ok(ToDto(snapshot));
        }

        // ════════════════════════════════════════════════════════════════
        //   POST /api/coach/ask  — personalized coaching (Candidate only)
        // ════════════════════════════════════════════════════════════════
        [HttpPost("ask")]
        [Authorize(Roles = "Candidate")]
        public async Task<IActionResult> Ask(
            [FromBody] CoachAskRequestDto dto, CancellationToken cancellationToken)
        {
            var userId = ExtractUserId();
            // The message itself is NOT logged — it is user content, and the answer is built
            // from the candidate's performance data. Outcome and duration are logged in the BLL.
            _logger.LogInformation("POST /api/coach/ask: candidate {UserId}.", userId);

            var answer = await _coachService.AskAsync(
                BuildQuery(userId, CoachMode.Personalized, dto), cancellationToken);

            return Ok(ToDto(answer));
        }

        // ════════════════════════════════════════════════════════════════
        //   POST /api/coach/help  — general platform guidance (any role)
        // ════════════════════════════════════════════════════════════════
        [HttpPost("help")]
        public async Task<IActionResult> Help(
            [FromBody] CoachAskRequestDto dto, CancellationToken cancellationToken)
        {
            var userId = ExtractUserId();
            _logger.LogInformation(
                "POST /api/coach/help: user {UserId} ({Role}).", userId, ExtractRole());

            var answer = await _coachService.AskAsync(
                BuildQuery(userId, CoachMode.General, dto), cancellationToken);

            return Ok(ToDto(answer));
        }

        // ───────────── PRIVATE HELPERS ─────────────

        // Identity comes from the token, never from the request body. Same helper as
        // AttemptsController/AnalyticsController.
        private int ExtractUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                        ?? User.FindFirst("sub")?.Value
                        ?? User.FindFirst("nameid")?.Value;

            if (!int.TryParse(claim, out var id))
                throw new UnauthorizedAccessException("Invalid token: missing user id claim.");
            return id;
        }

        // Role likewise comes from the validated token. A role in the body would be a lie
        // waiting to happen, so the DTO does not even have the field.
        private string ExtractRole() => User.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty;

        private CoachQuery BuildQuery(int userId, CoachMode mode, CoachAskRequestDto dto) => new(
            UserId: userId,
            Role: ExtractRole(),
            Mode: mode,
            Message: dto.Message,
            History: (dto.History ?? new List<CoachTurnDto>())
                .Select(t => new CoachTurn(t.Question ?? string.Empty, t.Answer ?? string.Empty))
                .ToList());

        private static CoachAnswerDto ToDto(CoachAnswer a) => new()
        {
            Answer = a.Answer,
            Summary = a.Summary,
            StrongTopics = a.StrongTopics.Select(ToDto).ToList(),
            FocusAreas = a.FocusAreas.Select(ToDto).ToList(),
            StudyPlan = a.StudyPlan.Select(d => new CoachStudyDayDto
            {
                Day = d.Day,
                Focus = d.Focus,
                Tasks = d.Tasks.ToList()
            }).ToList(),
            SuggestedQuestions = a.SuggestedQuestions.ToList(),
            HasPerformanceData = a.HasPerformanceData,
            GeneratedAt = a.GeneratedAt
        };

        private static CoachSnapshotDto ToDto(CoachSnapshot s) => new()
        {
            FirstName = s.FirstName,
            HasData = s.HasData,
            TotalAttempts = s.TotalAttempts,
            AverageScore = s.AverageScore,
            PassRate = s.PassRate,
            CertificatesEarned = s.CertificatesEarned,
            Topics = s.Topics.Select(ToDto).ToList(),
            StrongTopics = s.StrongTopics.Select(ToDto).ToList(),
            FocusAreas = s.FocusAreas.Select(ToDto).ToList()
        };

        private static CoachTopicDto ToDto(TopicScore t) => new()
        {
            Topic = t.Topic,
            Score = t.Score
        };

        private static CoachFocusAreaDto ToDto(FocusArea f) => new()
        {
            Topic = f.Topic,
            Score = f.Score,
            Priority = f.Priority.ToString(),
            Reason = f.Reason
        };
    }
}
