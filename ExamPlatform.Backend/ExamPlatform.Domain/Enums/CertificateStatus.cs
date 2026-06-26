namespace ExamPlatform.Domain.Enums
{
    // Certificate lifecycle. Note we model revocation as a STATUS change,
    // never a row deletion — business rule 29/30 says revoked certificates
    // stay in the DB for auditability; only their downloadability changes.
    public enum CertificateStatus
    {
        Issued,   // Active, valid, downloadable by the candidate.
        Revoked   // Invalidated by an admin; retained in DB but not downloadable.
    }
}