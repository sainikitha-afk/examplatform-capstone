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

            // 2. Mark the attempt as processed. We no longer write a JSON file to disk:
            //    the topic scores are computed on demand from the DB and served by
            //    GET /api/analytics/attempts/{id}/topic-scores. (The old static-file
            //    approach 404'd on Azure, where the /assets path is not mounted.)
            //    TopicScoresUrl stays non-null as the "analytics ready" signal, and
            //    points straight at the live endpoint.
            attempt.TopicScoresUrl = $"/api/analytics/attempts/{attempt.AttemptId}/topic-scores";

            // 4. Save to DB
            _attemptRepo.Update(attempt);
            await _attemptRepo.SaveChangesAsync();
            
            _logger.LogInformation("Analytics Chart completely processed for Attempt {AttemptId}", attemptId);
        }
    }
}
