using Microsoft.EntityFrameworkCore;
using ExamPlatform.DAL.Contexts;
using ExamPlatform.DAL.Interfaces;
using ExamPlatform.Domain.Entities;
using ExamPlatform.Domain.Enums;

namespace ExamPlatform.DAL.Repositories
{
    // AttemptRepository supports the candidate exam-taking workflow.
    public class AttemptRepository : AbstractRepository<int, Attempt>, IAttemptRepository
    {
        public AttemptRepository(ExamPlatformContext context) : base(context) { }

        // ════════════════════════════════════════════════════════════════
        //   GetInProgressAsync
        // ════════════════════════════════════════════════════════════════
        /// <summary>
        /// Finds an existing InProgress attempt for the (user, exam) pair.
        /// Returns null if none — the service will create a new one.
        /// </summary>
        public async Task<Attempt?> GetInProgressAsync(int userId, int examId)
        {
            return await _dbSet.FirstOrDefaultAsync(a =>
                a.UserId == userId &&
                a.ExamId == examId &&
                a.AttemptStatus == AttemptStatus.InProgress);
        }

        // ════════════════════════════════════════════════════════════════
        //   TryAddInProgressAsync  (race-safe)
        // ════════════════════════════════════════════════════════════════
        /// <summary>
        /// Inserts a new InProgress attempt, returning false (instead of throwing)
        /// when the filtered unique index "uix_attempts_user_exam_inprogress" is
        /// violated — i.e. a concurrent double-start already created one. The
        /// service then resumes the winning attempt. This is what makes rapid
        /// double-clicks / two tabs safe rather than a 500.
        /// </summary>
        public async Task<bool> TryAddInProgressAsync(Attempt attempt)
        {
            _dbSet.Add(attempt);
            try
            {
                await _context.SaveChangesAsync();
                return true;
            }
            catch (DbUpdateException ex) when (
                ex.InnerException is Npgsql.PostgresException { SqlState: "23505" })
            {
                // Detach the failed insert so the context stays usable, then let
                // the caller fall back to resuming the concurrently-created attempt.
                _context.Entry(attempt).State = EntityState.Detached;
                return false;
            }
        }

        // ════════════════════════════════════════════════════════════════
        //   HasCompletedAttemptAsync
        // ════════════════════════════════════════════════════════════════
        /// <summary>
        /// True if the user has already submitted (manually or by timeout) an
        /// attempt for this exam. AnyAsync issues a SQL EXISTS — no rows loaded.
        /// </summary>
        public async Task<bool> HasCompletedAttemptAsync(int userId, int examId)
        {
            return await _dbSet.AnyAsync(a =>
                a.UserId == userId &&
                a.ExamId == examId &&
                (a.AttemptStatus == AttemptStatus.Submitted ||
                 a.AttemptStatus == AttemptStatus.AutoSubmitted));
        }

        // ════════════════════════════════════════════════════════════════
        //   CountCompletedAttemptsAsync
        // ════════════════════════════════════════════════════════════════
        /// <summary>
        /// How many attempts this user has finished (submitted or timed out) for
        /// this exam — compared against Exam.MaxAttempts to decide whether another
        /// attempt is allowed. SQL COUNT — no rows loaded.
        /// </summary>
        public async Task<int> CountCompletedAttemptsAsync(int userId, int examId)
        {
            return await _dbSet.CountAsync(a =>
                a.UserId == userId &&
                a.ExamId == examId &&
                (a.AttemptStatus == AttemptStatus.Submitted ||
                 a.AttemptStatus == AttemptStatus.AutoSubmitted));
        }

        // ════════════════════════════════════════════════════════════════
        //   GetByUserAsync
        // ════════════════════════════════════════════════════════════════
        /// <summary>Returns all attempts by a user, newest first, with Exam loaded.</summary>
        public async Task<IEnumerable<Attempt>> GetByUserAsync(int userId)
        {
            // Include Exam so the response DTO can show exam title, passing marks etc.
            return await _dbSet
                .Include(a => a.Exam)
                .Where(a => a.UserId == userId)
                .OrderByDescending(a => a.StartedAt)
                .ToListAsync();
        }

