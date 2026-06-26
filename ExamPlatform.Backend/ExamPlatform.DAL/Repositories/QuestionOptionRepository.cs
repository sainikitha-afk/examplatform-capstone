using Microsoft.EntityFrameworkCore;
using ExamPlatform.DAL.Contexts;
using ExamPlatform.DAL.Interfaces;
using ExamPlatform.Domain.Entities;

namespace ExamPlatform.DAL.Repositories
{
    // QuestionOptionRepository extends AbstractRepository with helpers for
    // bulk operations on a question's option rows.
    public class QuestionOptionRepository : AbstractRepository<int, QuestionOption>, IQuestionOptionRepository
    {
        public QuestionOptionRepository(ExamPlatformContext context) : base(context) { }

        // ════════════════════════════════════════════════════════════════
        //   GetByQuestionAsync
        // ════════════════════════════════════════════════════════════════
        /// <summary>Returns all options belonging to the specified question.</summary>
        public async Task<IEnumerable<QuestionOption>> GetByQuestionAsync(int questionId)
        {
            // WHERE question_id = @questionId — filters to only this question's rows.
            return await _dbSet
                .Where(o => o.QuestionId == questionId)
                .ToListAsync();
        }

        // ════════════════════════════════════════════════════════════════
        //   DeleteByQuestionAsync
        // ════════════════════════════════════════════════════════════════
        /// <summary>
        /// Queues a bulk DELETE of all option rows for a question in the EF
        /// change tracker. Does NOT call SaveChangesAsync — the caller owns the commit.
        /// </summary>
        public async Task DeleteByQuestionAsync(int questionId)
        {
            // 1. Materialise the rows we need to delete.
            //    We can't call RemoveRange on an IQueryable; EF needs actual entity
            //    objects to mark them as Deleted in the change tracker.
            var options = await _dbSet
                .Where(o => o.QuestionId == questionId)
                .ToListAsync();

            // 2. Mark all as Deleted in the change tracker.
            //    RemoveRange is the bulk equivalent of Remove() — it queues one
            //    DELETE statement per row, all committed in a single round-trip
            //    when SaveChangesAsync is eventually called by the service.
            _dbSet.RemoveRange(options);

            // ⚠ No SaveChangesAsync here — by design.
            // The service calls SaveChangesAsync once AFTER also adding the new
            // options, so EF Core wraps both the deletes and inserts in a single
            // implicit database transaction (atomicity without explicit BEGIN/COMMIT).
        }
    }
}