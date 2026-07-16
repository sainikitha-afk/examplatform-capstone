// CoachContextBuilder — the deterministic heart of the AI Learning Coach.
//
// This class does ALL of the maths the coach relies on: which attempts count, what the
// average and pass rate are, which topics are strengths, which are weaknesses, and how
// urgent each weakness is. It is a pure function — no IO, no HTTP, no database, no clock
// beyond what the caller passes in — which is exactly why it can be tested exhaustively
// and why its output is reproducible for the same inputs.
//
// Gemini never computes any of this. The model receives the finished numbers as reference
// context and is asked only to explain them and build a study plan around them. If the
// model comes back claiming a score we did not compute, LearningCoachService drops it.

using System.Globalization;
using ExamPlatform.BLL.Models;
using ExamPlatform.Domain.Entities;
using ExamPlatform.Domain.Enums;

namespace ExamPlatform.BLL.Services
{
    public static class CoachContextBuilder
    {
        /// <summary>
        /// Builds the candidate's performance snapshot from data the caller has already
        /// loaded. The caller is responsible for loading only this candidate's rows;
        /// this method re-filters by ownership anyway (CoachAttemptRules.IsUsableBy).
        /// </summary>
        /// <param name="topicScoresByAttemptId">
        /// Per-attempt topic maps read from the analytics JSON files. Attempts with no
        /// entry (analytics still processing, file missing, or file malformed) simply
        /// contribute no topics — they still count toward attempts/average/pass rate.
        /// </param>
        public static CoachSnapshot Build(
            int userId,
            string? fullName,
            IEnumerable<Attempt> attempts,
            IEnumerable<Certificate> certificates,
            IReadOnlyDictionary<int, IReadOnlyDictionary<string, decimal>> topicScoresByAttemptId)
        {
            // ─── 1. AUTHORIZATION FILTER ──────────────────────────────
            // Owned + evaluated + results published. Everything downstream flows from here.
            var usable = attempts
                .Where(a => CoachAttemptRules.IsUsableBy(a, userId))
                // Newest first. SubmittedAt is non-null for Submitted/AutoSubmitted rows,
                // but coalesce anyway so a bad row can't throw; AttemptId breaks ties so
                // the ordering is fully deterministic.
                .OrderByDescending(a => a.SubmittedAt ?? DateTime.MinValue)
                .ThenByDescending(a => a.AttemptId)
                .ToList();

            var firstName = ExtractFirstName(fullName);

            if (usable.Count == 0)
            {
                // No published results yet — the honest answer is "nothing to coach on".
                // The caller uses HasData to skip the Gemini call entirely.
                return new CoachSnapshot(
                    FirstName: firstName,
                    HasData: false,
                    TotalAttempts: 0,
                    AverageScore: 0,
                    PassRate: 0,
                    CertificatesEarned: CountIssued(certificates),
                    RecentAttempts: Array.Empty<CoachAttempt>(),
                    Topics: Array.Empty<TopicScore>(),
                    StrongTopics: Array.Empty<TopicScore>(),
                    FocusAreas: Array.Empty<FocusArea>());
            }

            // ─── 2. HEADLINE STATS ────────────────────────────────────
            // Same semantics as AnalyticsService.GetCandidateDashboardAsync so the coach
            // and the dashboard can never quote different numbers to the same candidate.
            var totalAttempts = usable.Count;
            var averageScore = Math.Round((double)usable.Average(a => a.Score ?? 0m), 2);
            var passedCount = usable.Count(a => a.IsPassed == true);
            var passRate = Math.Round((double)passedCount / totalAttempts * 100.0, 2);

            // ─── 3. RECENT ATTEMPTS (bounded) ─────────────────────────
            var recent = usable
                .Take(CoachAttemptRules.MaxRecentAttempts)
                .Select(a => new CoachAttempt(
                    ExamTitle: a.Exam?.Title ?? string.Empty,
                    SubmittedAt: a.SubmittedAt ?? a.StartedAt,
                    Score: a.Score ?? 0m,
                    IsPassed: a.IsPassed == true,
                    Topics: TopicsFor(a.AttemptId, topicScoresByAttemptId)))
                .ToList();

            // ─── 4. TOPIC AGGREGATION ─────────────────────────────────
            // A topic can appear in several attempts (retakes, overlapping syllabi).
            // Average its percentage across every attempt it appears in, so one lucky
            // exam doesn't erase a consistent weakness.
            var aggregated = usable
                .SelectMany(a => TopicsFor(a.AttemptId, topicScoresByAttemptId))
                .GroupBy(t => t.Topic, StringComparer.OrdinalIgnoreCase)
                .Select(g => new TopicScore(g.First().Topic, Math.Round(g.Average(t => t.Score), 2)))
                .ToList();

            var topics = CapTopics(aggregated);

            // ─── 5. STRENGTHS & FOCUS AREAS ───────────────────────────
            // Strengths are only claimed when genuinely strong: if nothing clears the
            // threshold the list is empty rather than dressing up a 45% as a strength.
            var strongTopics = topics
                .Where(t => t.Score >= CoachAttemptRules.StrongTopicThreshold)
                .OrderByDescending(t => t.Score)
                .ThenBy(t => t.Topic, StringComparer.Ordinal)
                .Take(CoachAttemptRules.MaxStrongTopics)
                .ToList();

            var weakest = topics
                .Where(t => t.Score < CoachAttemptRules.StrongTopicThreshold)
                .OrderBy(t => t.Score)
                .ThenBy(t => t.Topic, StringComparer.Ordinal)
                .Take(CoachAttemptRules.MaxFocusAreas)
                .ToList();

            var focusAreas = weakest
                .Select((t, index) => new FocusArea(
                    Topic: t.Topic,
                    Score: t.Score,
                    Priority: PriorityFor(t.Score),
                    // A deterministic reason so the feature still says something useful
                    // when Gemini is unavailable, and so the model has a factual default
                    // it can only rephrase — not invent around.
                    Reason: ReasonFor(t.Score, isWeakest: index == 0)))
                .ToList();

            return new CoachSnapshot(
                FirstName: firstName,
                HasData: true,
                TotalAttempts: totalAttempts,
                AverageScore: averageScore,
                PassRate: passRate,
                CertificatesEarned: CountIssued(certificates),
                RecentAttempts: recent,
                Topics: topics,
                StrongTopics: strongTopics,
                FocusAreas: focusAreas);
        }

