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
    }
}
