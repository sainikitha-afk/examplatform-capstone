using ExamPlatform.Domain.Entities;

namespace ExamPlatform.DAL.Interfaces
{
    // IQuestionOptionRepository inherits generic CRUD and adds bulk helpers
    // needed for atomic question-update (replace all options at once).
    public interface IQuestionOptionRepository : IRepository<int, QuestionOption>
    {
        // Returns all options belonging to a question.
        // Used when building response DTOs without the full Question include.
        Task<IEnumerable<QuestionOption>> GetByQuestionAsync(int questionId);

        // Queues a RemoveRange for all options of a question in the EF change
        // tracker WITHOUT calling SaveChangesAsync. The caller is responsible
        // for committing. This enables the atomic update pattern:
        //   DeleteByQuestionAsync (queue deletes)
        //   AddAsync x4           (queue inserts)
        //   SaveChangesAsync      (one round-trip, one implicit DB transaction)
        Task DeleteByQuestionAsync(int questionId);
    }
}