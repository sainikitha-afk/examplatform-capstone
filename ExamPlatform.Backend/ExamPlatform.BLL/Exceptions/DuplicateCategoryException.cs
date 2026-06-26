namespace ExamPlatform.BLL.Exceptions
{
    // Thrown when creating/renaming a category to a name that already exists.
    // Maps to HTTP 409 in the global middleware. Add a case for this exception
    // in GlobalExceptionMiddleware (alongside DuplicateEmail/Username) → 409.
    public class DuplicateCategoryException : BusinessException
    {
        public DuplicateCategoryException(string name)
            : base($"A category named '{name}' already exists.") { }
    }
}