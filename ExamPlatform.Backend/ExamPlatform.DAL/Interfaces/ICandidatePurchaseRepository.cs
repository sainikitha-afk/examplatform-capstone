using ExamPlatform.Domain.Entities;

namespace ExamPlatform.DAL.Interfaces
{
    // Contract for CandidatePurchase persistence — the access grant checked
    // by AttemptService before a premium exam can be started/resumed.
    public interface ICandidatePurchaseRepository : IRepository<int, CandidatePurchase>
    {
        // Friendly pre-check before attempting an insert (the DB unique index
        // on (UserId, ExamId) is the actual race-safe guard — this just
        // avoids a raw constraint-violation exception on the happy path).
        Task<CandidatePurchase?> GetByUserAndExamAsync(int userId, int examId);

        // A candidate's purchase/billing history, newest first.
        Task<IEnumerable<CandidatePurchase>> GetByUserIdAsync(int userId);
    }
}
