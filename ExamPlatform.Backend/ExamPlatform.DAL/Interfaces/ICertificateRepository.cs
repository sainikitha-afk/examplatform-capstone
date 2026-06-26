using ExamPlatform.Domain.Entities;

namespace ExamPlatform.DAL.Interfaces
{
    public interface ICertificateRepository : IRepository<int, Certificate>
    {
        // Admin needs all certs across all users. Generic GetAllAsync works,
        // but we add this dedicated method that eagerly includes related
        // user + exam data so the admin list view doesn't N+1 query.
        Task<IEnumerable<Certificate>> GetAllWithUserAndExamAsync();

        // Checks whether a certificate already exists for a (user, exam) pair.
        // The DB has a UNIQUE constraint on (user_id, exam_id), but we check
        // first to give a clean no-op skip during publish-results instead of
        // a constraint-violation exception bubbling up.
        Task<bool> ExistsForUserAndExamAsync(int userId, int examId);

        // Returns all certificates earned by a single candidate.
        // Used by the analytics dashboard and the candidate's certificate list.
        Task<IEnumerable<Certificate>> GetByUserAsync(int userId);

        // Paginated variant of GetAllWithUserAndExamAsync — preserves the Include
        // chains for User + Exam navigation, but adds Skip/Take for one page at a time.
        Task<(IEnumerable<Certificate> Items, int TotalCount)> GetAllWithUserAndExamPagedAsync(
            int page, int pageSize);

        // Loads a single certificate with User + Exam navigation eagerly included.
        // Used by: admin GetById (to show UserFullName / ExamTitle), candidate
        // GetByIdForCandidateAsync (ownership check + display), and the download
        // endpoint (needs both CandidateName and ExamTitle for the certificate text).
        // The base GetByIdAsync skips includes — this dedicated method exists for
        // scenarios where the navigation data is required, avoiding N+1 lazy loads.
        Task<Certificate?> GetByIdWithUserAndExamAsync(int certId);
    }
}