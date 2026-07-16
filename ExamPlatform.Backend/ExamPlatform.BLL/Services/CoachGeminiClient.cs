// CoachGeminiClient — the AI Learning Coach's only outbound call.
//
// Two rules govern this file:
//
//   1. NOTHING SENSITIVE IS EVER LOGGED OR RETURNED. The API key is appended to the request
//      URL and to nothing else — not to a log line, not to an exception message. The prompt
//      (which contains the candidate's performance context) and the raw model response are
//      likewise never logged. We log the endpoint, the status code, and a failure category.
//
//   2. EVERY FAILURE BECOMES AiCoachUnavailableException. Missing key, timeout, 404, 429,
//      500, empty body, garbage body — all of them collapse into one controlled exception
//      with one fixed user-facing message. The caller therefore has a single degraded path,
//      and no raw provider payload can leak to Angular.
//
// Gemini specifics reused from the proven AgentService integration: gemini-flash-latest is
// a thinking model, so thinkingBudget=0 keeps structured JSON fast and reliable; and
// AgentService.ExtractJsonObject recovers the object when the model fences it in markdown
// or drops the closing brace.

// CA1848: standard ILogger extension methods used deliberately — see CertificateService.cs
#pragma warning disable CA1848
using System.Net.Http.Json;
using System.Text.Json;
using ExamPlatform.BLL.Exceptions;
using ExamPlatform.BLL.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ExamPlatform.BLL.Services
{
    public class CoachGeminiClient : ICoachGeminiClient
    {
        // Default endpoint matches AgentService: an always-current alias. The retired
        // gemini-2.5-flash 404s for new API keys.
        private const string DefaultApiUrl =
            "https://generativelanguage.googleapis.com/v1beta/models/gemini-flash-latest:generateContent";

        // Enough for a coaching answer plus a 7-day plan; low enough to bound cost per call.
        private const int MaxOutputTokens = 1400;

        // One retry, short pause. Google's generateContent endpoint intermittently returns
        // 503 "overloaded" for a second or two (observed live); a single spaced retry absorbs
        // most of those without meaningfully raising cost. 429 is deliberately NOT retried —
        // retrying into an exhausted quota only digs the hole deeper.
        private const int TransientRetryDelayMs = 1500;

        private readonly HttpClient _httpClient;
        private readonly ILogger<CoachGeminiClient> _logger;
        private readonly string? _apiKey;
        private readonly string _apiUrl;

        public CoachGeminiClient(
            HttpClient httpClient, IConfiguration config, ILogger<CoachGeminiClient> logger)
        {
            _httpClient = httpClient;
            _logger = logger;

            // Same configuration keys as the existing integration — one key, one place.
            // Read lazily into a field: a missing key must degrade THIS feature at call time,
            // never prevent the application from starting.
            _apiKey = config["Gemini:ApiKey"];
            _apiUrl = string.IsNullOrWhiteSpace(config["Gemini:ApiUrl"])
                ? DefaultApiUrl
                : config["Gemini:ApiUrl"]!;
        }

        public async Task<string> GenerateJsonAsync(
            string systemInstruction, string userContent, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(_apiKey))
            {
                // Deliberately NOT a startup failure: the rest of ExamHub runs fine without
                // a Gemini key; only the coach degrades.
                _logger.LogError("AI Coach: Gemini API key is not configured; returning unavailable.");
                throw new AiCoachUnavailableException(CoachFailureCategory.Misconfigured);
            }

            var requestBody = new
            {
                system_instruction = new
                {
                    parts = new[] { new { text = systemInstruction } }
                },
                contents = new[]
                {
                    new { parts = new[] { new { text = userContent } } }
                },
                generationConfig = new
                {
                    response_mime_type = "application/json",
                    // See AgentService: with thinking left on, this model spends its output
                    // budget on hidden reasoning and intermittently truncates the JSON.
                    thinkingConfig = new { thinkingBudget = 0 },
                    temperature = 0.4,
                    maxOutputTokens = MaxOutputTokens
                }
            };

            // The key lives ONLY in this local. It is never logged and never thrown.
            var requestUrl = $"{_apiUrl}?key={_apiKey}";

            var response = await SendWithOneRetryAsync(requestUrl, requestBody, cancellationToken);

            using (response)
            {
                if (!response.IsSuccessStatusCode)
                {
                    // Status code only. The provider's error body is not logged: it is not needed
                    // to diagnose the four cases we care about (404 model, 429 quota, 401 key,
                    // 5xx outage) and keeping it out of the logs keeps the blast radius small.
                    _logger.LogWarning(
                        "AI Coach: Gemini returned HTTP {StatusCode} from {Endpoint}.",
                        (int)response.StatusCode, _apiUrl);
                    throw new AiCoachUnavailableException(CoachFailureCategory.HttpStatus);
                }

                var payload = await response.Content.ReadAsStringAsync(cancellationToken);

                var text = ExtractText(payload);
                if (string.IsNullOrWhiteSpace(text))
                {
                    // 200 OK with no usable content — e.g. the model returned only a safety
                    // block or an empty candidate list.
                    _logger.LogWarning("AI Coach: Gemini returned an empty response body.");
                    throw new AiCoachUnavailableException(CoachFailureCategory.EmptyContent);
                }

                try
                {
                    // Tolerant recovery, shared with the proven analytics integration: strips
                    // ```json fences, trims surrounding prose, repairs a missing closing brace.
                    return AgentService.ExtractJsonObject(text);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("AI Coach: Gemini response did not contain a JSON object.");
                    throw new AiCoachUnavailableException(CoachFailureCategory.MalformedJson, ex);
                }
            }
        }

        /// <summary>
        /// Sends the request, retrying exactly once (after a short pause) when the failure is
        /// plausibly transient: a 5xx status from the provider, or a network-level send error.
        /// Everything else — 4xx (including 429 quota), timeout, caller cancellation — is
        /// returned/propagated immediately. Returns the FINAL response; the caller decides
        /// what a non-success status means.
        /// </summary>
        private async Task<HttpResponseMessage> SendWithOneRetryAsync(
            string requestUrl, object requestBody, CancellationToken cancellationToken)
        {
            var isRetry = false;

            while (true)
            {
                try
                {
                    var response = await _httpClient.PostAsJsonAsync(requestUrl, requestBody, cancellationToken);

                    var status = (int)response.StatusCode;
                    if (status >= 500 && !isRetry)
                    {
                        // Transient provider trouble (503 "model overloaded" is the classic).
                        // Status code only in the log — never the body, never the URL's key.
                        _logger.LogWarning(
                            "AI Coach: Gemini returned HTTP {StatusCode}; retrying once after {DelayMs}ms.",
                            status, TransientRetryDelayMs);
                        response.Dispose();
                        await Task.Delay(TransientRetryDelayMs, cancellationToken);
                        isRetry = true;
                        continue;
                    }

                    return response;
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    // The CALLER cancelled (client disconnected / request aborted). That is not
                    // an AI outage — propagate it so the request pipeline can unwind normally.
                    throw;
                }
                catch (OperationCanceledException ex)
                {
                    // HttpClient.Timeout elapsed. No retry: the 20s budget is already spent, and
                    // making the user wait up to 40s is worse than an honest "try again shortly".
                    _logger.LogWarning("AI Coach: Gemini request timed out against {Endpoint}.", _apiUrl);
                    throw new AiCoachUnavailableException(CoachFailureCategory.Timeout, ex);
                }
                catch (HttpRequestException ex)
                {
                    // DNS failure, connection refused, TLS error — often momentary, so it gets
                    // the same single retry as a 5xx.
                    if (!isRetry)
                    {
                        _logger.LogWarning(
                            "AI Coach: Gemini request failed to reach {Endpoint}; retrying once.", _apiUrl);
                        await Task.Delay(TransientRetryDelayMs, cancellationToken);
                        isRetry = true;
                        continue;
                    }

                    _logger.LogWarning("AI Coach: Gemini request failed to reach {Endpoint}.", _apiUrl);
                    throw new AiCoachUnavailableException(CoachFailureCategory.HttpStatus, ex);
                }
            }
        }

        /// <summary>
        /// Pulls candidates[0].content.parts[0].text out of Gemini's envelope, returning null
        /// if the envelope is not the shape we expect. Never throws — a surprising envelope is
        /// just "no content" to the caller.
        /// </summary>
        private static string? ExtractText(string payload)
        {
            try
            {
                using var document = JsonDocument.Parse(payload);

                if (!document.RootElement.TryGetProperty("candidates", out var candidates) ||
                    candidates.ValueKind != JsonValueKind.Array ||
                    candidates.GetArrayLength() == 0)
                {
                    return null;
                }

                if (!candidates[0].TryGetProperty("content", out var content) ||
                    !content.TryGetProperty("parts", out var parts) ||
                    parts.ValueKind != JsonValueKind.Array ||
                    parts.GetArrayLength() == 0 ||
                    !parts[0].TryGetProperty("text", out var textElement))
                {
                    return null;
                }

                return textElement.GetString();
            }
            catch (JsonException)
            {
                return null;
            }
        }
    }
}
