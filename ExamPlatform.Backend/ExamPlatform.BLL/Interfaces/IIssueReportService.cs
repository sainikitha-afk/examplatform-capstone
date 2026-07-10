using ExamPlatform.BLL.Models;
using ExamPlatform.Domain.Entities;
using ExamPlatform.Domain.Enums;

namespace ExamPlatform.BLL.Interfaces
{
    // Contract for candidate-raised issue reports (the in-exam "flag / report a
    // problem" feature). Candidates raise reports; examiners triage them.
    public interface IIssueReportService
    {
        // Candidate raises a report against an exam. questionId is optional
        // (null = general experience/technical issue). Immediately creates an
        // in-app notification for the exam's examiner. Returns the saved report.
        Task<IssueReport> RaiseAsync(
            int candidateId, int examId, int? attemptId, int? questionId, string message);

        // Examiner inbox: reports for exams this examiner owns, newest first.
        Task<PagedResult<IssueReport>> GetForExaminerPagedAsync(
            int examinerId, int page, int pageSize, bool openOnly);

        // Count of still-open reports for the examiner (summary / badge).
        Task<int> OpenCountForExaminerAsync(int examinerId);

        // Examiner updates a report's status (Reviewed / Resolved), optionally
        // with a note. On Resolved, notifies the candidate. Enforces ownership.
        Task<IssueReport> UpdateStatusAsync(
            int issueReportId, int examinerId, IssueStatus status, string? resolutionNote);
    }
}
