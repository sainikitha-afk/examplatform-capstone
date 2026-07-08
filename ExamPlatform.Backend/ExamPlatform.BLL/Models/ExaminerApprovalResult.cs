using ExamPlatform.Domain.Entities;

namespace ExamPlatform.BLL.Models
{
    // Returned by IExaminerRequestService.ApproveAsync.
    // Bundles the updated request, the newly created User, and the
    // auto-generated temporary password so the API layer can surface
    // the credentials to the admin (in case the email wasn't delivered
    // because the SMTP host is unconfigured — NoOp mode).
    public class ExaminerApprovalResult
    {
        public ExaminerRequest Request { get; set; } = null!;
        public User CreatedUser { get; set; } = null!;
        // Plain-text temp password — only exists in this result object
        // long enough to be returned in the HTTP response. It is NOT
        // persisted anywhere beyond the BCrypt hash stored on the User.
        public string TemporaryPassword { get; set; } = null!;
    }
}
