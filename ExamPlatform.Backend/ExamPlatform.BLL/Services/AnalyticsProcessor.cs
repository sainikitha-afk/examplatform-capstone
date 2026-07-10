using ExamPlatform.BLL.Interfaces;
using ExamPlatform.DAL.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace ExamPlatform.BLL.Services
{
    public class AnalyticsProcessor : IAnalyticsProcessor
    {
        private readonly IAttemptRepository _attemptRepo;
        private readonly IExamQuestionRepository _examQuestionRepo;
        private readonly IAgentService _agentService;
        private readonly ILogger<AnalyticsProcessor> _logger;

        public AnalyticsProcessor(
            IAttemptRepository attemptRepo,
            IExamQuestionRepository examQuestionRepo,
            IAgentService agentService,
            ILogger<AnalyticsProcessor> logger)
        {
            _attemptRepo = attemptRepo;
            _examQuestionRepo = examQuestionRepo;
            _agentService = agentService;
            _logger = logger;
        }

        public async Task ProcessAttemptAsync(int attemptId, CancellationToken stoppingToken)
        {
            var attempt = await _attemptRepo.GetByIdWithAnswersAndExamAsync(attemptId);
            if (attempt == null) return;
            
            // Used to mark as Processing, but now we rely on TopicScoresUrl
            _attemptRepo.Update(attempt);
            await _attemptRepo.SaveChangesAsync();

            var exam = attempt.Exam;
            if (exam == null || exam.ExamQuestions == null || !exam.ExamQuestions.Any()) return;

            // 1. Check if categories exist. If not, generate them (Lazy Evaluation)
            if (exam.ExamQuestions.Any(eq => string.IsNullOrEmpty(eq.AnalyticCategory)))
            {
                _logger.LogInformation("Generating categories for Exam {ExamId} via Gemini API", exam.ExamId);
                var categoryMappings = await _agentService.CategorizeQuestionsAsync(exam.ExamQuestions);
                
                foreach (var eq in exam.ExamQuestions)
                {
                    if (categoryMappings.TryGetValue(eq.QuestionId, out var category))
                    {
                        eq.AnalyticCategory = category;
                        _examQuestionRepo.Update(eq);
                    }
                }
                await _examQuestionRepo.SaveChangesAsync();
            }

            // 2. Calculate Topic-wise Accuracy
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

            // 3. Save JSON
            var contentRoot = Directory.GetCurrentDirectory();
            var baseAssetPath = Path.GetFullPath(Path.Combine(contentRoot, "..", "ExamPlatform.BLL", "Assets", "candidates"));
            var userDirectory = Path.Combine(baseAssetPath, attempt.UserId.ToString());
            if (!Directory.Exists(userDirectory))
            {
                Directory.CreateDirectory(userDirectory);
            }

            var jsonFileName = $"attempt_{attempt.AttemptId}_scores.json";
            var jsonFilePath = Path.Combine(userDirectory, jsonFileName);
            await File.WriteAllTextAsync(jsonFilePath, JsonSerializer.Serialize(topicScores));
            
            _logger.LogInformation("Successfully saved JSON for Attempt {AttemptId} at {FilePath}", attempt.AttemptId, jsonFilePath);
            
            var topicScoresUrl = $"/assets/candidates/{attempt.UserId}/{jsonFileName}";

            // 4. Save to DB
            attempt.TopicScoresUrl = topicScoresUrl;
            _attemptRepo.Update(attempt);
            await _attemptRepo.SaveChangesAsync();
            
            _logger.LogInformation("Analytics Chart completely processed for Attempt {AttemptId}", attemptId);
        }
    }
}
