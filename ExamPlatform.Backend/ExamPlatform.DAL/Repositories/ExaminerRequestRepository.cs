using Microsoft.EntityFrameworkCore;
using ExamPlatform.DAL.Contexts;
using ExamPlatform.DAL.Interfaces;
using ExamPlatform.Domain.Entities;
using ExamPlatform.Domain.Enums;

namespace ExamPlatform.DAL.Repositories
{
    // Concrete implementation of IExaminerRequestRepository.
    // Inherits generic CRUD from AbstractRepository<int, ExaminerRequest>.
    public class ExaminerRequestRepository
        : AbstractRepository<int, ExaminerRequest>, IExaminerRequestRepository
    {
        public ExaminerRequestRepository(ExamPlatformContext context) : base(context)
        {
        }

        // Returns all requests ordered by submission time descending so the
        // admin dashboard naturally shows the newest requests first.
        public override async Task<IEnumerable<ExaminerRequest>> GetAllAsync()
        {
            return await _dbSet
                .OrderByDescending(r => r.RequestedAt)
                .ToListAsync();
        }

        // Returns only requests still awaiting admin action.
        // The admin sees this as their "action queue" — everything here
        // needs a decision; everything else is historical.
        public async Task<IEnumerable<ExaminerRequest>> GetPendingAsync()
        {
            return await _dbSet
                .Where(r => r.Status == ExaminerRequestStatus.Pending)
                .OrderByDescending(r => r.RequestedAt)
                .ToListAsync();
        }

        // Returns true when the email already has a Pending or Approved
        // request. AnyAsync short-circuits on first match and translates
        // to SELECT EXISTS(...) — the most efficient existence check.
        //
        // WHY include Approved here?
        //   If someone was already approved we don't want them submitting
        //   again before logging in. A Rejected request is NOT blocked —
        //   the user may legitimately reapply after fixing the issue.
        public async Task<bool> HasActiveRequestAsync(string email)
        {
            return await _dbSet.AnyAsync(r =>
                r.Email == email &&
                (r.Status == ExaminerRequestStatus.Pending ||
                 r.Status == ExaminerRequestStatus.Approved));
        }
    }
}
