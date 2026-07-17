using Microsoft.EntityFrameworkCore;
using ExamPlatform.DAL.Contexts;
using ExamPlatform.DAL.Interfaces;
using ExamPlatform.Domain.Entities;
using ExamPlatform.Domain.Enums;

namespace ExamPlatform.DAL.Repositories
{
    // QuestionRepository provides Question-specific queries on top of the
    // generic CRUD inherited from AbstractRepository<int, Question>.
    public class QuestionRepository : AbstractRepository<int, Question>, IQuestionRepository
    {
        // Constructor passes the DbContext up to AbstractRepository, which
        // stores it as _context and initialises _dbSet = context.Set<Question>().
        public QuestionRepository(ExamPlatformContext context) : base(context) { }

        // ════════════════════════════════════════════════════════════════
        //   GetByIdWithOptionsAsync
        // ════════════════════════════════════════════════════════════════
        /// <summary>
        /// Loads a single question together with its option rows in one query.
        /// </summary>
        public async Task<Question?> GetByIdWithOptionsAsync(int questionId)
        {
            // .Include(q => q.Options) tells EF Core to LEFT JOIN question_options
            // on question_id in the same SQL round-trip (eager loading).
            // Without Include, accessing question.Options would either trigger
            // a second query per question (N+1) or throw if lazy loading is disabled.
            //
            // FirstOrDefaultAsync returns null when no row matches — the caller
            // (QuestionService) converts null to QuestionNotFoundException.
            return await _dbSet
                .Include(q => q.Options) // JOIN question_options
                .FirstOrDefaultAsync(q => q.QuestionId == questionId);
        }

        // ════════════════════════════════════════════════════════════════
        //   GetByExaminerAsync
        // ════════════════════════════════════════════════════════════════
        /// <summary>
        /// Returns all active questions authored by the given examiner.
        /// </summary>
        public async Task<IEnumerable<Question>> GetByExaminerAsync(int examinerId)
        {
            // WHERE examiner_id = @examinerId AND is_active = true
            // IsActive filter: soft-deleted questions are invisible to examiners.
            // LINQ lambdas here translate to a parameterised SQL WHERE clause;
            // EF Core never interpolates raw strings, so there's no SQL injection risk.
            return await _dbSet
                .Where(q => q.ExaminerId == examinerId && q.IsActive)
                .ToListAsync();
        }

        // ════════════════════════════════════════════════════════════════
        //   IsUsedInPublishedExamAsync
        // ════════════════════════════════════════════════════════════════
        /// <summary>
        /// Returns true if the question is attached to at least one Published exam.
        /// </summary>
        public async Task<bool> IsUsedInPublishedExamAsync(int questionId)
        {
            // Business rule: a question that is currently live in a Published exam
            // cannot be soft-deleted — candidates may be mid-attempt.
            //
            // Strategy: join exam_questions → exams, filter by questionId and
            // ExamStatus == Published, then AnyAsync (translates to EXISTS in SQL —
            // stops scanning as soon as one matching row is found, efficient).
            //
            // _context.ExamQuestions accesses the ExamQuestion DbSet directly.
            // AbstractRepository only exposes the DbSet<T> for its own type,
            // so we go through _context here for the cross-entity join.
            return await _context.ExamQuestions
                .AnyAsync(eq =>
                    eq.QuestionId == questionId &&               // this question
                    eq.Exam.ExamStatus == ExamStatus.Published); // in a live exam
            // EF Core translates eq.Exam.ExamStatus into a JOIN on exams without
            // us writing explicit join syntax. Navigation-property access inside
            // LINQ = implicit JOIN in the generated SQL.
        }

        // ════════════════════════════════════════════════════════════════
        //   IsDuplicateAsync
        // ════════════════════════════════════════════════════════════════
        /// <summary>
        /// Returns true when the examiner already has an active question with
        /// identical text (case-insensitive, whitespace-trimmed) in the same category.
        /// </summary>
        public async Task<bool> IsDuplicateAsync(int examinerId, int catId, string questionText)
        {
            // Normalise the incoming text so "  What is DI?  " and "WHAT IS DI?"
            // are caught as duplicates. ToLower() on the LINQ expression translates
            // to LOWER() in PostgreSQL SQL; the right-hand side is client-evaluated
            // and passed as a parameter — no SQL injection risk.
            var normalised = questionText.Trim().ToLower();

            return await _dbSet.AnyAsync(q =>
                q.ExaminerId == examinerId &&       // scoped to this examiner
                q.CatId == catId &&                 // same category
                q.IsActive &&                       // only check against active questions
                q.QuestionText.ToLower() == normalised); // case-insensitive match
        }

