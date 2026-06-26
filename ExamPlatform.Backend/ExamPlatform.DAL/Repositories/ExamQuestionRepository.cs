using Microsoft.EntityFrameworkCore;
using ExamPlatform.DAL.Contexts;
using ExamPlatform.DAL.Interfaces;
using ExamPlatform.Domain.Entities;

namespace ExamPlatform.DAL.Repositories
{
    // ExamQuestionRepository manages the exam ↔ question N-N bridge table.
    public class ExamQuestionRepository : AbstractRepository<int, ExamQuestion>, IExamQuestionRepository
    {
        public ExamQuestionRepository(ExamPlatformContext context) : base(context) { }

        // ════════════════════════════════════════════════════════════════
        //   GetByExamIdAsync
        // ════════════════════════════════════════════════════════════════
        /// <summary>Returns all bridge rows for an exam, with Question eagerly loaded.</summary>
        public async Task<IEnumerable<ExamQuestion>> GetByExamIdAsync(int examId)
        {
            // Include the Question nav so callers can access question.Marks,
            // question.QuestionText etc. without a second query per row.
            return await _dbSet
                .Include(eq => eq.Question)
                .Where(eq => eq.ExamId == examId)
                .OrderBy(eq => eq.QuestionOrder)
                .ToListAsync();
        }

        // ════════════════════════════════════════════════════════════════
        //   ExistsAsync
        // ════════════════════════════════════════════════════════════════
        /// <summary>Returns true if the question is already attached to the exam.</summary>
        public async Task<bool> ExistsAsync(int examId, int questionId)
        {
            // AnyAsync = EXISTS in SQL — stops on the first match.
            return await _dbSet.AnyAsync(eq => eq.ExamId == examId && eq.QuestionId == questionId);
        }

        // ════════════════════════════════════════════════════════════════
        //   DeleteByExamAndQuestionAsync
        // ════════════════════════════════════════════════════════════════
        /// <summary>Hard-deletes the bridge row detaching one question from an exam.</summary>
        public async Task DeleteByExamAndQuestionAsync(int examId, int questionId)
        {
            // Load the specific row first — Remove() needs a tracked entity.
            var row = await _dbSet
                .FirstOrDefaultAsync(eq => eq.ExamId == examId && eq.QuestionId == questionId);

            if (row != null)
            {
                // Queue the DELETE in the change tracker. Caller does SaveChangesAsync.
                _dbSet.Remove(row);
                await _context.SaveChangesAsync();
            }
            // If the row doesn't exist, silently do nothing — detach is idempotent.
        }
    }
}