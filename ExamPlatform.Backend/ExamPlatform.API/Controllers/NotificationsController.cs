// NotificationsController exposes the current user's in-app notification inbox.
// All endpoints are available to any authenticated user — each user sees only
// their own notifications (enforced via JWT claim extraction).

// CA1848: standard ILogger extension methods used deliberately — see CertificateService.cs
#pragma warning disable CA1848
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using ExamPlatform.API.DTOs.Common;
using ExamPlatform.API.DTOs.Notification;
using ExamPlatform.BLL.Interfaces;
using ExamPlatform.Domain.Entities;

namespace ExamPlatform.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // any authenticated user
    public class NotificationsController : ControllerBase
    {
        private readonly INotificationService _notificationService;
        private readonly ILogger<NotificationsController> _logger;

        public NotificationsController(
            INotificationService notificationService,
            ILogger<NotificationsController> logger)
        {
            _notificationService = notificationService;
            _logger = logger;
        }

        // ════════════════════════════════════════════════════════════════
        //   GET /api/notifications  — current user's notifications, newest first, paginated
        //   Supports: ?page=1 &pageSize=20
        //   pageSize clamped to [1, 100] by the service.
        // ════════════════════════════════════════════════════════════════
        [HttpGet]
        public async Task<IActionResult> GetAll(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            var userId = ExtractUserId();
            var paged = await _notificationService.GetByUserPagedAsync(userId, page, pageSize);

            return Ok(new PagedResultDto<NotificationDto>
            {
                Items           = paged.Items.Select(ToDto),
                Page            = paged.Page,
                PageSize        = paged.PageSize,
                TotalCount      = paged.TotalCount,
                TotalPages      = paged.TotalPages,
                HasNextPage     = paged.HasNextPage,
                HasPreviousPage = paged.HasPreviousPage
            });
        }

        // ════════════════════════════════════════════════════════════════
        //   GET /api/notifications/unread-count  — badge count
        //   MUST be declared before {id:int} to avoid routing conflict.
        // ════════════════════════════════════════════════════════════════
        [HttpGet("unread-count")]
        public async Task<IActionResult> GetUnreadCount()
        {
            var userId = ExtractUserId();
            var count = await _notificationService.GetUnreadCountAsync(userId);
            // Return the integer directly — the response body is just { "count": 3 }.
            return Ok(new { count });
        }

        // ════════════════════════════════════════════════════════════════
        //   POST /api/notifications/{id}/read  — mark one notification read
        // ════════════════════════════════════════════════════════════════
        [HttpPost("{id:int}/read")]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            var userId = ExtractUserId();
            var notif = await _notificationService.MarkAsReadAsync(id, userId);
            return Ok(ToDto(notif));
        }

        // ════════════════════════════════════════════════════════════════
        //   POST /api/notifications/read-all  — mark ALL notifications read
        //   MUST be declared before {id:int}/read — literal beats template.
        // ════════════════════════════════════════════════════════════════
        [HttpPost("read-all")]
        public async Task<IActionResult> MarkAllRead()
        {
            var userId = ExtractUserId();
            _logger.LogInformation(
                "POST /api/notifications/read-all: user {UserId}.", userId);
            await _notificationService.MarkAllReadAsync(userId);
            return NoContent(); // 204 — operation complete, nothing to return
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

        private static NotificationDto ToDto(Notification n) => new()
        {
            NotificationId = n.NotificationId,
            NotifType = n.NotifType.ToString(), // enum → string for JSON readability
            Message = n.Message,
            RelatedExamId = n.RelatedExamId,
            IsRead = n.IsRead,
            CreatedAt = n.CreatedAt
        };
    }
}
