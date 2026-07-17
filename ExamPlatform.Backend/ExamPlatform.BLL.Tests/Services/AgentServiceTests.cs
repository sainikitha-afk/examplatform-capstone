// AgentServiceTests.cs — focused tests for the question-categorization call
// (now against the Presidio LLM Gateway's Anthropic-compatible pass-through —
// see LlmGatewayProtocol.cs) and the Gemini-era prompt-path resolution fix,
// which is unaffected by the provider swap: the default path must resolve
// against the binary's output directory (where the build copies
// Assets/gemini-prompt.txt) rather than the process working directory.

using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using ExamPlatform.BLL.Services;
using ExamPlatform.Domain.Entities;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace ExamPlatform.BLL.Tests.Services
{
    [TestFixture]
    public class AgentServiceTests
    {
        // ── Test doubles for the LLM Gateway HTTP integration ─────────────
        // A stub handler returns a canned response and records the request
        // (URI, headers) so we can assert on the endpoint/model actually called.
        private sealed class StubHandler : HttpMessageHandler
        {
            private readonly HttpStatusCode _status;
            private readonly string _body;
            public Uri? LastRequestUri { get; private set; }
            public HttpRequestHeaders? LastRequestHeaders { get; private set; }

            public StubHandler(HttpStatusCode status, string body)
            {
                _status = status;
                _body = body;
            }

            protected override Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request, CancellationToken cancellationToken)
            {
                LastRequestUri = request.RequestUri;
                LastRequestHeaders = request.Headers;
                return Task.FromResult(new HttpResponseMessage(_status)
                {
                    Content = new StringContent(_body, Encoding.UTF8, "application/json")
                });
            }
        }

        private static IConfiguration BuildConfig(string apiKey, string? baseUrl = "https://example.test")
        {
            var dict = new Dictionary<string, string?> { ["LLM:ApiKey"] = apiKey };
            if (baseUrl != null) dict["LLM:BaseUrl"] = baseUrl;
            return new ConfigurationBuilder().AddInMemoryCollection(dict).Build();
        }

        // Wraps the model's JSON output in the Anthropic Messages API response envelope.
        private static string AnthropicEnvelope(string modelJsonText)
            => JsonSerializer.Serialize(new
            {
                content = new[] { new { type = "text", text = modelJsonText } }
            });

        private static List<ExamQuestion> SampleQuestions() => new()
        {
            new ExamQuestion { QuestionId = 1, Question = new Question { QuestionId = 1, QuestionText = "What is 2 + 2?" } },
            new ExamQuestion { QuestionId = 2, Question = new Question { QuestionId = 2, QuestionText = "Area of a circle?" } },
        };

        private static AgentService BuildSut(StubHandler handler, IConfiguration config)
            => new AgentService(new HttpClient(handler), config, new Mock<ILogger<AgentService>>().Object);

        [Test]
        public async Task CategorizeQuestionsAsync_SuccessResponse_MapsCategoriesByIntId()
        {
            var handler = new StubHandler(HttpStatusCode.OK,
                AnthropicEnvelope("{\"1\":\"Arithmetic\",\"2\":\"Geometry\"}"));
            var config = BuildConfig("SECRET-KEY-123", "https://example.test");
            var sut = BuildSut(handler, config);

            var result = await sut.CategorizeQuestionsAsync(SampleQuestions());

            result[1].Should().Be("Arithmetic");
            result[2].Should().Be("Geometry");
            // The gateway's Anthropic pass-through endpoint is the one actually called.
            handler.LastRequestUri!.ToString().Should().Be("https://example.test/v1/messages");
        }

        [Test]
        public async Task CategorizeQuestionsAsync_SendsApiKeyHeaderAndDefaultModel()
        {
            var handler = new StubHandler(HttpStatusCode.OK, AnthropicEnvelope("{\"1\":\"A\",\"2\":\"B\"}"));
            var config = BuildConfig("SECRET-KEY-123");
            var sut = BuildSut(handler, config);

            await sut.CategorizeQuestionsAsync(SampleQuestions());

            handler.LastRequestHeaders!.GetValues("x-api-key").Should().Contain("SECRET-KEY-123");
            handler.LastRequestHeaders!.GetValues("anthropic-version").Should().Contain("2023-06-01");
        }

        [Test]
        public async Task CategorizeQuestionsAsync_NonSuccessResponse_ThrowsWithStatusAndNeverLeaksKey()
        {
            const string apiKey = "SUPER-SECRET-KEY-XYZ";
            var errorBody = "{\"error\":{\"type\":\"not_found_error\",\"message\":\"model not found\"}}";
            var handler = new StubHandler(HttpStatusCode.NotFound, errorBody);
            var config = BuildConfig(apiKey, "https://example.test");
            var sut = BuildSut(handler, config);

            var act = async () => await sut.CategorizeQuestionsAsync(SampleQuestions());

            var thrown = await act.Should().ThrowAsync<HttpRequestException>();
            thrown.Which.Message.Should().Contain("404");
            // The API key must NEVER appear in any diagnostic/exception message.
            thrown.Which.Message.Should().NotContain(apiKey);
        }

        [Test]
        public async Task CategorizeQuestionsAsync_MissingApiKey_Throws()
        {
            var handler = new StubHandler(HttpStatusCode.OK, AnthropicEnvelope("{}"));
            var sut = BuildSut(handler, BuildConfig("")); // no key configured

            var act = async () => await sut.CategorizeQuestionsAsync(SampleQuestions());

            await act.Should().ThrowAsync<Exception>();
        }

        [Test]
        public async Task CategorizeQuestionsAsync_MissingBaseUrl_Throws()
        {
            var handler = new StubHandler(HttpStatusCode.OK, AnthropicEnvelope("{}"));
            var sut = BuildSut(handler, BuildConfig("SOME-KEY", baseUrl: null)); // no base URL configured

            var act = async () => await sut.CategorizeQuestionsAsync(SampleQuestions());

            await act.Should().ThrowAsync<Exception>();
        }

        [Test]
        public async Task CategorizeQuestionsAsync_MalformedResponse_Throws()
        {
            var handler = new StubHandler(HttpStatusCode.OK, "{\"unexpected\":true}");
            var config = BuildConfig("KEY", "https://example.test");
            var sut = BuildSut(handler, config);

            var act = async () => await sut.CategorizeQuestionsAsync(SampleQuestions());

            await act.Should().ThrowAsync<Exception>();
        }

        [Test]
        public async Task CategorizeQuestionsAsync_ResponseMissingClosingBrace_StillRecovers()
        {
            // Reproduces the real defect observed with the previous provider: the model
            // intermittently returns the answer JSON with the closing brace omitted.
            // Tolerant parsing must recover it regardless of provider.
            var truncated = "{\n  \"1\": \"Arithmetic\",\n  \"2\": \"Geometry\"";
            var handler = new StubHandler(HttpStatusCode.OK, AnthropicEnvelope(truncated));
            var config = BuildConfig("KEY", "https://example.test");
            var sut = BuildSut(handler, config);

            var result = await sut.CategorizeQuestionsAsync(SampleQuestions());

            result[1].Should().Be("Arithmetic");
            result[2].Should().Be("Geometry");
        }

        // ── ExtractJsonObject: tolerant JSON recovery ─────────────────────
        [Test]
        public void ExtractJsonObject_CleanJson_ReturnedAsIs()
        {
            AgentService.ExtractJsonObject("{\"1\":\"A\",\"2\":\"B\"}")
                .Should().Be("{\"1\":\"A\",\"2\":\"B\"}");
        }

        [Test]
        public void ExtractJsonObject_MissingClosingBrace_IsRepaired()
        {
            var repaired = AgentService.ExtractJsonObject("{\n  \"1\": \"A\",\n  \"2\": \"B\"");
            var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(repaired);
            dict!["1"].Should().Be("A");
            dict["2"].Should().Be("B");
        }

        [Test]
        public void ExtractJsonObject_MarkdownFenced_IsStripped()
        {
            var repaired = AgentService.ExtractJsonObject("```json\n{\"1\":\"A\"}\n```");
            JsonSerializer.Deserialize<Dictionary<string, string>>(repaired)!["1"].Should().Be("A");
        }

        [Test]
        public void ExtractJsonObject_SurroundingProse_IsTrimmed()
        {
            var repaired = AgentService.ExtractJsonObject("Here is the mapping: {\"1\":\"A\"} — done.");
            JsonSerializer.Deserialize<Dictionary<string, string>>(repaired)!["1"].Should().Be("A");
        }

        [Test]
        public void ExtractJsonObject_BraceInsideStringValue_NotMiscounted()
        {
            var repaired = AgentService.ExtractJsonObject("{\"1\":\"A } B\"}");
            JsonSerializer.Deserialize<Dictionary<string, string>>(repaired)!["1"].Should().Be("A } B");
        }

        [Test]
        public void ExtractJsonObject_NoObject_Throws()
        {
            var act = () => AgentService.ExtractJsonObject("no json here");
            act.Should().Throw<Exception>();
        }

        [Test]
        public void ResolvePromptPath_NoConfiguredPath_ResolvesToAnExistingFile()
        {
            // With no Gemini:PromptFilePath configured, the default resolves under
            // AppContext.BaseDirectory/Assets. The BLL csproj copies the prompt to
            // output, which flows to this test project's output too — so the file
            // must exist at the resolved path regardless of the working directory.
            var resolved = AgentService.ResolvePromptPath(null);

            File.Exists(resolved).Should().BeTrue(
                $"the prompt template should be copied to the output directory; resolved to '{resolved}'");
        }

        [Test]
        public void ResolvePromptPath_NoConfiguredPath_UsesAppBaseDirectory()
        {
            var resolved = AgentService.ResolvePromptPath(null);

            resolved.Should().StartWith(AppContext.BaseDirectory);
            resolved.Should().EndWith(Path.Combine("Assets", "gemini-prompt.txt"));
        }

        [Test]
        public void ResolvePromptPath_ConfiguredPath_IsHonoredAsIs()
        {
            var custom = Path.Combine(Path.GetTempPath(), "my-custom-prompt.txt");

            var resolved = AgentService.ResolvePromptPath(custom);

            resolved.Should().Be(custom);
        }

        [Test]
        public void ResolvePromptPath_BlankConfiguredPath_FallsBackToDefault()
        {
            // Whitespace/empty is treated as "not configured" so a stray blank
            // setting doesn't break resolution.
            var resolved = AgentService.ResolvePromptPath("   ");

            resolved.Should().EndWith(Path.Combine("Assets", "gemini-prompt.txt"));
            File.Exists(resolved).Should().BeTrue();
        }
    }
}
