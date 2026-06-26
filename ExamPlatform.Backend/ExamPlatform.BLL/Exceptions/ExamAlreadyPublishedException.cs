namespace ExamPlatform.BLL.Exceptions
{
    // Thrown when the examiner tries to publish an exam that is already Published
    // (or in a later status), or tries to publish results on an exam whose results
    // are already published. Idempotency protection.
    // Maps to HTTP 409 Conflict (wrong state for the requested transition).
    public class ExamAlreadyPublishedException : BusinessException
    {
        public ExamAlreadyPublishedException()
            : base("The exam is already published.") { }

        public ExamAlreadyPublishedException(string message)
            : base(message) { }
    }
}
