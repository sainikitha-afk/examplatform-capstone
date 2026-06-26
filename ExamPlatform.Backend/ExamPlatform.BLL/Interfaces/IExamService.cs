using ExamPlatform.BLL.Models;
using ExamPlatform.Domain.Entities;

namespace ExamPlatform.BLL.Interfaces
{
    // Contract for the Exam feature lifecycle: creation → editing → publishing →
    // result publishing. Services return domain entities; the API maps to DTOs.
    public interface IExamService
    {
        // Candidates and any authenticated user browse published exams.
        // categoryId filters by category (optional — null means all).
        Task<IEnumerable<Exam>> GetPublishedAsync(int? categoryId);

        // Paginated browse of published exams.
        Task<PagedResult<Exam>> GetPublishedPagedAsync(int? categoryId, int page, int pageSize);

        // Examiner's "My Exams" — all statuses, scoped to their userId.
        Task<IEnumerable<Exam>> GetMineAsync(int examinerId);

        // Paginated "My Exams" for an examiner.
        Task<PagedResult<Exam>> GetMinePagedAsync(int examinerId, int page, int pageSize);

        // Loads an exam with its full question+option tree.
        // The controller decides which DTO to use based on the caller's role.
        Task<Exam> GetByIdWithQuestionsAndOptionsAsync(int examId);

        // Creates a new exam in Draft status.
        Task<Exam> CreateAsync(
            int examinerId, int catId, string title, string? instructions,
            int durationMinutes, decimal totalMarks, decimal passingMarks,
            DateTime startTime, DateTime endTime);

        // Updates exam metadata. Only allowed while exam is in Draft.
        Task<Exam> UpdateAsync(
            int examId, int examinerId, string title, string? instructions,
            int durationMinutes, decimal totalMarks, decimal passingMarks,
            DateTime startTime, DateTime endTime);

        // Hard-deletes the exam. Only allowed while exam is in Draft.
        Task DeleteAsync(int examId, int examinerId);

        // Attaches a list of questions to the exam (bulk). Draft only.
        Task<Exam> AttachQuestionsAsync(int examId, IEnumerable<int> questionIds, int examinerId);

        // Detaches a single question from the exam. Draft only.
        Task<Exam> DetachQuestionAsync(int examId, int questionId, int examinerId);

        // Validates and transitions exam from Draft → Published.
        Task<Exam> PublishAsync(int examId, int examinerId);

        // Transitions Published → ResultsPublished, generates certificates,
        // and fans out notifications. Runs inside an explicit DB transaction.
        Task<Exam> PublishResultsAsync(int examId, int examinerId);

        // Administrative rollback: Published → Draft.
        // Only allowed when no attempts exist for the exam.
        // isAdmin=true bypasses the ownership check (Admin role can act on any exam).
        Task<Exam> UnpublishAsync(int examId, int callerId, bool isAdmin);
    }
}
