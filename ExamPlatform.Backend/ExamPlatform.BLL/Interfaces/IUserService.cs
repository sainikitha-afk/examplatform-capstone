using ExamPlatform.BLL.Models;
using ExamPlatform.Domain.Entities;

namespace ExamPlatform.BLL.Interfaces
{
    public interface IUserService
    {
        // For GET /api/users/me — current user's own profile.
        Task<User> GetByIdAsync(int userId);

        // Admin: list all users.
        Task<IEnumerable<User>> GetAllAsync();

        // Admin: paginated list of all users.
        // pageSize is clamped to [1, 100] inside the service implementation.
        Task<PagedResult<User>> GetAllPagedAsync(int page, int pageSize);

        // Admin: toggle active status.
        Task<User> SetActiveStatusAsync(int userId, bool isActive);
    }
}