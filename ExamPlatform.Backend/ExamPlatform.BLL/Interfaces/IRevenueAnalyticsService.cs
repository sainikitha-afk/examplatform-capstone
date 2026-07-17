using ExamPlatform.BLL.Models;

namespace ExamPlatform.BLL.Interfaces
{
    // Admin Revenue Dashboard (Part 7) — pure aggregation over Payment and
    // ExaminerSubscription rows. Admin-only; the controller enforces the role.
    public interface IRevenueAnalyticsService
    {
        Task<RevenueSummary> GetSummaryAsync();
    }
}
