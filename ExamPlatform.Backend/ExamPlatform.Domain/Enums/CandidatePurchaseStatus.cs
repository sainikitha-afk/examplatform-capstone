namespace ExamPlatform.Domain.Enums
{
    // A candidate's access grant to one premium exam. Mirrors CertificateStatus's
    // revoke-not-delete pattern: a simulated refund flips this to Refunded (attempt
    // access revoked) rather than deleting the audit row.
    public enum CandidatePurchaseStatus
    {
        Active,
        Refunded
    }
}
