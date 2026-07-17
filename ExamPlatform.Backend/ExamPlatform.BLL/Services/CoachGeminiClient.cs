// CoachGeminiClient — the AI Learning Coach's only outbound call.
//
// Migrated from calling Gemini directly to calling it through the Presidio
// LLM Gateway (a LiteLLM Proxy) via its native Anthropic-format endpoint —
// see LlmGatewayProtocol.cs for how that shape (and specifically why
// `/v1/messages`, not the look-alike `/anthropic/v1/messages` pass-through,
// is the correct route for a LiteLLM virtual key) was confirmed live. The
// class name and the ICoachGeminiClient interface are unchanged on purpose:
// every other caller (LearningCoachService and its tests) is unaffected by
// this swap.
//
// Two rules govern this file, unchanged from the Gemini integration:
//
//   1. NOTHING SENSITIVE IS EVER LOGGED OR RETURNED. The API key is sent only
//      in the x-api-key request header — never in a log line, a URL, or an
//      exception message. The prompt (which contains the candidate's
//      performance context) and the raw model response are likewise never
//      logged. We log the endpoint, the status code, and a failure category.
//
//   2. EVERY FAILURE BECOMES AiCoachUnavailableException. Missing key, missing
//      base URL, timeout, 404, 429, 500, empty body, garbage body — all of
//      them collapse into one controlled exception with one fixed user-facing
//      message. The caller therefore has a single degraded path, and no raw
//      provider payload can leak to Angular.

