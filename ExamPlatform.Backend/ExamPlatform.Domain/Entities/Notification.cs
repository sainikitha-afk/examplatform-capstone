using ExamPlatform.Domain.Enums;

namespace ExamPlatform.Domain.Entities
{
    // Maps to "notifications". TRANSACTIONAL — created on key events
    // (result published, certificate issued/revoked). Persisted in DB even
    // after the email is sent, so the user can view notification history
    // in-app (FR10.2).
    public class Notification
    {
        public int NotificationId { get; set; }               // PK

        public int UserId { get; set; }                       // FK → User (recipient)

        public NotificationType NotifType { get; set; }

        // The display message. Pre-built at notification creation time so
        // we don't have to recompute it for each fetch (and so it stays
        // accurate even if the underlying entity changes later).
        public string Message { get; set; } = null!;

        // Nullable FK — many notifications relate to a specific exam, but
        // not all do. Generic "welcome" notifications, for instance, would
        // leave this null. Nullable FK = optional relationship.
        public int? RelatedExamId { get; set; }

        // Default false; set true when the user views the notification.
        public bool IsRead { get; set; }

        public DateTime CreatedAt { get; set; }               // UTC

        // ───────────── NAVIGATION ─────────────
        public User User { get; set; } = null!;

        // Nullable navigation paired with the nullable FK.
        public Exam? RelatedExam { get; set; }
    }
}