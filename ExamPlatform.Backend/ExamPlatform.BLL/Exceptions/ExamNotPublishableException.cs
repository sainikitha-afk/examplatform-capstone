namespace ExamPlatform.BLL.Exceptions
{
    // Thrown when the examiner tries to publish an exam that fails one or more
    // publish pre-conditions (e.g., no questions, marks mismatch, invalid schedule).
    // Carries a human-readable reason so the examiner knows exactly what to fix.
    // Maps to HTTP 400 Bad Request (invalid state for the requested transition).
    public class ExamNotPublishableException : BusinessException
    {
        // 'reason' explains which specific validation rule failed.
        public ExamNotPublishableException(string reason)
            : base($"Exam cannot be published: {reason}") { }
    }
}
