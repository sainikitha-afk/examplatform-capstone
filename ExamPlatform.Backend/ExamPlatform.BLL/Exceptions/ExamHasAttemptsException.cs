namespace ExamPlatform.BLL.Exceptions
{
    // Thrown when the examiner tries to unpublish an exam that already has one or
    // more candidate attempts (any status: InProgress, Submitted, AutoSubmitted).
    // Reverting in this situation would corrupt attempt records that reference the
    // exam's Published state, so the operation is rejected.
    // Maps to HTTP 409 Conflict — the resource state prevents the operation.
    public class ExamHasAttemptsException : BusinessException
    {
        public ExamHasAttemptsException()
            : base("Cannot unpublish exam because candidates have already started attempts.") { }

        public ExamHasAttemptsException(string message)
            : base(message) { }
    }
}
