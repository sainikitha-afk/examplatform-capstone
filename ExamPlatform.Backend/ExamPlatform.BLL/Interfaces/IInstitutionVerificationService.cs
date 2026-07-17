using ExamPlatform.Domain.Entities;

namespace ExamPlatform.BLL.Interfaces
{
    // Institute Verification badge (Part 3/7) — mirrors ExaminerRequestService's
    // request/decision shape exactly.
    public interface IInstitutionVerificationService
    {
        // Only a Professional-tier examiner may submit. Re-submission after a
        // Rejected decision updates the same row (OwnerUserId is unique) and
        // resets it to Pending.
        Task<Institution> SubmitAsync(int examinerId, string name, string? logoUrl);

        Task<Institution?> GetMineAsync(int examinerId);

        // Admin's action queue.
        Task<IEnumerable<Institution>> GetPendingAsync();

        Task<Institution> ApproveAsync(int institutionId, int adminUserId);

        Task<Institution> RejectAsync(int institutionId, int adminUserId);
    }
}
