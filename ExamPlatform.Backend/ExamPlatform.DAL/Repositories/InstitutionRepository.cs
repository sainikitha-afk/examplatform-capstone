using Microsoft.EntityFrameworkCore;
using ExamPlatform.DAL.Contexts;
using ExamPlatform.DAL.Interfaces;
using ExamPlatform.Domain.Entities;
using ExamPlatform.Domain.Enums;

namespace ExamPlatform.DAL.Repositories
{
    public class InstitutionRepository : AbstractRepository<int, Institution>, IInstitutionRepository
    {
        public InstitutionRepository(ExamPlatformContext context) : base(context) { }

        public async Task<Institution?> GetByOwnerUserIdAsync(int ownerUserId)
        {
            return await _dbSet.FirstOrDefaultAsync(i => i.OwnerUserId == ownerUserId);
        }

        // Batch lookup for list views (browse cards) — one query for many examiners,
        // avoiding an N+1 institute lookup per exam card. Only Verified institutions
        // are returned, since only a verified institute earns the badge.
        public async Task<IEnumerable<Institution>> GetVerifiedByOwnerIdsAsync(IEnumerable<int> ownerUserIds)
        {
            var ids = ownerUserIds.Distinct().ToList();
            if (ids.Count == 0) return new List<Institution>();

            return await _dbSet
                .Where(i => ids.Contains(i.OwnerUserId)
                            && i.VerificationStatus == InstitutionVerificationStatus.Verified)
                .ToListAsync();
        }

        public async Task<IEnumerable<Institution>> GetByStatusAsync(InstitutionVerificationStatus status)
        {
            return await _dbSet
                .Include(i => i.Owner)
                .Where(i => i.VerificationStatus == status)
                .OrderBy(i => i.RequestedAt)
                .ToListAsync();
        }
    }
}
