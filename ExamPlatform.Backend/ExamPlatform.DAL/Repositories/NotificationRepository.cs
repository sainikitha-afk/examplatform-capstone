using Microsoft.EntityFrameworkCore;
using ExamPlatform.DAL.Contexts;
using ExamPlatform.DAL.Interfaces;
using ExamPlatform.Domain.Entities;

namespace ExamPlatform.DAL.Repositories
{
    // NotificationRepository provides per-user notification access and
    // a bulk read-mark operation for the notification inbox.
    public class NotificationRepository : AbstractRepository<int, Notification>, INotificationRepository
    {
        public NotificationRepository(ExamPlatformContext context) : base(context) { }

        // ════════════════════════════════════════════════════════════════
        //   GetByUserAsync
        // ════════════════════════════════════════════════════════════════
        /// <summary>Returns all notifications for a user, newest first.</summary>
        public async Task<IEnumerable<Notification>> GetByUserAsync(int userId)
        {
            return await _dbSet
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt) // newest notifications first
                .ToListAsync();
        }

        // ════════════════════════════════════════════════════════════════
        //   UnreadCountAsync
        // ════════════════════════════════════════════════════════════════
        /// <summary>Returns the count of unread notifications for the badge.</summary>
        public async Task<int> UnreadCountAsync(int userId)
        {
            // CountAsync = SELECT COUNT(*) WHERE ... — no rows materialised.
            return await _dbSet.CountAsync(n => n.UserId == userId && !n.IsRead);
        }

        // ════════════════════════════════════════════════════════════════
        //   MarkAllReadAsync
        // ════════════════════════════════════════════════════════════════
        /// <summary>
        /// Marks every unread notification for a user as read in a single SQL UPDATE.
        /// </summary>
        public async Task MarkAllReadAsync(int userId)
        {
            // ExecuteUpdateAsync (EF Core 7+) translates to:
            //   UPDATE notifications SET is_read = true
            //   WHERE user_id = @userId AND is_read = false
            //
            // Key differences from the usual Update() pattern:
            //   • Does NOT load entities into memory first — no SELECT.
            //   • Does NOT go through the EF change tracker.
            //   • Executes immediately — no SaveChangesAsync needed.
            //   • One round-trip regardless of how many rows match.
            await _dbSet
                .Where(n => n.UserId == userId && !n.IsRead)
                .ExecuteUpdateAsync(setters =>
                    setters.SetProperty(n => n.IsRead, true));
        }

        // ════════════════════════════════════════════════════════════════
        //   GetByUserPagedAsync
        // ════════════════════════════════════════════════════════════════
        /// <summary>
        /// Paginated user inbox: same ORDER BY CreatedAt DESC as GetByUserAsync,
        /// but returns only one page of items and the total count.
        /// </summary>
        public async Task<(IEnumerable<Notification> Items, int TotalCount)> GetByUserPagedAsync(
            int userId, int page, int pageSize)
        {
            var query = _dbSet
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt);

            var totalCount = await query.CountAsync();
            var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
            return (items, totalCount);
        }
    }
}