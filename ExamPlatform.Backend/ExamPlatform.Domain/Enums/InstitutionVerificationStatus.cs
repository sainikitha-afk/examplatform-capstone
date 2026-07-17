namespace ExamPlatform.Domain.Enums
{
    // Lifecycle of an examiner's Institute Verification request, mirroring
    // ExaminerRequestStatus's Pending/decision shape.
    public enum InstitutionVerificationStatus
    {
        Pending,
        Verified,
        Rejected
    }
}
