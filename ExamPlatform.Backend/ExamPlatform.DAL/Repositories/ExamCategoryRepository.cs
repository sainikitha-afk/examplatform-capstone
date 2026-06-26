using ExamPlatform.DAL.Contexts;
using ExamPlatform.DAL.Interfaces;
using ExamPlatform.Domain.Entities;

namespace ExamPlatform.DAL.Repositories
{
    // Inherits all CRUD from AbstractRepository<int, ExamCategory>.
    // No overrides or extra methods needed yet.
    public class ExamCategoryRepository : AbstractRepository<int, ExamCategory>, IExamCategoryRepository
    {
        public ExamCategoryRepository(ExamPlatformContext context) : base(context) { }
    }
}