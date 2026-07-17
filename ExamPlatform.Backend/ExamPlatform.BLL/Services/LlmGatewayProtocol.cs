// LlmGatewayProtocol — the wire format for the Presidio LLM Gateway's native
// Anthropic-format endpoint.
//
// Confirmed LIVE against the running gateway (not guessed): the gateway is a
// LiteLLM Proxy (v1.90.1, per its own /openapi.json). It exposes TWO
// Anthropic-shaped routes that look similar but behave very differently:
//
//   - `{BaseUrl}/anthropic/v1/messages` — a raw PASS-THROUGH. It forwards
//     whatever key you send straight to the real upstream provider
//     unmodified. A LiteLLM *virtual* key is not a real upstream credential,
//     so this route 401s with the upstream's own "invalid x-api-key" /
//     "Invalid bearer token" error — confirmed live, this is not a guess.
//   - `{BaseUrl}/v1/messages` (no `/anthropic` prefix) — LiteLLM's NATIVE
//     Anthropic-format endpoint. It validates the caller's virtual key
//     against LiteLLM's own key store, then dispatches to whatever backend
//     model/credentials the proxy admin configured (this deployment's
//     backend is AWS Bedrock, visible in the `msg_bdrk_...` response ids).
//     This is the one that actually works with a virtual key and is what
//     this class uses.
//
// Both request and response bodies are the standard Anthropic Messages API
// shape either way. CoachGeminiClient and AgentService both speak this same
// shape — kept in exactly one place so it's defined once, not duplicated.

using System.Text.Json;

namespace ExamPlatform.BLL.Services
{
    internal static class LlmGatewayProtocol
    {
        public const string MessagesPath = "/v1/messages";
        public const string AnthropicVersion = "2023-06-01";

        // The approved default model for this migration. Overridable via the
        // optional "LLM:Model" config key; never hardcoded elsewhere.
        public const string DefaultModel = "claude-sonnet-4-6";

        /// <summary>
        /// Builds an Anthropic Messages API request body. `system` is omitted
        /// entirely when there is no system instruction, matching Anthropic's
        /// own convention (it is optional, not an empty string).
        /// </summary>
        public static object BuildRequestBody(
            string? systemInstruction, string userContent, string model, int maxTokens)
        {
            var messages = new[] { new { role = "user", content = userContent } };

            return string.IsNullOrEmpty(systemInstruction)
                ? new { model, max_tokens = maxTokens, messages }
                : (object)new { model, max_tokens = maxTokens, system = systemInstruction, messages };
        }

        /// <summary>
        /// Extracts the first text content block's text from an Anthropic
        /// Messages API response body (`{"content":[{"type":"text","text":"..."}],...}`).
        /// Returns null — never throws — if the shape doesn't match (empty
        /// content array, non-text block, malformed JSON); callers convert
        /// that into their own "no usable content" handling.
        /// </summary>
        public static string? ExtractFirstText(string responseBody)
        {
            try
            {
                using var doc = JsonDocument.Parse(responseBody);
                if (!doc.RootElement.TryGetProperty("content", out var content) ||
                    content.ValueKind != JsonValueKind.Array)
                    return null;

                foreach (var block in content.EnumerateArray())
                {
                    if (block.TryGetProperty("type", out var type) &&
                        type.ValueKind == JsonValueKind.String &&
                        type.GetString() == "text" &&
                        block.TryGetProperty("text", out var text))
                    {
                        return text.GetString();
                    }
                }
                return null;
            }
            catch (JsonException)
            {
                return null;
            }
        }
    }
}
