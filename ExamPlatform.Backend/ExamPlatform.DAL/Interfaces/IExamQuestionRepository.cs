using ExamPlatform.Domain.Entities;

namespace ExamPlatform.DAL.Interfaces
{
    // IExamQuestionRepository manages the N-N bridge table between Exam and Question.
    public interface IExamQuestionRepository : IRepository<int, ExamQuestion>
    {
        // Returns all bridge rows for a given exam, with the Question nav loaded.
        // Used when building the exam's question list and for ordering display.
        Task<IEnumerable<ExamQuestion>> GetByExamIdAsync(int examId);

        // Checks whether a specific question is already attached to an exam.
        // Prevents duplicates (DB UNIQUE constraint backs this up, but we check
        // first to give a meaningful error instead of a constraint-violation exception).
        Task<bool> ExistsAsync(int examId, int questionId);

        // Hard-deletes a single bridge row to detach one question from an exam.
        // Only legal while the exam is in Draft status (enforced by the service).
        Task DeleteByExamAndQuestionAsync(int examId, int questionId);
    }
}