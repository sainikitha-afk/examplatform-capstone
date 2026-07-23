using ExamPlatform.BLL.Models;

namespace ExamPlatform.BLL.Interfaces
{
    // Contract for computing platform analytics.
    // Returns BLL-level value objects (records), not domain entities or DTOs.
    public interface IAnalyticsService
    {
        // Computes the candidate's personal performance dashboard.
        Task<CandidateDashboard> GetCandidateDashboardAsync(int userId);

        // Computes platform-wide statistics for the Admin overview.
        Task<PlatformStats> GetPlatformStatsAsync();

        // Computes per-topic accuracy (%) for one attempt directly from the database
        // (question analytic-category + marks + the candidate's answers). Replaces the
        // old static-file delivery so the radar chart works on Azure where the
        // filesystem-served /assets path does not exist. Returns null when the attempt
        // isn't the caller's, results aren't published, or there is nothing to show.
        Task<IReadOnlyDictionary<string, decimal>?> ComputeTopicScoresAsync(int attemptId, int userId);
    }
}
