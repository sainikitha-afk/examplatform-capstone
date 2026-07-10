using ExamPlatform.Domain.Entities;

namespace ExamPlatform.BLL.Interfaces
{
    public interface IAgentService
    {
        Task<Dictionary<int, string>> CategorizeQuestionsAsync(IEnumerable<ExamQuestion> examQuestions);
    }
}
