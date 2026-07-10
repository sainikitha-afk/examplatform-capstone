using ExamPlatform.Domain.Entities;

namespace ExamPlatform.DAL.Interfaces
{
    // IIssueReportRepository provides examiner-scoped and candidate-scoped
    // access to candidate-raised issue reports.
    public interface IIssueReportRepository : IRepository<int, IssueReport>
    {
        // All reports raised against exams owned by a given examiner, newest first.
        // Includes Candidate, Exam and Question so the examiner sees context.
        Task<(IEnumerable<IssueReport> Items, int TotalCount)> GetForExaminerPagedAsync(
            int examinerId, int page, int pageSize, bool openOnly);

        // Count of still-open reports for an examiner (badge / summary).
        Task<int> OpenCountForExaminerAsync(int examinerId);

        // A single report loaded with Candidate/Exam/Question, used before an
        // examiner resolves it (also lets the service verify exam ownership).
        Task<IssueReport?> GetByIdWithContextAsync(int issueReportId);
    }
}
