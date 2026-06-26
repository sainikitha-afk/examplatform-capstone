// ImportResultDto — HTTP response body for POST /api/questions/import.
// Maps 1-to-1 from BLL.Models.ImportResult.
namespace ExamPlatform.API.DTOs.Question
{
    public class ImportResultDto
    {
        // Number of rows that were successfully parsed, validated, and saved.
        public int SuccessCount { get; set; }

        // Number of rows that failed (not persisted).
        public int FailureCount { get; set; }

        // Per-row failure messages in the format "Row N: <reason>".
        // Empty list when all rows succeeded.
        public List<string> Errors { get; set; } = new();

        // Database-generated QuestionIds of the successfully created questions.
        // The client can use these to reload or link to the new questions.
        public List<int> CreatedQuestionIds { get; set; } = new();
    }
}
