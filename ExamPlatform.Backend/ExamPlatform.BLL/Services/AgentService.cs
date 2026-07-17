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
        // Question categorization can cover many questions in one call — give
        // it more headroom than the coach's single-answer budget so a large
        // exam's {questionId: category} map is never truncated.
        private const int MaxOutputTokens = 4096;

        private readonly HttpClient _httpClient;
        private readonly ILogger<AgentService> _logger;
        private readonly IConfiguration _config;
        private readonly string? _apiKey;
        private readonly string? _baseUrl;
        private readonly string _model;

        public AgentService(HttpClient httpClient, IConfiguration config, ILogger<AgentService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
            _config = config;
            // Migrated from calling Gemini directly to calling it through the
            // Presidio LLM Gateway (Anthropic-compatible pass-through) — see
            // LlmGatewayProtocol.cs for how the wire format was confirmed.
            _apiKey = config["LLM:ApiKey"];
            _baseUrl = config["LLM:BaseUrl"];
            _model = string.IsNullOrWhiteSpace(config["LLM:Model"])
                ? LlmGatewayProtocol.DefaultModel
                : config["LLM:Model"]!;
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
            if (string.IsNullOrWhiteSpace(_apiKey) || string.IsNullOrWhiteSpace(_baseUrl))
            {
                _logger.LogError("LLM Gateway is not configured (missing key or base URL).");
                throw new Exception("LLM Gateway is not configured. Failing instead of falling back.");
            }

            var questionDetails = examQuestions.Select(eq => new
            {
                Id = eq.QuestionId,
                Text = eq.Question.QuestionText
            }).ToList();

            var promptTemplatePath = ResolvePromptPath(_config["Gemini:PromptFilePath"]);
            var promptTemplate = await File.ReadAllTextAsync(promptTemplatePath);
            var prompt = string.Format(promptTemplate, JsonSerializer.Serialize(questionDetails));

            // Migrated from calling Gemini's generateContent directly to calling
            // the Presidio LLM Gateway's Anthropic-compatible pass-through — see
            // LlmGatewayProtocol.cs. The prompt itself (built above from the
            // unchanged template file) is sent as-is as the user message; no
            // system instruction is needed for this call.
            var requestBody = LlmGatewayProtocol.BuildRequestBody(
                systemInstruction: null, userContent: prompt, model: _model, maxTokens: MaxOutputTokens);

            try
            {
                var requestUrl = _baseUrl.TrimEnd('/') + LlmGatewayProtocol.MessagesPath;

                // NOTE: the API key is sent only in the x-api-key header, which is
                // NEVER logged. Diagnostics below log requestUrl (no key) and the
                // safe response body only.
                var request = new HttpRequestMessage(HttpMethod.Post, requestUrl)
                {
                    Content = JsonContent.Create(requestBody)
                };
                request.Headers.Add("x-api-key", _apiKey);
                request.Headers.Add("anthropic-version", LlmGatewayProtocol.AnthropicVersion);

                var response = await _httpClient.SendAsync(request);

                if (!response.IsSuccessStatusCode)
                {
                    // Read the provider's error payload so failures are diagnosable.
                    // The gateway's error body does not contain the API key.
                    var errorBody = await response.Content.ReadAsStringAsync();
                    _logger.LogError(
                        "LLM Gateway request failed: HTTP {StatusCode} from endpoint {Endpoint}. Response: {Body}",
                        (int)response.StatusCode, requestUrl, Truncate(errorBody, 500));
                    throw new HttpRequestException(
                        $"LLM Gateway returned HTTP {(int)response.StatusCode} for endpoint '{requestUrl}'. Response: {Truncate(errorBody, 500)}");
                }

                var jsonResponse = await response.Content.ReadAsStringAsync();

                // Parse the Anthropic Messages API response structure.
                var textResult = LlmGatewayProtocol.ExtractFirstText(jsonResponse);

                if (string.IsNullOrWhiteSpace(textResult))
                {
                    throw new Exception("Empty response from LLM Gateway.");
                }

                // Parse tolerantly: the model can wrap the JSON in markdown fences,
                // add surrounding prose, or (observed) omit the closing brace. See
                // ExtractJsonObject — it recovers a well-formed object from all three.
                var categories = JsonSerializer.Deserialize<Dictionary<string, string>>(
                    ExtractJsonObject(textResult));

                if (categories == null) throw new Exception("LLM Gateway returned null categories.");

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
                _logger.LogError(ex, "Failed to call LLM Gateway. Failing instead of falling back.");

                // Best-effort append to BLL/service.log. This path is CWD-relative and
                // may not exist under every host (e.g. the test runner), so a secondary
                // IO failure here must never mask the real error we're rethrowing.
                try
                {
                    var contentRoot = Directory.GetCurrentDirectory();
                    var logPath = Path.GetFullPath(Path.Combine(contentRoot, "..", "ExamPlatform.BLL", "service.log"));
                    await File.AppendAllTextAsync(logPath, $"[{DateTime.UtcNow:O}] LLM GATEWAY API ERROR: {ex.Message}\n{ex.StackTrace}\n\n");
                }
                catch (Exception logEx)
                {
                    _logger.LogWarning(logEx, "Could not write LLM Gateway error to service.log (non-fatal).");
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
