using Microsoft.EntityFrameworkCore;
using ExamPlatform.DAL.Contexts;
using ExamPlatform.DAL.Interfaces;
using ExamPlatform.Domain.Entities;

namespace ExamPlatform.DAL.Repositories
{
    // AttemptAnswerRepository handles per-question answer rows for an attempt.
    public class AttemptAnswerRepository : AbstractRepository<int, AttemptAnswer>, IAttemptAnswerRepository
    {
        public AttemptAnswerRepository(ExamPlatformContext context) : base(context) { }

        // ════════════════════════════════════════════════════════════════
        //   UpsertAsync
        // ════════════════════════════════════════════════════════════════
        /// <summary>
        /// Inserts or updates the candidate's answer for a specific question.
        /// Uses the read-then-write pattern: fetch the existing row, update if
        /// found, insert if not. No raw SQL; relies on the UNIQUE DB constraint
        /// as a safety net.
        /// </summary>
        public async Task UpsertAsync(int attemptId, int questionId, int? selectedOptionId)
        {
            // ─── FIND EXISTING ROW ───────────────────────────────────────
            // The DB has a UNIQUE index on (attempt_id, question_id), so there
            // can be at most one row per (attempt, question) pair.
            var existing = await _dbSet.FirstOrDefaultAsync(
                a => a.AttemptId == attemptId && a.QuestionId == questionId);

            if (existing != null)
            {
                // ─── UPDATE PATH ─────────────────────────────────────────
                // Candidate changed their answer. Overwrite the selected option
                // and refresh the timestamp.
                existing.SelectedOptionId = selectedOptionId;
                existing.AnsweredAt = DateTime.UtcNow;
                _dbSet.Update(existing); // marks entity Modified in change tracker
            }
            else
            {
                // ─── INSERT PATH ─────────────────────────────────────────
                // First time this question is answered in this attempt.
                await _dbSet.AddAsync(new AttemptAnswer
                {
                    AttemptId = attemptId,
                    QuestionId = questionId,
                    SelectedOptionId = selectedOptionId, // null = skipped
                    AnsweredAt = DateTime.UtcNow
                    // IsCorrect and MarksAwarded are null until the attempt is
                    // submitted and evaluated — they're filled by the service.
                });
            }

            // Persist immediately so the answer is durable even if the
            // candidate closes their browser before submitting.
            await _context.SaveChangesAsync();
        }

        // ════════════════════════════════════════════════════════════════
        //   GetByAttemptAsync
        // ════════════════════════════════════════════════════════════════
        /// <summary>
        /// Returns all answer rows for an attempt, with Question and
        /// SelectedOption eagerly loaded.
        /// </summary>
        public async Task<IEnumerable<AttemptAnswer>> GetByAttemptAsync(int attemptId)
        {
            return await _dbSet
                .Include(aa => aa.Question)         // question text + marks for result view
                .Include(aa => aa.SelectedOption)   // what the candidate chose
                .Where(aa => aa.AttemptId == attemptId)
                .ToListAsync();
        }
    }
}