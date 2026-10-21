// CoachGeminiClientTests — the failure surface of the AI Learning Coach.
//
// The acceptance bar for this feature is "an AI outage must never break the application",
// so the bulk of these tests deliberately exercise Gemini behaving badly: 404, 429, 500,
// timeouts, empty bodies, truncated JSON, no key at all. Every one of them must collapse
// into a single controlled AiCoachUnavailableException — never a raw provider payload, and
// never the API key.

using System.Net;
using System.Text;
using System.Text.Json;
using ExamPlatform.BLL.Exceptions;
using ExamPlatform.BLL.Services;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace ExamPlatform.BLL.Tests.Services
{
    [TestFixture]
    public class CoachGeminiClientTests
    {
        private const string ApiKey = "SUPER-SECRET-COACH-KEY-XYZ";
        private const string TestEndpoint = "https://example.test/v1beta/models/gemini-flash-latest:generateContent";

        // Canned-response handler (same shape as the existing AgentServiceTests double) that
        // also records the request so we can assert on what actually went over the wire.
        private sealed class StubHandler : HttpMessageHandler
        {
            private readonly HttpStatusCode _status;
            private readonly string _body;
            private readonly Exception? _throw;

            public Uri? LastRequestUri { get; private set; }
            public string? LastRequestBody { get; private set; }

            public StubHandler(HttpStatusCode status, string body) { _status = status; _body = body; }
            public StubHandler(Exception toThrow) { _throw = toThrow; _status = HttpStatusCode.OK; _body = ""; }

            protected override async Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request, CancellationToken cancellationToken)
            {
                LastRequestUri = request.RequestUri;
                LastRequestBody = request.Content is null
                    ? null
                    : await request.Content.ReadAsStringAsync(cancellationToken);

                // Honour cancellation so the propagation test is meaningful.
                cancellationToken.ThrowIfCancellationRequested();

                if (_throw is not null) throw _throw;

                return new HttpResponseMessage(_status)
                {
                    Content = new StringContent(_body, Encoding.UTF8, "application/json")
                };
            }
        }

        // Returns a different canned response per call and counts the calls — used to prove
        // the transient-retry behaviour (one retry on 5xx, none on 4xx).
        private sealed class SequenceHandler : HttpMessageHandler
        {
            private readonly Queue<(HttpStatusCode Status, string Body)> _responses;
            public int CallCount { get; private set; }

            public SequenceHandler(params (HttpStatusCode Status, string Body)[] responses)
                => _responses = new Queue<(HttpStatusCode, string)>(responses);

            protected override Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request, CancellationToken cancellationToken)
            {
                CallCount++;
                var (status, body) = _responses.Count > 1 ? _responses.Dequeue() : _responses.Peek();
                return Task.FromResult(new HttpResponseMessage(status)
                {
                    Content = new StringContent(body, Encoding.UTF8, "application/json")
                });
            }
        }

        private static IConfiguration Config(string apiKey = ApiKey, string? apiUrl = TestEndpoint)
        {
            var dict = new Dictionary<string, string?> { ["Gemini:ApiKey"] = apiKey };
            if (apiUrl is not null) dict["Gemini:ApiUrl"] = apiUrl;
            return new ConfigurationBuilder().AddInMemoryCollection(dict).Build();
        }

        private static CoachGeminiClient BuildSut(StubHandler handler, IConfiguration? config = null)
            => new(new HttpClient(handler),
                   config ?? Config(),
                   new Mock<ILogger<CoachGeminiClient>>().Object);

        // Wraps model output in Gemini's response envelope.
        private static string Envelope(string modelText)
            => JsonSerializer.Serialize(new
            {
                candidates = new[]
                {
                    new { content = new { parts = new[] { new { text = modelText } } } }
                }
            });

        private static Task<string> Ask(CoachGeminiClient sut, CancellationToken ct = default)
            => sut.GenerateJsonAsync("You are the ExamHub AI Learning Coach.", "context + question", ct);

        // ════════════════════════════════════════════════════════════════
        //   HAPPY PATH
        // ════════════════════════════════════════════════════════════════

        [Test]
        public async Task GenerateJsonAsync_Success_ReturnsModelJson()
        {
            var handler = new StubHandler(HttpStatusCode.OK, Envelope("{\"answer\":\"Focus on LINQ.\"}"));

            var json = await Ask(BuildSut(handler));

            json.Should().Be("{\"answer\":\"Focus on LINQ.\"}");
            handler.LastRequestUri!.ToString().Should().Contain("gemini-flash-latest:generateContent");
        }

        [Test]
        public async Task GenerateJsonAsync_MarkdownFencedOrTruncatedJson_IsRecovered()
        {
            // gemini-flash-latest really does both of these (see AgentService's notes); the
            // coach reuses the same tolerant recovery rather than failing the request.
            var fencedAndTruncated = "```json\n{\"answer\":\"Focus on LINQ.\"\n```";
            var handler = new StubHandler(HttpStatusCode.OK, Envelope(fencedAndTruncated));

            var json = await Ask(BuildSut(handler));

            JsonDocument.Parse(json).RootElement.GetProperty("answer").GetString()
                .Should().Be("Focus on LINQ.");
        }

        [Test]
        public async Task GenerateJsonAsync_SendsSystemInstructionAndDisablesThinkingBudget()
        {
            var handler = new StubHandler(HttpStatusCode.OK, Envelope("{\"answer\":\"ok\"}"));

            await Ask(BuildSut(handler));

            var body = handler.LastRequestBody!;
            body.Should().Contain("system_instruction");
            body.Should().Contain("application/json", "we ask for structured output, not prose");
            body.Should().Contain("thinkingBudget");
        }

        // ════════════════════════════════════════════════════════════════
        //   SECRET HYGIENE
        // ════════════════════════════════════════════════════════════════

        [Test]
        public async Task GenerateJsonAsync_ApiKey_IsSentOnlyInTheQueryString_NeverInTheBody()
        {
            var handler = new StubHandler(HttpStatusCode.OK, Envelope("{\"answer\":\"ok\"}"));

            await Ask(BuildSut(handler));

            handler.LastRequestUri!.Query.Should().Contain(ApiKey, "the provider authenticates via ?key=");
            handler.LastRequestBody.Should().NotContain(ApiKey, "the key has no business being in the prompt");
        }

        [Test]
        public async Task GenerateJsonAsync_ProviderError_ExceptionMessageNeverLeaksTheKeyOrTheBody()
        {
            // A real provider error body, which we must not relay to the client.
            var errorBody = "{\"error\":{\"code\":429,\"message\":\"Quota exceeded for project 12345\"}}";
            var handler = new StubHandler(HttpStatusCode.TooManyRequests, errorBody);

            var act = async () => await Ask(BuildSut(handler));

            var thrown = await act.Should().ThrowAsync<AiCoachUnavailableException>();
            thrown.Which.Message.Should().NotContain(ApiKey);
            thrown.Which.Message.Should().NotContain("Quota exceeded");
            thrown.Which.Message.Should().NotContain("12345");
            thrown.Which.Message.Should().Be(AiCoachUnavailableException.UserMessage,
                "every failure shows the same safe message");
        }

        // ════════════════════════════════════════════════════════════════
        //   FAILURE MODES — all collapse to one controlled exception
        // ════════════════════════════════════════════════════════════════

        [Test]
        public async Task GenerateJsonAsync_MissingApiKey_IsUnavailable_NotAStartupFailure()
        {
            var handler = new StubHandler(HttpStatusCode.OK, Envelope("{\"answer\":\"ok\"}"));
            var sut = BuildSut(handler, Config(apiKey: ""));   // key absent from configuration

            var act = async () => await Ask(sut);

            var thrown = await act.Should().ThrowAsync<AiCoachUnavailableException>();
            thrown.Which.Category.Should().Be(CoachFailureCategory.Misconfigured);
        }

        [TestCase(HttpStatusCode.NotFound)]            // model retired / wrong endpoint
        [TestCase(HttpStatusCode.TooManyRequests)]     // quota exhausted
        [TestCase(HttpStatusCode.InternalServerError)] // provider outage
        [TestCase(HttpStatusCode.Unauthorized)]        // key rejected
        public async Task GenerateJsonAsync_ProviderHttpError_IsUnavailable(HttpStatusCode status)
        {
            var handler = new StubHandler(status, "{\"error\":{\"message\":\"boom\"}}");

            var act = async () => await Ask(BuildSut(handler));

            var thrown = await act.Should().ThrowAsync<AiCoachUnavailableException>();
            thrown.Which.Category.Should().Be(CoachFailureCategory.HttpStatus);
        }

        [Test]
        public async Task GenerateJsonAsync_Timeout_IsUnavailable()
        {
            // HttpClient surfaces its own timeout as a TaskCanceledException with no
            // cancellation requested by us.
            var handler = new StubHandler(new TaskCanceledException("The request timed out."));

            var act = async () => await Ask(BuildSut(handler));

            var thrown = await act.Should().ThrowAsync<AiCoachUnavailableException>();
            thrown.Which.Category.Should().Be(CoachFailureCategory.Timeout);
        }

        [Test]
        public async Task GenerateJsonAsync_ProviderUnreachable_IsUnavailable()
        {
            var handler = new StubHandler(new HttpRequestException("Name or service not known"));

            var act = async () => await Ask(BuildSut(handler));

            var thrown = await act.Should().ThrowAsync<AiCoachUnavailableException>();
            thrown.Which.Category.Should().Be(CoachFailureCategory.HttpStatus);
        }

        [Test]
        public async Task GenerateJsonAsync_EmptyResponseText_IsUnavailable()
        {
            var handler = new StubHandler(HttpStatusCode.OK, Envelope(""));

            var act = async () => await Ask(BuildSut(handler));

            var thrown = await act.Should().ThrowAsync<AiCoachUnavailableException>();
            thrown.Which.Category.Should().Be(CoachFailureCategory.EmptyContent);
        }

        [Test]
        public async Task GenerateJsonAsync_NoCandidatesInEnvelope_IsUnavailable()
        {
            // e.g. the model returned only a safety block.
            var handler = new StubHandler(HttpStatusCode.OK, "{\"promptFeedback\":{\"blockReason\":\"SAFETY\"}}");

            var act = async () => await Ask(BuildSut(handler));

            var thrown = await act.Should().ThrowAsync<AiCoachUnavailableException>();
            thrown.Which.Category.Should().Be(CoachFailureCategory.EmptyContent);
        }

        [Test]
        public async Task GenerateJsonAsync_ResponseIsNotJsonAtAll_IsUnavailable()
        {
            var handler = new StubHandler(HttpStatusCode.OK, Envelope("I'm sorry, I can't help with that."));

            var act = async () => await Ask(BuildSut(handler));

            var thrown = await act.Should().ThrowAsync<AiCoachUnavailableException>();
            thrown.Which.Category.Should().Be(CoachFailureCategory.MalformedJson);
        }

        [Test]
        public async Task GenerateJsonAsync_UnparseableProviderPayload_IsUnavailable()
        {
            var handler = new StubHandler(HttpStatusCode.OK, "<html>502 Bad Gateway</html>");

            var act = async () => await Ask(BuildSut(handler));

            var thrown = await act.Should().ThrowAsync<AiCoachUnavailableException>();
            thrown.Which.Category.Should().Be(CoachFailureCategory.EmptyContent);
        }

        // ════════════════════════════════════════════════════════════════
        //   TRANSIENT RETRY — one spaced retry on 5xx, never on 4xx
        // ════════════════════════════════════════════════════════════════

        [Test]
        public async Task GenerateJsonAsync_TransientServerError_IsRetriedOnceAndSucceeds()
        {
            // The exact live scenario: Google's endpoint returns 503 "overloaded" for a
            // moment, then recovers. One retry turns a user-visible failure into a success.
            var handler = new SequenceHandler(
                (HttpStatusCode.ServiceUnavailable, "{\"error\":{\"code\":503,\"message\":\"overloaded\"}}"),
                (HttpStatusCode.OK, Envelope("{\"answer\":\"Recovered.\"}")));
            var sut = new CoachGeminiClient(new HttpClient(handler), Config(),
                new Mock<ILogger<CoachGeminiClient>>().Object);

            var json = await sut.GenerateJsonAsync("system", "content", CancellationToken.None);

            json.Should().Be("{\"answer\":\"Recovered.\"}");
            handler.CallCount.Should().Be(2);
        }

        [Test]
        public async Task GenerateJsonAsync_ServerErrorPersists_FailsAfterExactlyOneRetry()
        {
            var handler = new SequenceHandler(
                (HttpStatusCode.ServiceUnavailable, "{\"error\":\"down\"}"),
                (HttpStatusCode.ServiceUnavailable, "{\"error\":\"still down\"}"));
            var sut = new CoachGeminiClient(new HttpClient(handler), Config(),
                new Mock<ILogger<CoachGeminiClient>>().Object);

            var act = async () => await sut.GenerateJsonAsync("system", "content", CancellationToken.None);

            var thrown = await act.Should().ThrowAsync<AiCoachUnavailableException>();
            thrown.Which.Category.Should().Be(CoachFailureCategory.HttpStatus);
            handler.CallCount.Should().Be(2, "exactly one retry — the coach must stay snappy, not loop");
        }

        [TestCase(HttpStatusCode.TooManyRequests)]  // quota: retrying makes it worse
        [TestCase(HttpStatusCode.NotFound)]         // config problem: retrying can't fix it
        public async Task GenerateJsonAsync_ClientError_IsNeverRetried(HttpStatusCode status)
        {
            var handler = new SequenceHandler((status, "{\"error\":\"nope\"}"));
            var sut = new CoachGeminiClient(new HttpClient(handler), Config(),
                new Mock<ILogger<CoachGeminiClient>>().Object);

            var act = async () => await sut.GenerateJsonAsync("system", "content", CancellationToken.None);

            await act.Should().ThrowAsync<AiCoachUnavailableException>();
            handler.CallCount.Should().Be(1);
        }

        // ════════════════════════════════════════════════════════════════
        //   CANCELLATION
        // ════════════════════════════════════════════════════════════════

        [Test]
        public async Task GenerateJsonAsync_CallerCancels_PropagatesCancellation()
        {
            // A cancelled request is NOT an AI outage — the caller went away. It must unwind
            // as OperationCanceledException rather than being reported as "AI unavailable".
            var handler = new StubHandler(HttpStatusCode.OK, Envelope("{\"answer\":\"ok\"}"));
            using var cts = new CancellationTokenSource();
            await cts.CancelAsync();

            var act = async () => await Ask(BuildSut(handler), cts.Token);

            await act.Should().ThrowAsync<OperationCanceledException>();
        }
    }
}
