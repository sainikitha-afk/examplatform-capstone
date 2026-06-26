using ExamPlatform.Domain.Entities;

namespace ExamPlatform.DAL.Interfaces
{
    // IAttemptRepository adds candidate-workflow queries to generic CRUD.
    public interface IAttemptRepository : IRepository<int, Attempt>
    {
        // Finds an existing InProgress attempt for a (user, exam) pair.
        // Used by StartOrResume: if one exists, return it instead of creating
        // a new attempt — the candidate resumes from where they left off.
        Task<Attempt?> GetInProgressAsync(int userId, int examId);

        // Returns the full attempt history for a candidate (all statuses).
        // Eagerly loads the Exam navigation so the response DTO can include
        // exam title, passing marks, etc. without extra queries.
        Task<IEnumerable<Attempt>> GetByUserAsync(int userId);

        // Returns all Submitted/AutoSubmitted attempts for a given exam.
        // Used during publish-results to find the best attempt per candidate.
        // Eagerly loads the User navigation (for email notification).
        Task<IEnumerable<Attempt>> GetByExamIdAsync(int examId);

        // Loads a single attempt together with its answers, the answered
        // questions, selected options, and the exam (with questions+options).
        // Used for the result view and the resume view.
        Task<Attempt?> GetByIdWithAnswersAndExamAsync(int attemptId);

        // Returns true if ANY attempt (any status) exists for the given exam.
        // Used by UnpublishAsync to block rollback when candidates have touched
        // the exam — even an InProgress attempt must prevent unpublishing.
        Task<bool> HasAnyForExamAsync(int examId);

        // Paginated variant of GetByUserAsync — includes Exam navigation, sorted by
        // StartedAt DESC, then pages the result.
        Task<(IEnumerable<Attempt> Items, int TotalCount)> GetByUserPagedAsync(
            int userId, int page, int pageSize);
    }
}