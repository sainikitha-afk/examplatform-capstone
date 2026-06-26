namespace ExamPlatform.BLL.Exceptions
{
    // Thrown when an examiner (or admin) tries to create a question whose text
    // already exists for the same category in the same examiner's active bank.
    // Duplicate rule: same ExaminerId + same CatId + same QuestionText (case-insensitive, trimmed).
    // Maps to HTTP 409 Conflict in GlobalExceptionMiddleware.
    public class DuplicateQuestionException : BusinessException
    {
        public DuplicateQuestionException()
            : base("A question with the same text already exists in this category.") { }

        // Carries the offending text so the error message is actionable.
        public DuplicateQuestionException(string questionText)
            : base($"A question with the same text already exists in this category: '{questionText}'.") { }
    }
}