        // ════════════════════════════════════════════════════════════════
        //   GetFilteredAsync
        // ════════════════════════════════════════════════════════════════
        /// <summary>
        /// Server-side filtered list. All predicates are translated to SQL WHERE
        /// clauses by EF Core — no in-memory filtering.
        /// </summary>
        public async Task<IEnumerable<Question>> GetFilteredAsync(
            int? examinerId,
            int? categoryId,
            string? difficulty,
            string? search)
        {
            // Start from the base set. We build the WHERE clause incrementally
            // by chaining .Where() calls — EF Core composes them into a single SQL.
            var query = _dbSet.AsQueryable();

            if (examinerId.HasValue)
            {
                // Examiner scope: their questions, active only (mirrors GetByExaminerAsync).
                query = query.Where(q => q.ExaminerId == examinerId.Value && q.IsActive);
            }
            // Admin (examinerId == null): no IsActive filter — Admin sees all questions
            // including soft-deleted ones (IsActive=false) for moderation purposes.

            if (categoryId.HasValue)
                query = query.Where(q => q.CatId == categoryId.Value);

            if (!string.IsNullOrWhiteSpace(difficulty))
            {
                // Case-insensitive difficulty match: ToLower() translates to LOWER() in SQL.
                var diffLower = difficulty.Trim().ToLower();
                query = query.Where(q => q.Difficulty.ToLower() == diffLower);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                // Substring search on QuestionText. ToLower() + Contains translates to
                // LOWER(question_text) LIKE '%keyword%' in PostgreSQL — full-text search
                // is outside scope for this platform.
                var searchLower = search.Trim().ToLower();
                query = query.Where(q => q.QuestionText.ToLower().Contains(searchLower));
            }

            return await query.ToListAsync();
        }

        // ════════════════════════════════════════════════════════════════
        //   GetForStatsAsync
        // ════════════════════════════════════════════════════════════════
        /// <summary>
        /// Returns active questions WITH the Category navigation eagerly loaded.
        /// Used by GetStatsAsync to compute per-category counts in one query
        /// rather than N+1 round-trips.
        /// </summary>
        public async Task<IEnumerable<Question>> GetForStatsAsync(int? examinerId)
        {
            // Always filter IsActive=true for stats — we count the live question bank,
            // not soft-deleted rows.
            var query = _dbSet
                .Include(q => q.Category) // eager-load the category so CatName is available
                .Where(q => q.IsActive);

            if (examinerId.HasValue)
                query = query.Where(q => q.ExaminerId == examinerId.Value);

            return await query.ToListAsync();
        }

        // ════════════════════════════════════════════════════════════════
        //   GetFilteredPagedAsync
        // ════════════════════════════════════════════════════════════════
        /// <summary>
        /// Paginated + filtered question list. Same WHERE predicates as GetFilteredAsync,
        /// but adds Skip/Take for server-side pagination.
        /// </summary>
        public async Task<(IEnumerable<Question> Items, int TotalCount)> GetFilteredPagedAsync(
            int? examinerId,
            int? categoryId,
            string? difficulty,
            string? search,
            int page,
            int pageSize)
        {
            // Build the identical WHERE chain used by GetFilteredAsync — reuses the same
            // IQueryable-composition pattern so filters are sent to SQL, not evaluated in memory.
            var query = _dbSet.AsQueryable();

            if (examinerId.HasValue)
                query = query.Where(q => q.ExaminerId == examinerId.Value && q.IsActive);

            if (categoryId.HasValue)
                query = query.Where(q => q.CatId == categoryId.Value);

            if (!string.IsNullOrWhiteSpace(difficulty))
            {
                var diffLower = difficulty.Trim().ToLower();
                query = query.Where(q => q.Difficulty.ToLower() == diffLower);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var searchLower = search.Trim().ToLower();
                query = query.Where(q => q.QuestionText.ToLower().Contains(searchLower));
            }

            // Stable ORDER BY before Skip/Take so the page is deterministic.
            query = query.OrderBy(q => q.QuestionId);

            var totalCount = await query.CountAsync();
            var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
            return (items, totalCount);
        }

        // ════════════════════════════════════════════════════════════════
        //   AnyByCategoryAsync
        // ════════════════════════════════════════════════════════════════
        /// <summary>True if any question (active or soft-deleted) references this category.</summary>
        public async Task<bool> AnyByCategoryAsync(int catId)
        {
            return await _dbSet.AnyAsync(q => q.CatId == catId);
        }
    }
}
