using ExamPlatform.Domain.Entities;

namespace ExamPlatform.DAL.Interfaces
{
    // IAttemptAnswerRepository handles per-question answer rows for an attempt.
    public interface IAttemptAnswerRepository : IRepository<int, AttemptAnswer>
    {
        // Auto-save / update an answer. If a row exists for (attemptId, questionId)
        // it is updated; otherwise a new row is inserted. Called on every answer
        // change so the candidate's progress is persisted even if they close the tab.
        // selectedOptionId is nullable — null means the candidate skipped the question.
        Task UpsertAsync(int attemptId, int questionId, int? selectedOptionId);

        // Returns all answer rows for an attempt, with the Question and
        // SelectedOption navigation properties eagerly loaded.
        // Used during submission evaluation and the result-view breakdown.
        Task<IEnumerable<AttemptAnswer>> GetByAttemptAsync(int attemptId);
    }
}