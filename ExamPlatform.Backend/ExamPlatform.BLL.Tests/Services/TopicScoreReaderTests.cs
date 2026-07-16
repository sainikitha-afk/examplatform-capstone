// TopicScoreReaderTests — proves the coach reads the analytics files the pipeline actually
// produces, and that every degraded state (missing file, empty file, truncated JSON, wrong
// shape) returns null instead of throwing. "Analytics is still processing" is a normal
// state on this platform, so the coach must never break because of it.

using System.Text.Json;
using ExamPlatform.BLL.Services;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace ExamPlatform.BLL.Tests.Services
{
    [TestFixture]
    public class TopicScoreReaderTests
    {
        private string _root = null!;

        [SetUp]
        public void SetUp()
        {
            // A throwaway candidates root per test — nothing touches the real Assets folder.
            _root = Path.Combine(Path.GetTempPath(), "examhub-coach-tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_root);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_root)) Directory.Delete(_root, recursive: true);
        }

        private TopicScoreReader BuildSut()
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?> { ["Coach:TopicScoresRoot"] = _root })
                .Build();

            return new TopicScoreReader(config, new Mock<ILogger<TopicScoreReader>>().Object);
        }

        // Writes a file exactly where AnalyticsProcessor would write it.
        private void WriteScoreFile(int userId, int attemptId, string content)
        {
            var dir = Path.Combine(_root, userId.ToString());
            Directory.CreateDirectory(dir);
            File.WriteAllText(Path.Combine(dir, TopicScoreReader.FileNameFor(attemptId)), content);
        }

        [Test]
        public async Task ReadAsync_ValidFile_ReturnsTopicScores()
        {
            // Same shape the pipeline emits: {"Topic":percent,...}
            WriteScoreFile(6, 15, JsonSerializer.Serialize(new Dictionary<string, decimal>
            {
                ["Transaction Management"] = 100m,
                ["Database Normalization"] = 40m,
            }));
            var sut = BuildSut();

            var scores = await sut.ReadAsync(6, 15, CancellationToken.None);

            scores.Should().NotBeNull();
            scores!["Transaction Management"].Should().Be(100m);
            scores["Database Normalization"].Should().Be(40m);
        }

        [Test]
        public async Task ReadAsync_FileNameMatchesAnalyticsProcessorConvention()
        {
            // If the pipeline's naming ever changed, the coach would silently lose topics —
            // this pins the contract between the two.
            TopicScoreReader.FileNameFor(15).Should().Be("attempt_15_scores.json");

            WriteScoreFile(6, 15, "{\"LINQ\":80}");
            var scores = await BuildSut().ReadAsync(6, 15, CancellationToken.None);

            scores.Should().NotBeNull();
        }

        [Test]
        public async Task ReadAsync_MissingFile_ReturnsNull()
        {
            // The background analytics job has not produced this file yet.
            var scores = await BuildSut().ReadAsync(6, 999, CancellationToken.None);

            scores.Should().BeNull();
        }

        [Test]
        public async Task ReadAsync_MalformedJson_ReturnsNull()
        {
            WriteScoreFile(6, 15, "{\"LINQ\": 80,");   // truncated
            var scores = await BuildSut().ReadAsync(6, 15, CancellationToken.None);

            scores.Should().BeNull();
        }

        [Test]
        public async Task ReadAsync_WrongJsonShape_ReturnsNull()
        {
            WriteScoreFile(6, 15, "[\"not\", \"an\", \"object\"]");
            var scores = await BuildSut().ReadAsync(6, 15, CancellationToken.None);

            scores.Should().BeNull();
        }

        [Test]
        public async Task ReadAsync_NonNumericScores_ReturnsNull()
        {
            WriteScoreFile(6, 15, "{\"LINQ\": \"very good\"}");
            var scores = await BuildSut().ReadAsync(6, 15, CancellationToken.None);

            scores.Should().BeNull();
        }

        [Test]
        public async Task ReadAsync_EmptyObject_ReturnsNull()
        {
            WriteScoreFile(6, 15, "{}");
            var scores = await BuildSut().ReadAsync(6, 15, CancellationToken.None);

            scores.Should().BeNull("an empty map carries no topic information");
        }

        [Test]
        public async Task ReadAsync_AnotherCandidatesFolder_IsNotReadForThisCandidate()
        {
            // Candidate 6's file exists; candidate 7 must not pick it up. The path is built
            // from the ids of an attempt the caller already proved is owned, so this is the
            // last line of defence rather than the first.
            WriteScoreFile(6, 15, "{\"LINQ\":95}");
            var sut = BuildSut();

            var foreign = await sut.ReadAsync(7, 15, CancellationToken.None);

            foreign.Should().BeNull();
        }

        [Test]
        public void ResolveRootPath_ConfiguredPath_IsHonored()
        {
            var custom = Path.Combine(Path.GetTempPath(), "custom-candidates");

            TopicScoreReader.ResolveRootPath(custom).Should().Be(Path.GetFullPath(custom));
        }

        [Test]
        public void ResolveRootPath_NoConfig_DefaultsToAnalyticsProcessorLocation()
        {
            // Must match where AnalyticsProcessor writes: <cwd>/../ExamPlatform.BLL/Assets/candidates
            var resolved = TopicScoreReader.ResolveRootPath(null);

            resolved.Should().EndWith(Path.Combine("ExamPlatform.BLL", "Assets", "candidates"));
        }

        [Test]
        public async Task ReadAsync_CancellationIsPropagated()
        {
            WriteScoreFile(6, 15, "{\"LINQ\":80}");
            var sut = BuildSut();
            using var cts = new CancellationTokenSource();
            await cts.CancelAsync();

            var act = async () => await sut.ReadAsync(6, 15, cts.Token);

            await act.Should().ThrowAsync<OperationCanceledException>();
        }
    }
}
