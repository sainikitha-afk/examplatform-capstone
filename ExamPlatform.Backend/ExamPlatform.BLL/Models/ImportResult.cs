// ImportResult — returned by ImportFromExcelAsync to summarise a bulk Excel import.
// Unlike BulkCreateAsync (which throws on the first invalid row), the import service
// uses continue-on-error semantics: every row is attempted, errors are collected,
// and the caller sees totals + a per-row error list.
namespace ExamPlatform.BLL.Models
{
    public class ImportResult
    {
        // Number of rows that passed validation and were persisted.
        public int SuccessCount { get; set; }

        // Number of rows that failed validation (not persisted).
        public int FailureCount { get; set; }

        // Per-row human-readable error strings in the format "Row N: <reason>".
        // Populated for every row that failed; empty when all rows succeeded.
        public List<string> Errors { get; set; } = new();

        // Database-assigned QuestionIds of the questions that were created.
        // Useful for the client to build links or reload the question bank.
        public List<int> CreatedQuestionIds { get; set; } = new();
    }
}
