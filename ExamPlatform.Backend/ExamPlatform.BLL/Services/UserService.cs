// CA1848: standard ILogger extension methods used deliberately — see CertificateService.cs
#pragma warning disable CA1848
using Microsoft.Extensions.Logging;
using ExamPlatform.BLL.Exceptions;
using ExamPlatform.BLL.Interfaces;
using ExamPlatform.BLL.Models;
using ExamPlatform.DAL.Interfaces;
using ExamPlatform.Domain.Entities;

namespace ExamPlatform.BLL.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;

        // ILogger<UserService> emits structured log entries tagged with this class name.
        // The DI container injects the logger from the logging pipeline configured
        // in Program.cs (appsettings.json controls min level + output sinks).
        private readonly ILogger<UserService> _logger;

        public UserService(IUserRepository userRepository, ILogger<UserService> logger)
        {
            _userRepository = userRepository;
            _logger = logger;
        }

        // ════════════════════════════════════════════════════════════════
        //   GET BY ID
        // ════════════════════════════════════════════════════════════════
        public async Task<User> GetByIdAsync(int userId)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                // WARNING: caller asked for a non-existent user.
                // Could be a stale UI reference or a probing attempt.
                _logger.LogWarning("User {UserId} not found.", userId);
                throw new UserNotFoundException(userId);
            }
            return user;
        }

        // ════════════════════════════════════════════════════════════════
        //   GET ALL
        // ════════════════════════════════════════════════════════════════
        public async Task<IEnumerable<User>> GetAllAsync()
        {
            // Admin-only operation — no special guard here, the controller
            // enforces [Authorize(Roles="Admin")].
            var users = await _userRepository.GetAllAsync();
            _logger.LogInformation("Admin retrieved full user list ({Count} users).",
                users.Count());
            return users;
        }

        // ════════════════════════════════════════════════════════════════
        //   GET ALL PAGED
        // ════════════════════════════════════════════════════════════════
        public async Task<PagedResult<User>> GetAllPagedAsync(int page, int pageSize)
        {
            // Clamp here rather than in the controller so the rule is enforced even
            // if a non-HTTP caller (test, CLI tool) uses the service directly.
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 100); // cap at 100 per page

            _logger.LogInformation(
                "Admin retrieving users page {Page} (pageSize={PageSize}).", page, pageSize);

            var (items, totalCount) = await _userRepository.GetPagedAsync(page, pageSize);

            _logger.LogInformation(
                "Admin user list page {Page}/{TotalPages}: {Count} of {Total} users.",
                page,
                (int)Math.Ceiling((double)totalCount / pageSize),
                items.Count(),
                totalCount);

            return new PagedResult<User>
            {
                Items = items,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount
            };
        }

        // ════════════════════════════════════════════════════════════════
        //   SET ACTIVE STATUS
        // ════════════════════════════════════════════════════════════════
        public async Task<User> SetActiveStatusAsync(int userId, bool isActive)
        {
            var user = await GetByIdAsync(userId);  // throws if not found

            // No-op detection: don't bother saving if no actual change.
            if (user.IsActive == isActive)
            {
                // WARNING: redundant call — the status is already what was requested.
                // Not an error, but worth surfacing in logs so an admin can notice
                // repeated no-op calls (possibly a UI bug).
                _logger.LogWarning(
                    "SetActiveStatus for user {UserId}: status is already {Status}; no-op.",
                    userId, isActive);
                return user;
            }

            var oldStatus = user.IsActive;
            user.IsActive = isActive;
            _userRepository.Update(user);
            await _userRepository.SaveChangesAsync();

            // INFO: admin deactivated or reactivated a user — a significant business event.
            _logger.LogInformation(
                "User {UserId} active status changed from {OldStatus} to {NewStatus}.",
                userId, oldStatus, isActive);

            return user;
        }
    }
}
