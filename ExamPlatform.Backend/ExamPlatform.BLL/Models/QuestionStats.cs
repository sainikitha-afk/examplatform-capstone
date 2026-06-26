// Question bank statistics value objects returned by IQuestionService.GetStatsAsync.
// BLL-layer models (not DTOs, not entities) — the API layer maps these to response DTOs.

namespace ExamPlatform.BLL.Models
{
    // Per-category question count, carries the category name for display.
    public record CategoryQuestionCount(int CategoryId, string CategoryName, int Count);

    // Aggregated question stats: difficulty breakdown + per-category counts.
    public record QuestionStats(
        int TotalQuestions,
        int Easy,
        int Medium,
        int Hard,
        IEnumerable<CategoryQuestionCount> ByCategory);
}