// CA1848: standard ILogger extension methods used deliberately — see CertificateService.cs
#pragma warning disable CA1848
using System.Net.Http.Json;
using ExamPlatform.BLL.Exceptions;
using ExamPlatform.BLL.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ExamPlatform.BLL.Services
{
    public class CoachGeminiClient : ICoachGeminiClient
    {
        // Enough for a coaching answer plus a 7-day plan; low enough to bound cost per call.
        private const int MaxOutputTokens = 1400;

        // One retry, short pause. Provider-side outages (5xx) intermittently last a second
        // or two; a single spaced retry absorbs most of those without meaningfully raising
        // cost. 429 is deliberately NOT retried — retrying into an exhausted quota only
        // digs the hole deeper.
        private const int TransientRetryDelayMs = 1500;

        private readonly HttpClient _httpClient;
        private readonly ILogger<CoachGeminiClient> _logger;
        private readonly string? _apiKey;
        private readonly string? _baseUrl;
        private readonly string _model;

        public CoachGeminiClient(
            HttpClient httpClient, IConfiguration config, ILogger<CoachGeminiClient> logger)
        {
            _httpClient = httpClient;
            _logger = logger;

            // Read lazily into fields: a missing key/URL must degrade THIS
            // feature at call time, never prevent the application from starting.
            _apiKey = config["LLM:ApiKey"];
            _baseUrl = config["LLM:BaseUrl"];
            _model = string.IsNullOrWhiteSpace(config["LLM:Model"])
                ? LlmGatewayProtocol.DefaultModel
                : config["LLM:Model"]!;
        }

        public async Task<string> GenerateJsonAsync(
            string systemInstruction, string userContent, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(_apiKey) || string.IsNullOrWhiteSpace(_baseUrl))
            {
                // Deliberately NOT a startup failure: the rest of ExamHub runs fine without
                // a gateway key/URL configured; only the coach degrades.
                _logger.LogError("AI Coach: LLM Gateway is not configured (missing key or base URL); returning unavailable.");
                throw new AiCoachUnavailableException(CoachFailureCategory.Misconfigured);
            }

            var requestUrl = _baseUrl.TrimEnd('/') + LlmGatewayProtocol.MessagesPath;
            var requestBody = LlmGatewayProtocol.BuildRequestBody(
                systemInstruction, userContent, _model, MaxOutputTokens);

            var response = await SendWithOneRetryAsync(requestUrl, requestBody, cancellationToken);

            using (response)
            {
                if (!response.IsSuccessStatusCode)
                {
                    // Status code only. The provider's error body is not logged: it is not needed
                    // to diagnose the cases we care about (404 model, 429 quota, 401 key,
                    // 5xx outage) and keeping it out of the logs keeps the blast radius small.
                    _logger.LogWarning(
                        "AI Coach: LLM Gateway returned HTTP {StatusCode} from {Endpoint}.",
                        (int)response.StatusCode, requestUrl);
                    throw new AiCoachUnavailableException(CoachFailureCategory.HttpStatus);
                }

                var payload = await response.Content.ReadAsStringAsync(cancellationToken);

                var text = LlmGatewayProtocol.ExtractFirstText(payload);
                if (string.IsNullOrWhiteSpace(text))
                {
                    // 200 OK with no usable content — e.g. the model returned only a refusal
                    // with no text block, or an unexpected envelope shape.
                    _logger.LogWarning("AI Coach: LLM Gateway returned an empty response body.");
                    throw new AiCoachUnavailableException(CoachFailureCategory.EmptyContent);
                }

                try
                {
                    // Tolerant recovery, shared with the analytics integration: strips
                    // ```json fences, trims surrounding prose, repairs a missing closing brace.
                    return AgentService.ExtractJsonObject(text);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("AI Coach: LLM Gateway response did not contain a JSON object.");
                    throw new AiCoachUnavailableException(CoachFailureCategory.MalformedJson, ex);
                }
            }
        }

        /// <summary>
        /// Sends the request, retrying exactly once (after a short pause) when the failure is
        /// plausibly transient: a 5xx status from the provider, or a network-level send error.
        /// Everything else — 4xx (including 429 quota), timeout, caller cancellation — is
        /// returned/propagated immediately. Returns the FINAL response; the caller decides
        /// what a non-success status means. Builds a fresh HttpRequestMessage per attempt —
        /// a message instance can only be sent once.
        /// </summary>
        private async Task<HttpResponseMessage> SendWithOneRetryAsync(
            string requestUrl, object requestBody, CancellationToken cancellationToken)
        {
            var isRetry = false;

            while (true)
            {
                try
                {
                    var request = new HttpRequestMessage(HttpMethod.Post, requestUrl)
                    {
                        Content = JsonContent.Create(requestBody)
                    };
                    // Anthropic-style auth: the key lives ONLY in this header. It is
                    // never logged and never thrown.
                    request.Headers.Add("x-api-key", _apiKey);
                    request.Headers.Add("anthropic-version", LlmGatewayProtocol.AnthropicVersion);

                    var response = await _httpClient.SendAsync(request, cancellationToken);

                    var status = (int)response.StatusCode;
                    if (status >= 500 && !isRetry)
                    {
                        // Transient provider trouble (e.g. a momentary 503 "overloaded").
                        // Status code only in the log — never the body, never the key.
                        _logger.LogWarning(
                            "AI Coach: LLM Gateway returned HTTP {StatusCode}; retrying once after {DelayMs}ms.",
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
                    // HttpClient.Timeout elapsed. No retry: the timeout budget is already
                    // spent, and doubling the user's wait is worse than an honest "try again shortly".
                    _logger.LogWarning("AI Coach: LLM Gateway request timed out against {Endpoint}.", requestUrl);
                    throw new AiCoachUnavailableException(CoachFailureCategory.Timeout, ex);
                }
                catch (HttpRequestException ex)
                {
                    // DNS failure, connection refused, TLS error — often momentary, so it gets
                    // the same single retry as a 5xx.
                    if (!isRetry)
                    {
                        _logger.LogWarning(
                            "AI Coach: LLM Gateway request failed to reach {Endpoint}; retrying once.", requestUrl);
                        await Task.Delay(TransientRetryDelayMs, cancellationToken);
                        isRetry = true;
                        continue;
                    }

                    _logger.LogWarning("AI Coach: LLM Gateway request failed to reach {Endpoint}.", requestUrl);
                    throw new AiCoachUnavailableException(CoachFailureCategory.HttpStatus, ex);
                }
            }
        }
    }
}
