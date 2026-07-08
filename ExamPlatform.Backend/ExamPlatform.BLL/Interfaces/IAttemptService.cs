using ExamPlatform.BLL.Models;
using ExamPlatform.Domain.Entities;

namespace ExamPlatform.BLL.Interfaces
{
    // Contract for the candidate exam-taking workflow.
    public interface IAttemptService
    {
        // Starts a new attempt OR resumes an existing InProgress one.
        // The returned Attempt has the Exam (with questions + options) loaded
        // so the controller can build the AttemptResumeDto.
        Task<Attempt> StartOrResumeAsync(int userId, int examId);

        // Returns an InProgress attempt for the resume view.
        // Also auto-submits if the server-side timer has expired.
        Task<Attempt> GetByIdAsync(int attemptId, int userId);

        // Upserts an answer for one question (auto-save).
        // Also checks timer expiry — auto-submits if time is up.
        Task SaveAnswerAsync(int attemptId, int questionId, int? selectedOptionId, int userId);

        // Manual submit triggered by the candidate.
        Task<Attempt> SubmitAsync(int attemptId, int userId);

        // Returns the candidate's full attempt history.
        Task<IEnumerable<Attempt>> GetByUserAsync(int userId);

        // Returns a paginated page of the candidate's attempt history.
        Task<PagedResult<Attempt>> GetByUserPagedAsync(int userId, int page, int pageSize);

        // Returns the detailed result for one attempt.
        // Only available once the exam is in ResultsPublished status.
        Task<Attempt> GetResultAsync(int attemptId, int userId);

        // Returns every attempt (all statuses) for an exam — the examiner
        // monitoring view. Examiners may only view their own exams; admins any.
        Task<IEnumerable<Attempt>> GetAllByExamForExaminerAsync(
            int examId, int requesterId, bool isAdmin);
    }
}
