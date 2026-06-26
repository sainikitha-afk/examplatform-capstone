using Microsoft.EntityFrameworkCore;
using ExamPlatform.DAL.Contexts;
using ExamPlatform.DAL.Interfaces;
using ExamPlatform.Domain.Entities;

namespace ExamPlatform.DAL.Repositories
{
    public class CertificateRepository : AbstractRepository<int, Certificate>, ICertificateRepository
    {
        public CertificateRepository(ExamPlatformContext context) : base(context) { }

        public async Task<IEnumerable<Certificate>> GetAllWithUserAndExamAsync()
        {
            // .Include() eagerly loads navigation properties in the SAME query
            // via SQL JOINs. Without it, accessing cert.User would trigger
            // a separate query per certificate (the classic N+1 problem).
            return await _dbSet
                .Include(c => c.User)
                .Include(c => c.Exam)
                .OrderByDescending(c => c.IssuedAt)
                .ToListAsync();
        }

        // ════════════════════════════════════════════════════════════════
        //   ExistsForUserAndExamAsync
        // ════════════════════════════════════════════════════════════════
        /// <summary>
        /// Returns true if a certificate already exists for this (user, exam) pair.
        /// </summary>
        public async Task<bool> ExistsForUserAndExamAsync(int userId, int examId)
        {
            // AnyAsync = SQL EXISTS — short-circuits on first match.
            return await _dbSet.AnyAsync(c => c.UserId == userId && c.ExamId == examId);
        }

        // ════════════════════════════════════════════════════════════════
        //   GetByUserAsync
        // ════════════════════════════════════════════════════════════════
        /// <summary>Returns all certificates earned by a specific user.</summary>
        public async Task<IEnumerable<Certificate>> GetByUserAsync(int userId)
        {
            return await _dbSet
                .Include(c => c.Exam) // exam title for the dashboard view
                .Where(c => c.UserId == userId)
                .OrderByDescending(c => c.IssuedAt)
                .ToListAsync();
        }

        // ════════════════════════════════════════════════════════════════
        //   GetAllWithUserAndExamPagedAsync
        // ════════════════════════════════════════════════════════════════
        /// <summary>
        /// Paginated admin list: preserves the User + Exam Include chains,
        /// adds Skip/Take so the DB returns only one page at a time.
        /// </summary>
        public async Task<(IEnumerable<Certificate> Items, int TotalCount)> GetAllWithUserAndExamPagedAsync(
            int page, int pageSize)
        {
            var query = _dbSet
                .Include(c => c.User)
                .Include(c => c.Exam)
                .OrderByDescending(c => c.IssuedAt);

            var totalCount = await query.CountAsync();
            var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
            return (items, totalCount);
        }

        // ════════════════════════════════════════════════════════════════
        //   GetByIdWithUserAndExamAsync
        // ════════════════════════════════════════════════════════════════
        /// <summary>
        /// Loads a single certificate with User + Exam eagerly included.
        /// The base AbstractRepository.GetByIdAsync deliberately omits Includes
        /// for performance (callers that don't need navigation shouldn't pay for
        /// joins). This method is used whenever the response needs candidate
        /// name, exam title, or exam total marks — i.e., the detail, download,
        /// and revocation notification flows.
        /// </summary>
        public async Task<Certificate?> GetByIdWithUserAndExamAsync(int certId)
        {
            // FirstOrDefaultAsync with two Includes translates to a single SQL
            // query with LEFT JOINs to users and exams — no N+1.
            return await _dbSet
                .Include(c => c.User)
                .Include(c => c.Exam)
                .FirstOrDefaultAsync(c => c.CertificateId == certId);
        }
    }
}