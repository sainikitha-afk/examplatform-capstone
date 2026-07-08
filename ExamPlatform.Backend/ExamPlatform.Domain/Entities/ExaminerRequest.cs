using ExamPlatform.Domain.Enums;

namespace ExamPlatform.Domain.Entities
{
    // ExaminerRequest captures a self-service application from someone who
    // wants to become an examiner on the platform. An anonymous user fills
    // in their name, email, and purpose; the record sits as Pending until
    // an admin approves or rejects it. On approval, a real User account
    // (Role = Examiner) is created and credentials are emailed.
    //
    // WHY a separate table and not a User row with IsActive=false?
    //   Because the request has its own lifecycle (Pending/Approved/Rejected)
    //   and fields (Purpose, AdminNote) that are meaningless on a User.
    //   Mixing them would violate Single Responsibility and pollute the
    //   Users table with partially-filled rows.
    public class ExaminerRequest
    {
        // Primary key — auto-incremented by PostgreSQL.
        public int ExaminerRequestId { get; set; }

        // The applicant's name as they want it to appear on the platform.
        public string FullName { get; set; } = null!;

        // Contact email. Used to:
        //   a) Notify the admin who will review the request.
        //   b) Send credentials when the account is approved.
        //   c) Prevent duplicate applications from the same address.
        public string Email { get; set; } = null!;

        // Free-text: "I teach Java at XYZ University and want to create
        // coding assessments." Helps admin decide whether to approve.
        public string Purpose { get; set; } = null!;

        // Lifecycle state: Pending → Approved / Rejected.
        // Stored as a string via HasConversion<string>() in configuration.
        public ExaminerRequestStatus Status { get; set; } = ExaminerRequestStatus.Pending;

        // UTC timestamp of when the form was submitted.
        public DateTime RequestedAt { get; set; } = DateTime.UtcNow;

        // UTC timestamp of when the admin took action (null while Pending).
        public DateTime? ProcessedAt { get; set; }

        // Optional note the admin can attach (e.g., reason for rejection,
        // or a welcome note). Surfaced in the admin response DTO.
        public string? AdminNote { get; set; }
    }
}
