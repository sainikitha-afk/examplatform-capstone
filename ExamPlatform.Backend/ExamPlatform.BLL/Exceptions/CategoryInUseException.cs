namespace ExamPlatform.BLL.Exceptions
{
    // Thrown when an Admin tries to delete a category that is still referenced
    // by at least one exam or question. The FK is DeleteBehavior.Restrict, so
    // the DB would reject the DELETE anyway — we pre-check and throw this
    // friendly, typed exception instead of letting a raw DbUpdateException
    // surface as an unhandled 500. Maps to HTTP 409 Conflict.
    public class CategoryInUseException : BusinessException
    {
        public CategoryInUseException(string categoryName)
            : base($"Category '{categoryName}' cannot be deleted because it still has exams or " +
                   "questions attached to it. Reassign or remove those first.") { }
    }
}
