namespace ExamPlatform.Domain.Enums
{
    // Lifecycle of a candidate-raised issue report (flag / help request).
    // Open      → just submitted, waiting for the examiner to look at it.
    // Reviewed  → the examiner has seen it (acknowledged) but no fix needed.
    // Resolved  → the examiner considers it handled (fixed / escalated / dismissed).
    public enum IssueStatus
    {
        Open,
        Reviewed,
        Resolved
    }
}
