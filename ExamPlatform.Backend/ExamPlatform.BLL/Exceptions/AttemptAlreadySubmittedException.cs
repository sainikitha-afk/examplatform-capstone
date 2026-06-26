namespace ExamPlatform.BLL.Exceptions
{
    // Thrown when a candidate tries to submit an attempt that is already in
    // Submitted or AutoSubmitted status. Prevents double-submission.
    // Maps to HTTP 409 Conflict.
    public class AttemptAlreadySubmittedException : BusinessException
    {
        public AttemptAlreadySubmittedException()
            : base("This attempt has already been submitted.") { }

        public AttemptAlreadySubmittedException(int attemptId)
            : base($"Attempt {attemptId} has already been submitted.") { }
    }
}
