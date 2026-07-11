// AnalyticsController exposes performance dashboards.
// Candidates view their own stats; Admins view platform-wide aggregates.

// CA1848: standard ILogger extension methods used deliberately — see CertificateService.cs
#pragma warning disable CA1848
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using ExamPlatform.API.DTOs.Analytics;
using ExamPlatform.BLL.Interfaces;
using ExamPlatform.BLL.Models;

namespace ExamPlatform.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class AnalyticsController : ControllerBase
    {
        private readonly IAnalyticsService _analyticsService;
        private readonly ILogger<AnalyticsController> _logger;

        public AnalyticsController(
            IAnalyticsService analyticsService,
            ILogger<AnalyticsController> logger)
        {
            _analyticsService = analyticsService;
            _logger = logger;
        }

        // ════════════════════════════════════════════════════════════════
        //   GET /api/analytics/me  — candidate performance dashboard
        // ════════════════════════════════════════════════════════════════
        [HttpGet("me")]
        [Authorize(Roles = "Candidate")]
        public async Task<IActionResult> GetMyDashboard()
        {
            var userId = ExtractUserId();
            _logger.LogInformation(
                "GET /api/analytics/me: candidate {UserId} requested dashboard.", userId);
            var dashboard = await _analyticsService.GetCandidateDashboardAsync(userId);
            return Ok(ToDto(dashboard));
        }

        // ════════════════════════════════════════════════════════════════
        //   GET /api/analytics/platform  — admin platform overview
        // ════════════════════════════════════════════════════════════════
        [HttpGet("platform")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetPlatformStats()
        {
            _logger.LogInformation("GET /api/analytics/platform requested by admin.");
            var stats = await _analyticsService.GetPlatformStatsAsync();
            return Ok(ToDto(stats));
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

        private static CandidateDashboardDto ToDto(CandidateDashboard d) => new()
        {
            TotalAttempts = d.TotalAttempts,
            AverageScore = d.AverageScore,
            PassRate = d.PassRate,
            CertificatesEarned = d.CertificatesEarned,
            BestScores = d.BestScores.Select(b => new ExamBestScoreDto
            {
                ExamId = b.ExamId,
                ExamTitle = b.ExamTitle,
                BestScore = b.BestScore,
                IsPassed = b.IsPassed,
                TopicScoresUrl = b.TopicScoresUrl
            }).ToList()
        };

        private static PlatformStatsDto ToDto(PlatformStats s) => new()
        {
            TotalUsers = s.TotalUsers,
            TotalExaminers = s.TotalExaminers,
            TotalCandidates = s.TotalCandidates,
            TotalExams = s.TotalExams,
            TotalAttempts = s.TotalAttempts,
            TotalCertificatesIssued = s.TotalCertificatesIssued,
            ExamsCompletedLast30Days = s.ExamsCompletedLast30Days
        };
    }
}
