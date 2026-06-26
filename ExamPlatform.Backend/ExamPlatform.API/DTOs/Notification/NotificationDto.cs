namespace ExamPlatform.API.DTOs.Notification
{
    // Response DTO for a single in-app notification.
    // Returned from GET /api/notifications and POST /api/notifications/{id}/read.
    public class NotificationDto
    {
        public int NotificationId { get; set; }
        public string NotifType { get; set; } = null!;  // enum as string: ResultPublished, etc.
        public string Message { get; set; } = null!;
        public int? RelatedExamId { get; set; }         // nullable — not all notifs relate to an exam
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
