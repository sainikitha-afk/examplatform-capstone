using ExamPlatform.Domain.Entities;

namespace ExamPlatform.DAL.Interfaces
{
    // IQuestionRepository inherits the generic CRUD contract and adds
    // Question-specific queries needed by QuestionService.
    public interface IQuestionRepository : IRepository<int, Question>
    {
        // Loads a question together with its 4 options in one SQL JOIN query.
        // Without this, accessing question.Options after GetByIdAsync would
        // lazy-load (N+1) or throw if lazy loading is off.
        Task<Question?> GetByIdWithOptionsAsync(int questionId);

        // Returns only the questions authored by a specific examiner.
        // Examiners may only see (and manage) their own questions.
        Task<IEnumerable<Question>> GetByExaminerAsync(int examinerId);

        // Checks whether a question is referenced by at least one exam that
        // is currently Published. Used by the soft-delete guard: a question
        // in a live exam cannot be removed from the question bank.
        Task<bool> IsUsedInPublishedExamAsync(int questionId);

        // Duplicate detection: returns true when the examiner already owns
        // an ACTIVE question with the same text (case-insensitive, trimmed)
        // in the same category. Used by CreateAsync and BulkCreateAsync.
        Task<bool> IsDuplicateAsync(int examinerId, int catId, string questionText);

        // Server-side filtering for GET /api/questions with optional query params.
        // examinerId null → Admin scope (all questions, including inactive).
        // examinerId non-null → Examiner scope (their active questions only).
        // All other params narrow the result set; null = no filter for that param.
        Task<IEnumerable<Question>> GetFilteredAsync(
            int? examinerId,
            int? categoryId,
            string? difficulty,
            string? search);

        // Loads active questions WITH the Category navigation property eagerly
        // loaded. Used by GetStatsAsync to compute per-category counts without
        // a separate round-trip per question (avoids N+1 on Category lookup).
        // examinerId null → Admin (all active questions); non-null → Examiner scope.
        Task<IEnumerable<Question>> GetForStatsAsync(int? examinerId);

        // Paginated variant of GetFilteredAsync — same WHERE clauses but with
        // Skip/Take so the DB returns only one page of results at a time.
        Task<(IEnumerable<Question> Items, int TotalCount)> GetFilteredPagedAsync(
            int? examinerId,
            int? categoryId,
            string? difficulty,
            string? search,
            int page,
            int pageSize);

        // True if at least one question (active or soft-deleted — the FK exists
        // on every row regardless of IsActive) references this category. Used
        // alongside IExamRepository.AnyByCategoryAsync by the category-delete
        // guard.
        Task<bool> AnyByCategoryAsync(int catId);
    }
}
