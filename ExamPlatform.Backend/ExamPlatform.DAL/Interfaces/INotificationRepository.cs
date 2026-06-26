using ExamPlatform.Domain.Entities;

namespace ExamPlatform.DAL.Interfaces
{
    // INotificationRepository provides per-user notification queries.
    public interface INotificationRepository : IRepository<int, Notification>
    {
        // Returns all notifications for a user, newest first.
        // Used for GET /api/notifications.
        Task<IEnumerable<Notification>> GetByUserAsync(int userId);

        // Counts unread notifications for the badge count endpoint.
        Task<int> UnreadCountAsync(int userId);

        // Bulk-marks all unread notifications for a user as read.
        // Uses EF Core ExecuteUpdateAsync — a single SQL UPDATE with no entity load.
        Task MarkAllReadAsync(int userId);

        // Paginated variant of GetByUserAsync — applies user scope then Skip/Take.
        Task<(IEnumerable<Notification> Items, int TotalCount)> GetByUserPagedAsync(
            int userId, int page, int pageSize);
    }
}