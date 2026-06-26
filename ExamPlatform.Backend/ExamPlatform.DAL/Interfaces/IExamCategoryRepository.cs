using ExamPlatform.Domain.Entities;

namespace ExamPlatform.DAL.Interfaces
{
    // Generic CRUD only for now. Entity-specific queries will be added
    // here when a service needs them (e.g., GetByNameAsync, GetActiveAsync).
    public interface IExamCategoryRepository : IRepository<int, ExamCategory> { }
}