namespace ExamPlatform.Domain.Enums
{
    // The state of a single candidate's attempt at an exam.
    // Distinguishes a manual submission from an automatic one (time-up),
    // which matters for analytics ("how many ran out of time?") and for
    // the resume-attempt logic (only InProgress attempts can be resumed).
    public enum AttemptStatus
    {
        InProgress,    // Started, timer running, not yet submitted. Resumable.
        Submitted,     // Candidate clicked Submit before time ran out.
        AutoSubmitted  // Server auto-submitted because the timer hit zero.
    }
}