namespace ExamPlatform.BLL.Exceptions
{
    // Thrown when an examiner tries to soft-delete a question that is already
    // attached to at least one Published exam. Deleting it would corrupt
    // the live exam's question set. Maps to HTTP 409 Conflict.
    public class QuestionInUseException : BusinessException
    {
        public QuestionInUseException()
            : base("This question is currently used in a published exam and cannot be deleted.") { }

        public QuestionInUseException(int questionId)
            : base($"Question {questionId} is currently used in a published exam and cannot be deleted.") { }

        public QuestionInUseException(string message)
            : base(message) { }
    }
}
