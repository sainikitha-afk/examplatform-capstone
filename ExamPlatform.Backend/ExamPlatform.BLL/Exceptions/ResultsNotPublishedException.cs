namespace ExamPlatform.BLL.Exceptions
{
    // Thrown when a candidate requests the result for an attempt whose exam
    // has not yet had results published by the examiner.
    // Maps to HTTP 409 Conflict (attempt exists but result isn't available yet).
    public class ResultsNotPublishedException : BusinessException
    {
        public ResultsNotPublishedException()
            : base("Results for this exam have not been published yet.") { }

        public ResultsNotPublishedException(string message)
            : base(message) { }
    }
}
