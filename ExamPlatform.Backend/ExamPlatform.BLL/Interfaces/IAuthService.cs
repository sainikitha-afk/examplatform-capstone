using ExamPlatform.Domain.Entities;

namespace ExamPlatform.BLL.Interfaces
{
    public interface IAuthService
    {
        // Self-registration — always creates a Candidate.
        Task<User> RegisterAsync(string fullName, string username, string email, string password);

        // Admin-only — creates an Examiner account.
        // Same validation as Register; only the role differs.
        Task<User> CreateExaminerAsync(string fullName, string username, string email, string password);

        Task<User> LoginAsync(string emailOrUsername, string password);

        // Authenticated self-service password change (any role). userId comes
        // from the caller's JWT; the current password is re-verified before
        // the new one is accepted. Completes the examiner temp-password flow.
        Task ChangePasswordAsync(int userId, string currentPassword, string newPassword);
    }
}