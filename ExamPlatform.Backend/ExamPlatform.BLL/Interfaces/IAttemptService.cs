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

        // Records that the candidate left the exam tab/window during an
        // InProgress attempt (proctoring). Increments the attempt's counter and,
        // once the warning threshold is crossed, notifies the examiner. Returns
        // the new total count. No-op (returns current count) if not InProgress.
        Task<int> RecordTabSwitchAsync(int attemptId, int userId);

        // Returns the candidate's full attempt history.
        Task<IEnumerable<Attempt>> GetByUserAsync(int userId);

        // How many completed (Submitted/AutoSubmitted) attempts the user has for
        // this exam — the details page shows it against Exam.MaxAttempts.
        Task<int> CountCompletedAttemptsAsync(int userId, int examId);

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
