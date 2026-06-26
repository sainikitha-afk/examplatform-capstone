namespace ExamPlatform.BLL.Exceptions
{
    // Thrown when a service method receives a questionId that doesn't exist
    // (or exists but belongs to a different examiner — we return the same
    // exception to avoid leaking information about other examiners' questions).
    // Maps to HTTP 404 Not Found in GlobalExceptionMiddleware.
    public class QuestionNotFoundException : BusinessException
    {
        public QuestionNotFoundException()
            : base("Question not found.") { }

        // Convenience constructor: builds the message for you so callers
        // can simply throw new QuestionNotFoundException(questionId).
        public QuestionNotFoundException(int questionId)
            : base($"Question with id {questionId} not found.") { }

        public QuestionNotFoundException(string message)
            : base(message) { }
    }
}
