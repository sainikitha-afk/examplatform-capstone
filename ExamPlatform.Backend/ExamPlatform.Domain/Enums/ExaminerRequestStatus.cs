namespace ExamPlatform.Domain.Enums
{
    // Tracks the lifecycle of an ExaminerRequest from the moment a user
    // submits it to the moment an admin processes it.
    // Stored as a string in the DB (HasConversion<string>() in config)
    // so the column is human-readable and immune to enum reordering.
    public enum ExaminerRequestStatus
    {
        Pending,    // Submitted, waiting for admin review.
        Approved,   // Admin approved; examiner account has been created.
        Rejected    // Admin rejected; the requester was notified via email.
    }
}
