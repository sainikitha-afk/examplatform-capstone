namespace ExamPlatform.BLL.Exceptions
{
    // Thrown when an attempt id doesn't exist or doesn't belong to the requesting
    // candidate. Same exception for both cases (no information leak).
    // Maps to HTTP 404 Not Found.
    public class AttemptNotFoundException : BusinessException
    {
        public AttemptNotFoundException()
            : base("Attempt not found.") { }

        public AttemptNotFoundException(int attemptId)
            : base($"Attempt with id {attemptId} not found.") { }

        public AttemptNotFoundException(string message)
            : base(message) { }
    }
}
