using ExamPlatform.Domain.Entities;

namespace ExamPlatform.DAL.Interfaces
{
    // IExamRepository adds exam-specific queries to the generic CRUD contract.
    public interface IExamRepository : IRepository<int, Exam>
    {
        // Candidates browse only Published exams. Optional categoryId enables
        // the ?categoryId=N filter on GET /api/exams.
        Task<IEnumerable<Exam>> GetPublishedAsync(int? categoryId);

        // An examiner's "My Exams" view — all statuses, scoped to their userId.
        Task<IEnumerable<Exam>> GetByExaminerAsync(int examinerId);

        // Loads the exam + the bridge rows + the Question on each bridge row.
        // Used when the examiner views their exam detail or before publishing
        // (to validate TotalMarks == sum of question marks).
        Task<Exam?> GetByIdWithQuestionsAsync(int examId);

        // Full depth: exam → ExamQuestions → Question → QuestionOption.
        // Used when rendering exam questions to candidates (options needed)
        // and when evaluating a submitted attempt (correct-option lookup).
        Task<Exam?> GetByIdWithQuestionsAndOptionsAsync(int examId);

        // Paginated variant of GetPublishedAsync — same WHERE clause but with
        // Skip/Take so the DB returns only one page at a time.
        // Returns a tuple to avoid a BLL dependency in DAL (same pattern as GetPagedAsync).
        Task<(IEnumerable<Exam> Items, int TotalCount)> GetPublishedPagedAsync(
            int? categoryId, int page, int pageSize);

        // Paginated variant of GetByExaminerAsync — scoped to one examiner, with paging.
        Task<(IEnumerable<Exam> Items, int TotalCount)> GetByExaminerPagedAsync(
            int examinerId, int page, int pageSize);
    }
}