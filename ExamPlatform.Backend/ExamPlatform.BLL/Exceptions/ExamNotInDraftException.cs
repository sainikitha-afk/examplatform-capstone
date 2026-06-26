namespace ExamPlatform.BLL.Exceptions
{
    // Thrown when an examiner tries to edit, delete, or attach questions to an
    // exam that is no longer in Draft status (e.g., it has already been Published).
    // Business rule: once published, the exam's structure is locked.
    // Maps to HTTP 409 Conflict (wrong state).
    public class ExamNotInDraftException : BusinessException
    {
        public ExamNotInDraftException()
            : base("This operation is only allowed while the exam is in Draft status.") { }

        public ExamNotInDraftException(int examId)
            : base($"Exam {examId} is not in Draft status. Only Draft exams can be modified.") { }

        public ExamNotInDraftException(string message)
            : base(message) { }
    }
}
