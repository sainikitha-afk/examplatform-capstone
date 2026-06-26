// NotificationService manages the in-app notification inbox for any user role.
// Notifications are created by ExamService (publish-results) and read by users
// through the NotificationsController.

// CA1848: standard ILogger extension methods used deliberately — see CertificateService.cs
#pragma warning disable CA1848
using Microsoft.Extensions.Logging;
using ExamPlatform.BLL.Exceptions;
using ExamPlatform.BLL.Interfaces;
using ExamPlatform.BLL.Models;
using ExamPlatform.DAL.Interfaces;
using ExamPlatform.Domain.Entities;

namespace ExamPlatform.BLL.Services
{
    public class NotificationService : INotificationService
    {
        private readonly INotificationRepository _notifRepository;

        // Logger for notification read/mark events. These are user-facing state
        // changes worth surfacing if something behaves unexpectedly.
        private readonly ILogger<NotificationService> _logger;

        public NotificationService(
            INotificationRepository notifRepository,
            ILogger<NotificationService> logger)
        {
            _notifRepository = notifRepository;
            _logger = logger;
        }

        // ════════════════════════════════════════════════════════════════
        //   GET BY USER
        // ════════════════════════════════════════════════════════════════
        public async Task<IEnumerable<Notification>> GetByUserAsync(int userId)
        {
            // GetByUserAsync already orders by CreatedAt DESC.
            var notifications = await _notifRepository.GetByUserAsync(userId);
            _logger.LogInformation(
                "User {UserId} retrieved their notification inbox.", userId);
            return notifications;
        }

        // ════════════════════════════════════════════════════════════════
        //   GET BY USER PAGED
        // ════════════════════════════════════════════════════════════════
        public async Task<PagedResult<Notification>> GetByUserPagedAsync(
            int userId, int page, int pageSize)
        {
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 100);

            _logger.LogInformation(
                "User {UserId} retrieving notification inbox page {Page} (pageSize={PageSize}).",
                userId, page, pageSize);

            var (items, totalCount) =
                await _notifRepository.GetByUserPagedAsync(userId, page, pageSize);

            return new PagedResult<Notification>
            {
                Items = items,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount
            };
        }

        // ════════════════════════════════════════════════════════════════
        //   UNREAD COUNT
        // ════════════════════════════════════════════════════════════════
        public async Task<int> GetUnreadCountAsync(int userId)
        {
            return await _notifRepository.UnreadCountAsync(userId);
        }

        // ════════════════════════════════════════════════════════════════
        //   MARK ONE AS READ
        // ════════════════════════════════════════════════════════════════
        public async Task<Notification> MarkAsReadAsync(int notificationId, int userId)
        {
            // ─── LOAD ──────────────────────────────────────────────────
            var notif = await _notifRepository.GetByIdAsync(notificationId);

            // Treat "not found" and "belongs to another user" identically.
            // 404 rather than 403 — no information leak.
            if (notif == null || notif.UserId != userId)
            {
                _logger.LogWarning(
                    "User {UserId} attempted to mark notification {NotifId} as read — not found or not owned.",
                    userId, notificationId);
                throw new UserNotFoundException(
                    $"Notification {notificationId} not found for this user.");
            }

            // ─── IDEMPOTENCY ───────────────────────────────────────────
            // Already read — no-op, return as-is.
            if (notif.IsRead)
            {
                _logger.LogWarning(
                    "User {UserId} marked notification {NotifId} as read, but it was already read; no-op.",
                    userId, notificationId);
                return notif;
            }

            notif.IsRead = true;
            _notifRepository.Update(notif);
            await _notifRepository.SaveChangesAsync();

            _logger.LogInformation(
                "Notification {NotifId} marked as read by user {UserId}.",
                notificationId, userId);

            return notif;
        }

        // ════════════════════════════════════════════════════════════════
        //   MARK ALL AS READ
        // ════════════════════════════════════════════════════════════════
        public async Task MarkAllReadAsync(int userId)
        {
            // MarkAllReadAsync uses EF Core ExecuteUpdateAsync — a single
            // UPDATE SQL statement with no entity loading or change tracking.
            // No SaveChangesAsync needed (ExecuteUpdateAsync is immediate).
            await _notifRepository.MarkAllReadAsync(userId);

            _logger.LogInformation(
                "All notifications marked as read for user {UserId}.", userId);
        }
    }
}
