using ExamPlatform.Domain.Entities;
using ExamPlatform.Domain.Enums;

namespace ExamPlatform.DAL.Interfaces
{
    // Contract for ExaminerRequest persistence. Inherits the generic CRUD
    // (GetByIdAsync, AddAsync, Update, SaveChangesAsync, …) from IRepository
    // and adds the queries specific to the request-approval workflow.
    public interface IExaminerRequestRepository : IRepository<int, ExaminerRequest>
    {
        // Returns only Pending requests — the admin's primary action queue.
        // Filtering in the repository avoids loading approved/rejected records
        // that the admin has already acted on.
        Task<IEnumerable<ExaminerRequest>> GetPendingAsync();

        // Checks whether an active (Pending or Approved) request already
        // exists for this email. Used to prevent duplicate submissions.
        Task<bool> HasActiveRequestAsync(string email);
    }
}
