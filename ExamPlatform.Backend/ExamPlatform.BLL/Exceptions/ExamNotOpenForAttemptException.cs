namespace ExamPlatform.BLL.Exceptions
{
    // Thrown when a candidate tries to start an attempt on an exam that is not
    // currently open — either not Published, or outside the start/end time window.
    // Maps to HTTP 409 Conflict (valid exam, wrong state for this operation).
    public class ExamNotOpenForAttemptException : BusinessException
    {
        public ExamNotOpenForAttemptException()
            : base("This exam is not currently open for attempts.") { }

        public ExamNotOpenForAttemptException(string reason)
            : base($"Exam is not open for attempts: {reason}") { }
    }
}
