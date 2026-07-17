using ExamPlatform.Domain.Entities;

namespace ExamPlatform.BLL.Interfaces
{
    // Service contract for exam category management.
    //
    // Returns ExamCategory entities. The controller maps them to DTOs.
    // This keeps BLL framework-agnostic — it knows nothing about DTOs or HTTP.
    public interface IExamCategoryService
    {
        // ───────────── READ ─────────────
        Task<IEnumerable<ExamCategory>> GetAllAsync();
        Task<ExamCategory> GetByIdAsync(int catId);   // throws CategoryNotFoundException if missing

        // Same list as GetAllAsync, paired with each category's published-exam
        // count. Powers the "category board" cards on Browse Exams (candidate)
        // and the examiner's category-browse view — one query, no N+1.
        Task<IEnumerable<(ExamCategory Category, int PublishedExamCount)>> GetAllWithExamCountsAsync();

        // Returns the special "Mixed" category, creating it on first use.
        // Used when an examiner labels an exam as multi-category — the exam's
        // CatId FK is non-nullable, so "Mixed" is a real category row that is
        // resolved by NAME (never a hardcoded id) so it works on any database.
        Task<ExamCategory> GetOrCreateMixedAsync();

        // ───────────── WRITE ─────────────
        Task<ExamCategory> CreateAsync(string catName, string? description);

        // Creates multiple categories in a single atomic operation.
        // All names are validated (required, length) and checked for uniqueness
        // against existing categories AND within the batch before any insert.
        // If any entry fails, the entire batch is rejected.
        Task<IEnumerable<ExamCategory>> BulkCreateAsync(
            IEnumerable<(string CatName, string? Description)> categories);

        Task<ExamCategory> UpdateAsync(int catId, string catName, string? description);
        Task DeleteAsync(int catId);
    }
}

/*
GetByIdAsync throws instead of returning null. Service-layer contract: "if I say GetByIdAsync, I either return a valid entity or throw." 
Forces the controller to either get a valid one OR a typed exception that the middleware can turn into a 404. No if (cat == null) return 
NotFound() repeated in every controller.
No ExamCategoryDto in the interface signature. Services traffic in entities. DTOs are an API-layer concern — the controller adapts entities
to DTOs.
*/