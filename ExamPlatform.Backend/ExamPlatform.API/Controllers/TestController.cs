using ExamPlatform.BLL.Interfaces;
using ExamPlatform.DAL.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace ExamPlatform.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TestController : ControllerBase
    {
        private readonly IAttemptRepository _attemptRepo;
        private readonly IExamQuestionRepository _examQuestionRepo;
        private readonly IAgentService _agentService;

        public TestController(
            IAttemptRepository attemptRepo,
            IExamQuestionRepository examQuestionRepo,
            IAgentService agentService)
        {
            _attemptRepo = attemptRepo;
            _examQuestionRepo = examQuestionRepo;
            _agentService = agentService;
        }

        [HttpPost("test-analytics/{attemptId}")]
        public async Task<IActionResult> TestAnalyticsFlow(int attemptId)
        {
            var attempt = await _attemptRepo.GetByIdWithAnswersAndExamAsync(attemptId);
            if (attempt == null)
                return NotFound($"Attempt {attemptId} not found");

            var exam = attempt.Exam;
            if (exam == null || exam.ExamQuestions == null || !exam.ExamQuestions.Any())
                return BadRequest("Exam has no questions.");

            // Step 1: Gemini Categorization
            Dictionary<int, string> categoryMappings = new();
            if (exam.ExamQuestions.Any(eq => string.IsNullOrEmpty(eq.AnalyticCategory)))
            {
                categoryMappings = await _agentService.CategorizeQuestionsAsync(exam.ExamQuestions);
                
                // Temporarily apply to objects for this request (won't save to DB unless SaveChangesAsync is called)
                foreach (var eq in exam.ExamQuestions)
                {
                    if (categoryMappings.TryGetValue(eq.QuestionId, out var category))
                    {
                        eq.AnalyticCategory = category;
                    }
                }
            }
            else
            {
                // If already categorized, just build the mapping from existing data
                foreach (var eq in exam.ExamQuestions)
                {
                    categoryMappings[eq.QuestionId] = eq.AnalyticCategory ?? "Uncategorized";
                }
            }

            // Step 2: Analytics Processing (Topic Scores)
            var topicScores = new Dictionary<string, decimal>();
            var topicTotalMarks = new Dictionary<string, decimal>();
            var topicAwardedMarks = new Dictionary<string, decimal>();

            foreach (var eq in exam.ExamQuestions)
            {
                var category = eq.AnalyticCategory ?? "Uncategorized";
                var question = eq.Question;
                var answer = attempt.AttemptAnswers.FirstOrDefault(a => a.QuestionId == question.QuestionId);

                if (!topicTotalMarks.ContainsKey(category))
                {
                    topicTotalMarks[category] = 0;
                    topicAwardedMarks[category] = 0;
                }

                topicTotalMarks[category] += question.Marks;
                if (answer != null && answer.IsCorrect == true)
                {
                    topicAwardedMarks[category] += answer.MarksAwarded ?? 0;
                }
            }

            foreach (var category in topicTotalMarks.Keys)
            {
                if (topicTotalMarks[category] > 0)
                {
                    topicScores[category] = Math.Round((topicAwardedMarks[category] / topicTotalMarks[category]) * 100m, 2);
                }
                else
                {
                    topicScores[category] = 0;
                }
            }

            // Step 3: Chart Generation (JSON ONLY)
            var contentRoot = Directory.GetCurrentDirectory();
            var baseAssetPath = Path.GetFullPath(Path.Combine(contentRoot, "..", "ExamPlatform.BLL", "Assets", "candidates"));
            var userDirectory = Path.Combine(baseAssetPath, attempt.UserId.ToString());
            if (!Directory.Exists(userDirectory))
            {
                Directory.CreateDirectory(userDirectory);
            }

            var jsonFileName = $"attempt_{attempt.AttemptId}_scores.json";
            var jsonFilePath = Path.Combine(userDirectory, jsonFileName);
            await System.IO.File.WriteAllTextAsync(jsonFilePath, JsonSerializer.Serialize(topicScores));
            var topicScoresUrl = $"/assets/candidates/{attempt.UserId}/{jsonFileName}";
            
            attempt.TopicScoresUrl = topicScoresUrl;

            return Ok(new
            {
                Message = "Analytics Flow Executed Successfully",
                AttemptId = attemptId,
                GeminiResponse = categoryMappings,
                TopicScores = topicScores,
                TopicScoresUrl = topicScoresUrl
            });
        }
    }
}
