// AnalyticsService computes dashboard statistics by loading data via
// existing repository methods and aggregating in-memory with LINQ.
//
// Note on approach: loading all rows in-memory is correct and clear for
// this educational project. In a production system with millions of rows,
// you would replace these in-memory aggregations with SQL COUNT/AVG/GROUP BY
// queries pushed down to the database (EF Core LINQ-to-SQL or raw queries).
// Comments call this out explicitly so the trade-off is understood.

// CA1848: standard ILogger extension methods used deliberately — see CertificateService.cs
#pragma warning disable CA1848
using Microsoft.Extensions.Logging;
using ExamPlatform.BLL.Interfaces;
using ExamPlatform.BLL.Models;
using ExamPlatform.DAL.Interfaces;
using ExamPlatform.Domain.Enums;

namespace ExamPlatform.BLL.Services
{
    public class AnalyticsService : IAnalyticsService
    {
        // ───────────── DEPENDENCIES ─────────────
        private readonly IAttemptRepository _attemptRepository;
        private readonly ICertificateRepository _certRepository;
        private readonly IUserRepository _userRepository;
        private readonly IExamRepository _examRepository;

        // ILogger<AnalyticsService>: analytics queries are read-only and relatively
        // heavy (they load multiple tables). Logging entry + exit makes it easy to
        // diagnose slow response times in production.
        private readonly ILogger<AnalyticsService> _logger;

        public AnalyticsService(
            IAttemptRepository attemptRepository,
            ICertificateRepository certRepository,
            IUserRepository userRepository,
            IExamRepository examRepository,
            ILogger<AnalyticsService> logger)
        {
            _attemptRepository = attemptRepository;
            _certRepository = certRepository;
            _userRepository = userRepository;
            _examRepository = examRepository;
            _logger = logger;
        }

        // ════════════════════════════════════════════════════════════════
        //   CANDIDATE DASHBOARD
        // ════════════════════════════════════════════════════════════════
        /// <summary>Computes personal stats for the candidate's dashboard.</summary>
        public async Task<CandidateDashboard> GetCandidateDashboardAsync(int userId)
        {
            _logger.LogInformation("Computing candidate dashboard for user {UserId}.", userId);

            // ─── LOAD DATA ────────────────────────────────────────────
            // GetByUserAsync includes the Exam navigation (for title and passing marks).
            var allAttempts = (await _attemptRepository.GetByUserAsync(userId)).ToList();

            // Filter to only evaluated (submitted) attempts — InProgress attempts
            // don't have scores yet.
            var submittedAttempts = allAttempts
                .Where(a => a.AttemptStatus == AttemptStatus.Submitted ||
                            a.AttemptStatus == AttemptStatus.AutoSubmitted)
                .ToList();

            // GetByUserAsync on certificate repository includes Exam navigation.
            var certs = (await _certRepository.GetByUserAsync(userId)).ToList();

            // ─── COMPUTE METRICS ──────────────────────────────────────

            // Total submitted attempts (InProgress excluded — no score yet).
            int totalAttempts = submittedAttempts.Count;

            // Average score across all evaluated attempts.
            // In production: push to SQL AVG(); here we use LINQ Average().
            double averageScore = totalAttempts > 0
                ? (double)submittedAttempts.Average(a => a.Score ?? 0m)
                : 0.0;

            // Pass rate: what percentage of submitted attempts resulted in a pass.
            int passedCount = submittedAttempts.Count(a => a.IsPassed == true);
            double passRate = totalAttempts > 0
                ? (double)passedCount / totalAttempts * 100.0
                : 0.0;

            int certificatesEarned = certs.Count;

            // Best score per exam: group by ExamId, take the highest score.
            // GroupBy in LINQ-to-Objects: materialised after the ToList() above.
            var bestScores = submittedAttempts
                .GroupBy(a => a.ExamId)
                .Select(g =>
                {
                    // MaxBy selects the attempt with the highest score in this group.
                    var best = g.MaxBy(a => a.Score ?? 0m)!;
                    return new ExamBestScore(
                        ExamId: g.Key,
                        ExamTitle: best.Exam?.Title ?? string.Empty,
                        BestScore: best.Score ?? 0m,
                        IsPassed: best.IsPassed == true,
                        TopicScoresUrl: best.TopicScoresUrl);
                })
                .OrderBy(b => b.ExamTitle)
                .ToList();

            var dashboard = new CandidateDashboard(
                TotalAttempts: totalAttempts,
                AverageScore: Math.Round(averageScore, 2),
                PassRate: Math.Round(passRate, 2),
                CertificatesEarned: certificatesEarned,
                BestScores: bestScores);

            _logger.LogInformation(
                "Dashboard computed for user {UserId}: totalAttempts={Total}, passRate={PassRate}%.",
                userId, totalAttempts, Math.Round(passRate, 1));

            return dashboard;
        }

        // ════════════════════════════════════════════════════════════════
        //   PLATFORM STATS (Admin)
        // ════════════════════════════════════════════════════════════════
        /// <summary>Computes platform-wide aggregate statistics for the Admin.</summary>
        public async Task<PlatformStats> GetPlatformStatsAsync()
        {
            _logger.LogInformation("Computing platform-wide analytics stats.");

            // ─── LOAD ALL DATA ────────────────────────────────────────
            // In production these would be SQL COUNT(*) queries, not full loads.
            // For this educational project, in-memory LINQ keeps the code readable
            // and avoids raw SQL complexity.
            var allUsers    = (await _userRepository.GetAllAsync()).ToList();
            var allExams    = (await _examRepository.GetAllAsync()).ToList();
            var allAttempts = (await _attemptRepository.GetAllAsync()).ToList();
            var allCerts    = (await _certRepository.GetAllAsync()).ToList();

            // ─── COMPUTE METRICS ──────────────────────────────────────
            int totalUsers      = allUsers.Count(u => u.Role == UserRole.Candidate);
            int totalExaminers  = allUsers.Count(u => u.Role == UserRole.Examiner);
            int totalCandidates = totalUsers; // alias for clarity in the response

            int totalExams    = allExams.Count;
            int totalAttempts = allAttempts.Count;
            int totalCertsIssued = allCerts.Count(c => c.CertStatus == CertificateStatus.Issued);

            // "Exams completed in the last 30 days" — interpreted as exams whose
            // EndTime fell within the last 30 days AND that have had results
            // published (status ResultsPublished or Archived).
            var cutoff = DateTime.UtcNow.AddDays(-30);
            int examsCompletedLast30Days = allExams.Count(e =>
                e.EndTime >= cutoff &&
                (e.ExamStatus == ExamStatus.ResultsPublished ||
                 e.ExamStatus == ExamStatus.Archived));

            var stats = new PlatformStats(
                TotalUsers: totalUsers,
                TotalExaminers: totalExaminers,
                TotalCandidates: totalCandidates,
                TotalExams: totalExams,
                TotalAttempts: totalAttempts,
                TotalCertificatesIssued: totalCertsIssued,
                ExamsCompletedLast30Days: examsCompletedLast30Days);

            _logger.LogInformation(
                "Platform stats computed: users={Users}, exams={Exams}, attempts={Attempts}, certs={Certs}.",
                totalUsers, totalExams, totalAttempts, totalCertsIssued);

            return stats;
        }
    }
}
