using Microsoft.EntityFrameworkCore;
using ExamPlatform.DAL.Contexts;
using ExamPlatform.DAL.Interfaces;
using ExamPlatform.Domain.Entities;
using ExamPlatform.Domain.Enums;

namespace ExamPlatform.DAL.Repositories
{
    // ExamRepository adds exam-lifecycle queries on top of generic CRUD.
    public class ExamRepository : AbstractRepository<int, Exam>, IExamRepository
    {
        public ExamRepository(ExamPlatformContext context) : base(context) { }

        // ════════════════════════════════════════════════════════════════
        //   GetPublishedAsync
        // ════════════════════════════════════════════════════════════════
        /// <summary>Returns all Published exams, optionally filtered by category.</summary>
        public async Task<IEnumerable<Exam>> GetPublishedAsync(int? categoryId)
        {
            // Start with a WHERE exam_status = 'Published' filter.
            var query = _dbSet.Where(e => e.ExamStatus == ExamStatus.Published);

            // If the caller supplied a category filter, append it.
            // IQueryable is lazy — the extra Where does not trigger a second query;
            // EF Core combines both predicates into one SQL WHERE clause.
            if (categoryId.HasValue)
                query = query.Where(e => e.CatId == categoryId.Value);

            return await query.OrderBy(e => e.StartTime).ToListAsync();
        }

        // ════════════════════════════════════════════════════════════════
        //   GetByExaminerAsync
        // ════════════════════════════════════════════════════════════════
        /// <summary>Returns all exams created by a specific examiner (all statuses).</summary>
        public async Task<IEnumerable<Exam>> GetByExaminerAsync(int examinerId)
        {
            return await _dbSet
                .Where(e => e.ExaminerId == examinerId)
                .OrderByDescending(e => e.CreatedAt)
                .ToListAsync();
        }

        // ════════════════════════════════════════════════════════════════
        //   GetByIdWithQuestionsAsync
        // ════════════════════════════════════════════════════════════════
        /// <summary>
        /// Loads an exam with its ExamQuestion bridge rows and the Question on each.
        /// Options are NOT loaded — use GetByIdWithQuestionsAndOptionsAsync for that.
        /// </summary>
        public async Task<Exam?> GetByIdWithQuestionsAsync(int examId)
        {
            // Two-level Include: Exam → ExamQuestions → Question.
            // ThenInclude navigates one step deeper from the previous Include result.
            // This gives us the Question.Marks for TotalMarks validation at publish time.
            return await _dbSet
                .Include(e => e.ExamQuestions)
                    .ThenInclude(eq => eq.Question)
                .FirstOrDefaultAsync(e => e.ExamId == examId);
        }

        // ════════════════════════════════════════════════════════════════
        //   GetByIdWithQuestionsAndOptionsAsync
        // ════════════════════════════════════════════════════════════════
        /// <summary>
        /// Full depth: Exam → ExamQuestions → Question → QuestionOption.
        /// Used for candidate exam rendering and attempt evaluation.
        /// </summary>
        public async Task<Exam?> GetByIdWithQuestionsAndOptionsAsync(int examId)
        {
            // Three-level eager load in one query:
            //   JOIN exam_questions ON exam_id
            //   JOIN questions ON question_id
            //   JOIN question_options ON question_id
            // EF Core issues one SQL with three JOINs rather than separate queries.
            return await _dbSet
                .Include(e => e.ExamQuestions)
                    .ThenInclude(eq => eq.Question)
                        .ThenInclude(q => q.Options)
                .FirstOrDefaultAsync(e => e.ExamId == examId);
        }

        // ════════════════════════════════════════════════════════════════
        //   GetPublishedPagedAsync
        // ════════════════════════════════════════════════════════════════
        /// <summary>
        /// Paginated browse: Published exams, optionally filtered by category.
        /// Two SQL round-trips: COUNT(*) then SELECT ... SKIP/TAKE.
        /// </summary>
        public async Task<(IEnumerable<Exam> Items, int TotalCount)> GetPublishedPagedAsync(
            int? categoryId, int page, int pageSize)
        {
            // Build the same WHERE clause as GetPublishedAsync, then apply paging.
            var query = _dbSet.Where(e => e.ExamStatus == ExamStatus.Published);

            if (categoryId.HasValue)
                query = query.Where(e => e.CatId == categoryId.Value);

            // ORDER BY ensures deterministic pages (same row always on same page).
            query = query.OrderBy(e => e.StartTime);

            var totalCount = await query.CountAsync();
            var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
            return (items, totalCount);
        }

        // ════════════════════════════════════════════════════════════════
        //   GetAvailableForCandidatePagedAsync
        // ════════════════════════════════════════════════════════════════
        /// <summary>
        /// Candidate browse: Published exams MINUS the ones this candidate has already
        /// completed. One attempt per candidate — once Submitted/AutoSubmitted, the exam
        /// is gone from their browse list (it could only ever 409 on Start). An exam with
        /// only an InProgress attempt stays listed so the candidate can find their way
        /// back and resume. The subquery translates to a SQL NOT EXISTS, so pagination
        /// counts remain correct.
        /// </summary>
        public async Task<(IEnumerable<Exam> Items, int TotalCount)> GetAvailableForCandidatePagedAsync(
            int userId, int? categoryId, int page, int pageSize)
        {
            var query = _dbSet
                .Where(e => e.ExamStatus == ExamStatus.Published)
                .Where(e => !e.Attempts.Any(a =>
                    a.UserId == userId &&
                    (a.AttemptStatus == AttemptStatus.Submitted ||
                     a.AttemptStatus == AttemptStatus.AutoSubmitted)));

            if (categoryId.HasValue)
                query = query.Where(e => e.CatId == categoryId.Value);

            // Same deterministic ordering as GetPublishedPagedAsync.
            query = query.OrderBy(e => e.StartTime);

            var totalCount = await query.CountAsync();
            var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
            return (items, totalCount);
        }

        // ════════════════════════════════════════════════════════════════
        //   GetByExaminerPagedAsync
        // ════════════════════════════════════════════════════════════════
        /// <summary>
        /// Paginated "My Exams": all statuses for one examiner, newest first.
        /// </summary>
        public async Task<(IEnumerable<Exam> Items, int TotalCount)> GetByExaminerPagedAsync(
            int examinerId, int page, int pageSize)
        {
            var query = _dbSet
                .Where(e => e.ExaminerId == examinerId)
                .OrderByDescending(e => e.CreatedAt);

            var totalCount = await query.CountAsync();
            var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
            return (items, totalCount);
        }

        // ════════════════════════════════════════════════════════════════
        //   GetPublishedCountsByCategoryAsync
        // ════════════════════════════════════════════════════════════════
        /// <summary>
        /// One GROUP BY query returning { CatId: publishedExamCount }. Categories
        /// with zero published exams are simply absent from the dictionary —
        /// callers default to 0 on lookup miss.
        /// </summary>
        public async Task<Dictionary<int, int>> GetPublishedCountsByCategoryAsync()
        {
            return await _dbSet
                .Where(e => e.ExamStatus == ExamStatus.Published)
                .GroupBy(e => e.CatId)
                .Select(g => new { CatId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.CatId, x => x.Count);
        }

        // ════════════════════════════════════════════════════════════════
        //   AnyByCategoryAsync
        // ════════════════════════════════════════════════════════════════
        /// <summary>True if any exam (any status) references this category.</summary>
        public async Task<bool> AnyByCategoryAsync(int catId)
        {
            return await _dbSet.AnyAsync(e => e.CatId == catId);
        }
    }
}