namespace ExamPlatform.BLL.Exceptions
{
    // Generic catch-all for input validation failures inside services
    // (e.g., weak password, bad email format, marks out of range).
    // Maps to HTTP 400 Bad Request.
    //
    // We always pass a specific message — there's no generic default
    // because validation failures need to tell the user WHAT was wrong.
    public class ValidationException : BusinessException
    {
        public ValidationException(string message) : base(message) { }
    }
}