        /// <summary>Score bands that set urgency. Backend-owned — the model cannot change these.</summary>
        public static FocusPriority PriorityFor(decimal score) => score switch
        {
            < 40m => FocusPriority.High,
            < 70m => FocusPriority.Medium,
            _ => FocusPriority.Low
        };

        // ───────────── PRIVATE HELPERS ─────────────

        private static string ReasonFor(decimal score, bool isWeakest)
        {
            var pct = score.ToString("0.##", CultureInfo.InvariantCulture);
            return isWeakest
                ? $"Lowest topic score across your published results ({pct}%)."
                : $"Below the {CoachAttemptRules.StrongTopicThreshold:0}% strength mark ({pct}%).";
        }

        // Only Issued certificates count — revoked ones are retained for audit
        // (see AnalyticsService) and must not inflate the figure.
        private static int CountIssued(IEnumerable<Certificate> certificates)
            => certificates?.Count(c => c.CertStatus == CertificateStatus.Issued) ?? 0;

        private static IReadOnlyList<TopicScore> TopicsFor(
            int attemptId,
            IReadOnlyDictionary<int, IReadOnlyDictionary<string, decimal>> topicScoresByAttemptId)
        {
            if (topicScoresByAttemptId is null ||
                !topicScoresByAttemptId.TryGetValue(attemptId, out var map) ||
                map is null)
            {
                return Array.Empty<TopicScore>();
            }

            return map
                .Select(kvp => new TopicScore(kvp.Key, kvp.Value))
                .OrderByDescending(t => t.Score)
                .ThenBy(t => t.Topic, StringComparer.Ordinal)
                .ToList();
        }

        /// <summary>
        /// Bounds how many topics can reach the prompt. When a candidate has more topics
        /// than the cap, we keep BOTH ends of the distribution — the weakest (what to fix)
        /// and the strongest (what to build on) — rather than blindly truncating one side.
        /// </summary>
        private static IReadOnlyList<TopicScore> CapTopics(List<TopicScore> aggregated)
        {
            var ordered = aggregated
                .OrderByDescending(t => t.Score)
                .ThenBy(t => t.Topic, StringComparer.Ordinal)
                .ToList();

            if (ordered.Count <= CoachAttemptRules.MaxTopics)
                return ordered;

            var half = CoachAttemptRules.MaxTopics / 2;
            var strongest = ordered.Take(half);
            var weakest = ordered.TakeLast(CoachAttemptRules.MaxTopics - half);

            return strongest
                .Concat(weakest)
                .DistinctBy(t => t.Topic, StringComparer.OrdinalIgnoreCase)
                .OrderByDescending(t => t.Score)
                .ThenBy(t => t.Topic, StringComparer.Ordinal)
                .ToList();
        }

        // The prompt addresses the candidate by first name only — the full name, username
        // and email are never sent to the model.
        private static string ExtractFirstName(string? fullName)
        {
            if (string.IsNullOrWhiteSpace(fullName)) return string.Empty;
            return fullName.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries)[0];
        }
    }
}
