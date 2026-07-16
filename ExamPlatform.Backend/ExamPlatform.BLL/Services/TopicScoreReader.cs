// TopicScoreReader — reads the topic-score JSON that the existing analytics pipeline
// produces, so the AI coach can talk about topics without duplicating that pipeline.
//
// WHY THE BACKEND READS THE FILE ITSELF:
//   Angular fetches these files over HTTP using Attempt.TopicScoresUrl. That URL is a
//   client-visible value, so if the coach accepted one from the request body a candidate
//   could simply paste another candidate's path and have the AI narrate their results.
//   Instead the coach passes the ids of an attempt it has ALREADY confirmed the candidate
//   owns, and this class rebuilds the path server-side. There is no code path from a
//   client-supplied string to a file read.
//
// WHY MISSING/BROKEN FILES RETURN null RATHER THAN THROW:
//   "Analytics hasn't finished yet" is a normal state, not an error — the background
//   processor may still be running. The coach degrades (talks about scores, stays quiet
//   about topics) instead of failing the request.

// CA1848: standard ILogger extension methods used deliberately — see CertificateService.cs
#pragma warning disable CA1848
using System.Text.Json;
using ExamPlatform.BLL.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ExamPlatform.BLL.Services
{
    public class TopicScoreReader : ITopicScoreReader
    {
        private readonly ILogger<TopicScoreReader> _logger;
        private readonly string _rootPath;

        public TopicScoreReader(IConfiguration configuration, ILogger<TopicScoreReader> logger)
        {
            _logger = logger;
            _rootPath = ResolveRootPath(configuration["Coach:TopicScoresRoot"]);
        }

        /// <summary>
        /// Resolves the folder that holds the per-candidate analytics files. Defaults to the
        /// exact location AnalyticsProcessor writes to, so the coach reads precisely what the
        /// pipeline produced. Overridable via Coach:TopicScoresRoot (used by tests, and by any
        /// deployment where the API is not launched from its project folder).
        /// </summary>
        public static string ResolveRootPath(string? configuredPath)
        {
            if (!string.IsNullOrWhiteSpace(configuredPath))
                return Path.GetFullPath(configuredPath);

            return Path.GetFullPath(Path.Combine(
                Directory.GetCurrentDirectory(), "..", "ExamPlatform.BLL", "Assets", "candidates"));
        }

        /// <summary>The file name AnalyticsProcessor writes for an attempt.</summary>
        public static string FileNameFor(int attemptId) => $"attempt_{attemptId}_scores.json";

        public async Task<IReadOnlyDictionary<string, decimal>?> ReadAsync(
            int userId, int attemptId, CancellationToken cancellationToken)
        {
            // Both ids are integers taken from a verified-owned attempt row, so they cannot
            // carry path separators. The containment check below is belt-and-braces: if the
            // resolved path ever escapes the candidates root, we refuse to read it.
            var fullPath = Path.GetFullPath(
                Path.Combine(_rootPath, userId.ToString(), FileNameFor(attemptId)));

            if (!IsInsideRoot(fullPath))
            {
                _logger.LogWarning(
                    "Refused to read a topic-score file outside the candidates root for attempt {AttemptId}.",
                    attemptId);
                return null;
            }

            if (!File.Exists(fullPath))
            {
                // Normal while the background analytics job is still running.
                _logger.LogInformation(
                    "No topic-score file yet for attempt {AttemptId}; coaching will omit topics for it.",
                    attemptId);
                return null;
            }

            try
            {
                await using var stream = File.OpenRead(fullPath);
                var scores = await JsonSerializer.DeserializeAsync<Dictionary<string, decimal>>(
                    stream, cancellationToken: cancellationToken);

                if (scores is null || scores.Count == 0)
                    return null;

                return scores;
            }
            catch (OperationCanceledException)
            {
                // The caller gave up (client disconnected / request timeout) — let it propagate.
                throw;
            }
            catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
            {
                // Truncated, hand-edited, or unreadable file. We log the attempt id and the
                // failure kind — never the file contents.
                _logger.LogWarning(ex,
                    "Topic-score file for attempt {AttemptId} could not be parsed; coaching will omit topics for it.",
                    attemptId);
                return null;
            }
        }

        private bool IsInsideRoot(string fullPath)
        {
            var root = _rootPath.EndsWith(Path.DirectorySeparatorChar)
                ? _rootPath
                : _rootPath + Path.DirectorySeparatorChar;

            return fullPath.StartsWith(root, StringComparison.Ordinal);
        }
    }
}
