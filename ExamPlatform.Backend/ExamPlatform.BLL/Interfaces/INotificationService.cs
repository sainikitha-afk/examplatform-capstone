using ExamPlatform.BLL.Models;
using ExamPlatform.Domain.Entities;

namespace ExamPlatform.BLL.Interfaces
{
    // Contract for the in-app notification inbox.
    public interface INotificationService
    {
        // Returns all notifications for the calling user, newest first.
        Task<IEnumerable<Notification>> GetByUserAsync(int userId);

        // Returns a paginated page of notifications for the calling user, newest first.
        Task<PagedResult<Notification>> GetByUserPagedAsync(int userId, int page, int pageSize);

        // Returns the count of unread notifications for the badge indicator.
        Task<int> GetUnreadCountAsync(int userId);

        // Marks a single notification as read. Validates ownership.
        Task<Notification> MarkAsReadAsync(int notificationId, int userId);

        // Marks all of the user's unread notifications as read in a single operation.
        Task MarkAllReadAsync(int userId);
    }
}
