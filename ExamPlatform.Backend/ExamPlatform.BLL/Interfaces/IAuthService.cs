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
    }
}