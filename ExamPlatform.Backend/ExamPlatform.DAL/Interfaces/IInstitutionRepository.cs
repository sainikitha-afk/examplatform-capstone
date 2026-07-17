using ExamPlatform.Domain.Entities;
using ExamPlatform.Domain.Enums;

namespace ExamPlatform.DAL.Interfaces
{
    // Contract for Institution persistence — mirrors IExaminerRequestRepository's
    // request/decision shape.
    public interface IInstitutionRepository : IRepository<int, Institution>
    {
        Task<Institution?> GetByOwnerUserIdAsync(int ownerUserId);

        // Batch lookup of Verified institutions for many examiners at once — used by
        // list views (browse cards) to attach the institute badge without an N+1 query.
        Task<IEnumerable<Institution>> GetVerifiedByOwnerIdsAsync(IEnumerable<int> ownerUserIds);

        // Admin's action queue — requests still awaiting a decision.
        Task<IEnumerable<Institution>> GetByStatusAsync(InstitutionVerificationStatus status);
    }
}
