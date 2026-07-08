namespace ExamPlatform.Domain.Enums
{
    // Categorizes a notification so the frontend can render the right icon/text
    // and so we can filter ("show me all certificate notifications").
    public enum NotificationType
    {
        ResultPublished,            // "Your result for <exam> is now available."
        CertificateIssued,          // "You earned a certificate for <exam>."
        CertificateRevoked,         // "Your certificate for <exam> has been revoked."
        ExaminerRequestReceived,    // Admin inbox: "New examiner request from <name>."
        ExaminerAccountApproved,    // New examiner: "Your examiner account is ready. Check your email for credentials."
        AttemptResumed,             // Examiner alert: "<candidate> resumed their attempt for <exam> (possible disconnection)."
        AttemptAutoSubmitted        // Examiner alert: "<candidate>'s attempt for <exam> was auto-submitted when time expired."
    }
}