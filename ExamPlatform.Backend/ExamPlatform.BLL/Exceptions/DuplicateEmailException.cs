namespace ExamPlatform.BLL.Exceptions
{
    // Thrown when registration is attempted with an email already in use.
    // Maps to HTTP 409 Conflict in the global middleware.
    public class DuplicateEmailException : BusinessException
    {
        public DuplicateEmailException()
            : base("An account with this email already exists.") { }

        public DuplicateEmailException(string message) : base(message) { }
    }
}