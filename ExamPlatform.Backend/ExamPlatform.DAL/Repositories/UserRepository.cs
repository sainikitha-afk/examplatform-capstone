using Microsoft.EntityFrameworkCore;
using ExamPlatform.DAL.Contexts;
using ExamPlatform.DAL.Interfaces;
using ExamPlatform.Domain.Entities;
using ExamPlatform.Domain.Enums;

namespace ExamPlatform.DAL.Repositories
{
    // UserRepository INHERITS the generic AbstractRepository<int, User>
    // (which provides all CRUD) AND implements IUserRepository (which
    // adds the user-specific operations).
    public class UserRepository : AbstractRepository<int, User>, IUserRepository
    {
        // Constructor chains to the base. DI injects the DbContext,
        // we pass it up to AbstractRepository's constructor.
        public UserRepository(ExamPlatformContext context) : base(context)
        {
        }

        // ───────────── USER-SPECIFIC QUERIES ─────────────

        // FirstOrDefaultAsync returns the matching row or null.
        // Translates to: SELECT * FROM users WHERE email = $1 LIMIT 1;
        // The unique index on email makes this O(log n).
        public async Task<User?> GetByEmailAsync(string email)
        {
            return await _dbSet.FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task<User?> GetByUsernameAsync(string username)
        {
            return await _dbSet.FirstOrDefaultAsync(u => u.Username == username);
        }

        // AnyAsync — translates to SELECT EXISTS(SELECT 1 FROM users WHERE ...)
        // Short-circuits on first match. Most efficient existence check.
        public async Task<bool> IsEmailTakenAsync(string email)
        {
            return await _dbSet.AnyAsync(u => u.Email == email);
        }

        public async Task<bool> IsUsernameTakenAsync(string username)
        {
            return await _dbSet.AnyAsync(u => u.Username == username);
        }

        // Loads all active users that hold the given role. Used by
        // ExaminerRequestService to fan-out a notification to every admin
        // when a new examiner request arrives.
        // WhereAsync is not a thing — we use Where().ToListAsync() which
        // translates to a single: SELECT * FROM users WHERE role = $1 AND is_active = true.
        public async Task<IEnumerable<User>> GetAllByRoleAsync(UserRole role)
        {
            return await _dbSet
                .Where(u => u.Role == role && u.IsActive)
                .ToListAsync();
        }
    }
}