        // ═════════════════════════════���══════════════════════════════════
        //   GetByExamIdAsync
        // ════════════════════════════════════════════════════════════════
        /// <summary>
        /// Returns all Submitted/AutoSubmitted attempts for an exam, with User loaded.
        /// Used by publish-results to find the best attempt per candidate.
        /// </summary>
        public async Task<IEnumerable<Attempt>> GetByExamIdAsync(int examId)
        {
            return await _dbSet
                .Include(a => a.User)   // email address for notifications
                .Where(a => a.ExamId == examId &&
                            (a.AttemptStatus == AttemptStatus.Submitted ||
                             a.AttemptStatus == AttemptStatus.AutoSubmitted))
                .ToListAsync();
        }

        // ════════════════════════════════════════════════════════════════
        //   GetAllByExamIdAsync
        // ════════════════════════════════════════════════════════════════
        /// <summary>
        /// Returns ALL attempts for an exam (every status, including InProgress),
        /// with User and answer rows loaded, newest first. Used by the examiner
        /// monitoring view to show who took the exam and how each attempt went.
        /// </summary>
        public async Task<IEnumerable<Attempt>> GetAllByExamIdAsync(int examId)
        {
            return await _dbSet
                .Include(a => a.User)            // candidate name + email
                .Include(a => a.AttemptAnswers)  // answered count for progress display
                .Where(a => a.ExamId == examId)
                .OrderByDescending(a => a.StartedAt)
                .ToListAsync();
        }

        // ════════════════════════════════════════════════════════════════
        //   GetByIdWithAnswersAndExamAsync
        // ════════════════════════════════════════════════════════════════
        /// <summary>
        /// Loads an attempt with all answer rows, the questions answered, the
        /// selected options, and the exam (with questions + options).
        /// Used for result view, resume view, and evaluation.
        /// </summary>
        public async Task<Attempt?> GetByIdWithAnswersAndExamAsync(int attemptId)
        {
            return await _dbSet
                // Load the candidate — needed for examiner alerts (resume / auto-submit).
                .Include(a => a.User)
                // Load answer rows
                .Include(a => a.AttemptAnswers)
                    .ThenInclude(aa => aa.SelectedOption) // what the candidate chose
                // Load the exam with full question depth (needed for evaluation)
                .Include(a => a.Exam)
                    .ThenInclude(e => e.ExamQuestions)
                        .ThenInclude(eq => eq.Question)
                            .ThenInclude(q => q.Options) // to find the correct option
                .FirstOrDefaultAsync(a => a.AttemptId == attemptId);
        }

        // ════════════════════════════════════════════════════════════════
        //   HasAnyForExamAsync
        // ════════════════════════════════════════════════════════════════
        /// <summary>
        /// Returns true if ANY attempt record (any status) exists for the exam.
        /// AnyAsync translates to SQL EXISTS — no rows are loaded, O(1) in practice.
        /// Used by UnpublishAsync to guard against reverting an exam that candidates
        /// have already interacted with, regardless of whether they submitted or not.
        /// </summary>
        public async Task<bool> HasAnyForExamAsync(int examId)
        {
            // AnyAsync stops at the first matching row (no full table scan).
            return await _dbSet.AnyAsync(a => a.ExamId == examId);
        }

        // ════════════════════════════════════════════════════════════════
        //   GetByUserPagedAsync
        // ════════════════════════════════════════════════════════════════
        /// <summary>
        /// Paginated attempt history for a candidate.
        /// Preserves the Exam Include so DTO mapping can show exam title.
        /// </summary>
        public async Task<(IEnumerable<Attempt> Items, int TotalCount)> GetByUserPagedAsync(
            int userId, int page, int pageSize)
        {
            var query = _dbSet
                .Include(a => a.Exam)
                .Where(a => a.UserId == userId)
                .OrderByDescending(a => a.StartedAt);

            var totalCount = await query.CountAsync();
            var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
            return (items, totalCount);
        }
    }
}