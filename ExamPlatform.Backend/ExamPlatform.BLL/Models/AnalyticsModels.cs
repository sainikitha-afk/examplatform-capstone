// Analytics value objects returned by IAnalyticsService.
// These are BLL-layer models (not DTOs, not entities) — they carry computed
// statistics that the API layer maps to response DTOs.

namespace ExamPlatform.BLL.Models
{
    // Per-exam best score for the candidate dashboard.
    public record ExamBestScore(
        int ExamId,
        string ExamTitle,
        decimal BestScore,
        bool IsPassed,
        string? TopicScoresUrl);

    // Candidate dashboard aggregate statistics.
    public record CandidateDashboard(
        int TotalAttempts,
        double AverageScore,
        double PassRate,
        int CertificatesEarned,
        IEnumerable<ExamBestScore> BestScores);

    // Platform-wide statistics for the Admin dashboard.
    public record PlatformStats(
        int TotalUsers,
        int TotalExaminers,
        int TotalCandidates,
        int TotalExams,
        int TotalAttempts,
        int TotalCertificatesIssued,
        int ExamsCompletedLast30Days);
}
