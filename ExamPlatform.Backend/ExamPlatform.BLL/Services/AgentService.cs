using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using ExamPlatform.BLL.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ExamPlatform.BLL.Services
{
    public class AgentService : IAgentService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<AgentService> _logger;
        private readonly IConfiguration _config;
        private readonly string? _apiKey;

        public AgentService(HttpClient httpClient, IConfiguration config, ILogger<AgentService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
            _config = config;
            _apiKey = config["Gemini:ApiKey"];
        }

        public async Task<Dictionary<int, string>> CategorizeQuestionsAsync(IEnumerable<ExamPlatform.Domain.Entities.ExamQuestion> examQuestions)
        {
            if (string.IsNullOrWhiteSpace(_apiKey))
            {
                _logger.LogError("Gemini API Key is missing.");
                throw new Exception("Gemini API Key is missing. Failing instead of falling back.");
            }

            var questionDetails = examQuestions.Select(eq => new
            {
                Id = eq.QuestionId,
                Text = eq.Question.QuestionText
            }).ToList();

            var promptTemplatePath = _config["Gemini:PromptFilePath"];
            var promptTemplate = await File.ReadAllTextAsync(promptTemplatePath ?? "Assets/gemini-prompt.txt");
            var prompt = string.Format(promptTemplate, JsonSerializer.Serialize(questionDetails));

            var requestBody = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new[]
                        {
                            new { text = prompt }
                        }
                    }
                },
                generationConfig = new
                {
                    response_mime_type = "application/json"
                }
            };

            try
            {
                var apiUrl = _config["Gemini:ApiUrl"];
                if (string.IsNullOrEmpty(apiUrl))
                {
                    apiUrl = "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent";
                }
                var requestUrl = $"{apiUrl}?key={_apiKey}";

                var response = await _httpClient.PostAsJsonAsync(requestUrl, requestBody);

                response.EnsureSuccessStatusCode();

                var jsonResponse = await response.Content.ReadAsStringAsync();
                
                // Parse the Gemini response structure
                using var document = JsonDocument.Parse(jsonResponse);
                var textResult = document.RootElement
                    .GetProperty("candidates")[0]
                    .GetProperty("content")
                    .GetProperty("parts")[0]
                    .GetProperty("text")
                    .GetString();

                if (string.IsNullOrWhiteSpace(textResult))
                {
                    throw new Exception("Empty response from Gemini.");
                }

                var categories = JsonSerializer.Deserialize<Dictionary<string, string>>(textResult);
                
                if (categories == null) throw new Exception("Gemini returned null categories.");

                var result = new Dictionary<int, string>();
                foreach (var kvp in categories)
                {
                    if (int.TryParse(kvp.Key, out int id))
                    {
                        result[id] = kvp.Value;
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to call Gemini API. Failing instead of falling back.");
                
                // Write error to BLL/service.log
                var contentRoot = Directory.GetCurrentDirectory();
                var logPath = Path.GetFullPath(Path.Combine(contentRoot, "..", "ExamPlatform.BLL", "service.log"));
                await File.AppendAllTextAsync(logPath, $"[{DateTime.UtcNow:O}] GEMINI API ERROR: {ex.Message}\n{ex.StackTrace}\n\n");
                
                throw;
            }
        }

        /*
        private Dictionary<int, string> GenerateFallbackCategories(IEnumerable<ExamPlatform.Domain.Entities.ExamQuestion> examQuestions)
        {
            var result = new Dictionary<int, string>();
            var fallbackTopics = new[] { "General Knowledge", "Problem Solving", "Core Concepts" };
            var rnd = new Random();

            foreach (var eq in examQuestions)
            {
                result[eq.QuestionId] = fallbackTopics[rnd.Next(fallbackTopics.Length)];
            }
            return result;
        }
        */
    }
}
