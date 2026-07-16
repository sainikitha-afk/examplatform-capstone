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

        /// <summary>
        /// Resolves the Gemini prompt-template file path.
        /// If <paramref name="configuredPath"/> (Gemini:PromptFilePath) is set, it is
        /// honored as-is (absolute or relative to the caller's choice). Otherwise the
        /// prompt is resolved against <see cref="AppContext.BaseDirectory"/> — the folder
        /// that holds the running binaries — where the build copies Assets/gemini-prompt.txt.
        /// This is machine-independent and works both under `dotnet run` (base dir =
        /// ExamPlatform.API/bin/...) and under the test runner (base dir = test bin/...),
        /// unlike a CWD-relative path which varies by launch directory.
        /// </summary>
        public static string ResolvePromptPath(string? configuredPath)
        {
            if (!string.IsNullOrWhiteSpace(configuredPath))
                return configuredPath;

            return Path.Combine(AppContext.BaseDirectory, "Assets", "gemini-prompt.txt");
        }

        // Caps a diagnostic string so a large provider error body can't flood the logs.
        private static string Truncate(string? value, int max)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            return value.Length <= max ? value : value.Substring(0, max) + "…";
        }

        /// <summary>
        /// Recovers a parseable JSON object from an LLM text response. LLMs occasionally
        /// wrap the JSON in ```json fences, prepend/append prose, or truncate the final
        /// closing brace (observed with thinking models even at finishReason=STOP). This
        /// strips fences, isolates the object starting at the first '{', trims any trailing
        /// content past the matching close, and appends missing closing braces when the
        /// model omitted them — so a complete-but-unterminated object still deserializes.
        /// </summary>
        public static string ExtractJsonObject(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                throw new Exception("Empty response from Gemini.");

            var s = raw.Trim();

            // Strip a leading ```json / ``` fence and its trailing ``` if present.
            if (s.StartsWith("```"))
            {
                var firstNewline = s.IndexOf('\n');
                if (firstNewline >= 0) s = s.Substring(firstNewline + 1);
                if (s.EndsWith("```")) s = s.Substring(0, s.Length - 3);
                s = s.Trim();
            }

            var start = s.IndexOf('{');
            if (start < 0)
                throw new Exception("No JSON object found in Gemini response.");
            s = s.Substring(start);

            // Walk the braces (ignoring those inside strings) to find the matching close.
            var open = 0;
            var inString = false;
            var escaped = false;
            for (var i = 0; i < s.Length; i++)
            {
                var c = s[i];
                if (escaped) { escaped = false; continue; }
                if (c == '\\') { escaped = true; continue; }
                if (c == '"') { inString = !inString; continue; }
                if (inString) continue;
                if (c == '{') open++;
                else if (c == '}')
                {
                    open--;
                    if (open == 0) return s.Substring(0, i + 1); // trims any trailing prose
                }
            }

            // Never closed — append the missing brace(s) so a complete object still parses.
            return s + new string('}', Math.Max(open, 1));
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

            var promptTemplatePath = ResolvePromptPath(_config["Gemini:PromptFilePath"]);
            var promptTemplate = await File.ReadAllTextAsync(promptTemplatePath);
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
                    response_mime_type = "application/json",
                    // gemini-flash-latest is a "thinking" model. Left on, it spends most of
                    // its output budget on hidden reasoning and intermittently returns the
                    // answer JSON with the closing brace missing (finishReason=STOP). A zero
                    // thinking budget makes structured JSON output fast and far more reliable.
                    thinkingConfig = new { thinkingBudget = 0 }
                }
            };

            try
            {
                var apiUrl = _config["Gemini:ApiUrl"];
                if (string.IsNullOrEmpty(apiUrl))
                {
                    // gemini-flash-latest is an always-current alias that supports
                    // generateContent; the retired gemini-2.5-flash returns 404 for
                    // new API keys ("no longer available to new users").
                    apiUrl = "https://generativelanguage.googleapis.com/v1beta/models/gemini-flash-latest:generateContent";
                }
                // NOTE: the API key is appended only to requestUrl, which is NEVER logged.
                // Diagnostics below log apiUrl (no key) and the safe response body only.
                var requestUrl = $"{apiUrl}?key={_apiKey}";

                var response = await _httpClient.PostAsJsonAsync(requestUrl, requestBody);

                if (!response.IsSuccessStatusCode)
                {
                    // Read the provider's error payload so failures are diagnosable.
                    // Google's error body does not contain the API key.
                    var errorBody = await response.Content.ReadAsStringAsync();
                    _logger.LogError(
                        "Gemini request failed: HTTP {StatusCode} from endpoint {Endpoint}. Response: {Body}",
                        (int)response.StatusCode, apiUrl, Truncate(errorBody, 500));
                    throw new HttpRequestException(
                        $"Gemini API returned HTTP {(int)response.StatusCode} for endpoint '{apiUrl}'. Response: {Truncate(errorBody, 500)}");
                }

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

                // Parse tolerantly: the model can wrap the JSON in markdown fences,
                // add surrounding prose, or (observed) omit the closing brace. See
                // ExtractJsonObject — it recovers a well-formed object from all three.
                var categories = JsonSerializer.Deserialize<Dictionary<string, string>>(
                    ExtractJsonObject(textResult));
                
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

                // Best-effort append to BLL/service.log. This path is CWD-relative and
                // may not exist under every host (e.g. the test runner), so a secondary
                // IO failure here must never mask the real Gemini error we're rethrowing.
                try
                {
                    var contentRoot = Directory.GetCurrentDirectory();
                    var logPath = Path.GetFullPath(Path.Combine(contentRoot, "..", "ExamPlatform.BLL", "service.log"));
                    await File.AppendAllTextAsync(logPath, $"[{DateTime.UtcNow:O}] GEMINI API ERROR: {ex.Message}\n{ex.StackTrace}\n\n");
                }
                catch (Exception logEx)
                {
                    _logger.LogWarning(logEx, "Could not write Gemini error to service.log (non-fatal).");
                }

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
