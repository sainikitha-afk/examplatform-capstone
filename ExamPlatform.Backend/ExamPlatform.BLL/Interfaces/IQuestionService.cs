using ExamPlatform.BLL.Models;
using ExamPlatform.Domain.Entities;
using ExamPlatform.Domain.Enums;

namespace ExamPlatform.BLL.Interfaces
{
    // Contract for the Question feature. Services return domain entities or BLL
    // value objects — the API layer is responsible for mapping them to DTOs.
    public interface IQuestionService
    {
        // Returns visible questions, optionally narrowed by category/difficulty/text.
        // Filtering is pushed to the database (not in-memory).
        // Admins see every question; Examiners see only their own active questions.
        Task<IEnumerable<Question>> GetAllAsync(
            int requesterId,
            UserRole requesterRole,
            int? categoryId = null,
            string? difficulty = null,
            string? search = null);

        // Paginated + filtered variant of GetAllAsync.
        // pageSize is clamped to [1, 100] inside the implementation.
        Task<PagedResult<Question>> GetAllPagedAsync(
            int requesterId,
            UserRole requesterRole,
            int page,
            int pageSize,
            int? categoryId = null,
            string? difficulty = null,
            string? search = null);

        // Returns a single question with its options loaded. Enforces
        // ownership for Examiner callers (they cannot view other examiners' questions).
        Task<Question> GetByIdWithOptionsAsync(int questionId, int requesterId, UserRole requesterRole);

        // Creates a new Question with exactly 4 options (MCQ rules enforced).
        // Duplicate check: same ExaminerId + CatId + QuestionText → 409 DuplicateQuestionException.
        Task<Question> CreateAsync(
            int examinerId,
            int catId,
            string questionText,
            decimal marks,
            string difficulty,
            IEnumerable<(string OptionText, bool IsCorrect)> options);

        // Replaces the question's text/marks/difficulty AND replaces all 4 options atomically.
        // requesterRole: Admin may update any question; Examiner only their own.
        Task<Question> UpdateAsync(
            int questionId,
            int requesterId,
            UserRole requesterRole,
            string questionText,
            decimal marks,
            string difficulty,
            IEnumerable<(string OptionText, bool IsCorrect)> options);

        // Soft-deletes the question (IsActive = false).
        // Fails if the question is used in a Published exam.
        // requesterRole: Admin may delete any question; Examiner only their own.
        Task SoftDeleteAsync(int questionId, int requesterId, UserRole requesterRole);

        // Creates multiple questions in a single transactional request.
        // Phase 1: validates ALL questions + duplicate detection before any DB ops.
        // Phase 2: transactional insert (all-or-nothing via explicit transaction).
        // Used by both POST /api/questions/bulk and the Excel import controller.
        Task<IEnumerable<Question>> BulkCreateAsync(
            int examinerId,
            IEnumerable<(int CatId, string QuestionText, decimal Marks, string Difficulty, IEnumerable<(string OptionText, bool IsCorrect)> Options)> questions);

        // Returns question bank statistics (counts by difficulty + by category).
        // Examiner: scoped to their own active questions.
        // Admin: platform-wide active question counts.
        Task<QuestionStats> GetStatsAsync(int requesterId, UserRole requesterRole);

        // Imports questions from an Excel stream.
        // Uses continue-on-error semantics: each row is attempted independently.
        // Per-row failures are collected into ImportResult.Errors (format: "Row N: reason").
        // Succeeds even if some rows fail — the caller inspects SuccessCount/FailureCount.
        // Excel column layout: see ImportFromExcelAsync XML doc in QuestionService.cs.
        Task<ImportResult> ImportFromExcelAsync(int examinerId, Stream excelStream);
    }
}
