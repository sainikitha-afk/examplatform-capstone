namespace ExamPlatform.Domain.Enums
{
    // Categorizes a notification so the frontend can render the right icon/text
    // and so we can filter ("show me all certificate notifications").
    public enum NotificationType
    {
        ResultPublished,    // "Your result for <exam> is now available."
        CertificateIssued,  // "You earned a certificate for <exam>."
        CertificateRevoked  // "Your certificate for <exam> has been revoked."
    }
}