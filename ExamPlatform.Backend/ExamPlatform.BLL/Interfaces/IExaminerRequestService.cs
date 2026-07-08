using ExamPlatform.BLL.Models;
using ExamPlatform.Domain.Entities;

namespace ExamPlatform.BLL.Interfaces
{
    // Business contract for the Examiner Request workflow.
    //
    // Lifecycle:
    //   Anonymous user → SubmitAsync → [admin sees notification] →
    //   admin calls ApproveAsync or RejectAsync.
    public interface IExaminerRequestService
    {
        // ── SUBMIT (public, no auth required) ───────────────────────────
        // Creates a new ExaminerRequest and fans-out a notification to
        // every admin. Throws ValidationException on bad input.
        // Throws DuplicateEmailException when a Pending/Approved request
        // already exists for the given email.
        Task<ExaminerRequest> SubmitAsync(string fullName, string email, string purpose);

        // ── ADMIN READ ──────────────────────────────────────────────────
        Task<IEnumerable<ExaminerRequest>> GetAllAsync();
        Task<IEnumerable<ExaminerRequest>> GetPendingAsync();
        Task<ExaminerRequest> GetByIdAsync(int requestId);

        // ── ADMIN ACTIONS ───────────────────────────────────────────────
        // Approve: creates an Examiner User account, sends credentials via
        // email, posts a welcome notification to the new account.
        // Returns ExaminerApprovalResult so the controller can echo the
        // temp password back to the admin (useful when SMTP is not set up).
        Task<ExaminerApprovalResult> ApproveAsync(int requestId);

        // Reject: marks the request as Rejected and optionally attaches
        // an admin note. Sends a rejection email to the requester.
        Task<ExaminerRequest> RejectAsync(int requestId, string? adminNote);
    }
}
