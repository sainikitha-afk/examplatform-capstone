// CoachAttemptRules — the SINGLE definition of which attempts the AI Learning Coach
// is allowed to look at. Everything the coach ever says about a candidate is derived
// from attempts that pass IsCoachable, so this predicate is the feature's security
// boundary for candidate data.
//
// It intentionally mirrors, term for term, the gate the rest of the app already enforces:
//   • AttemptService.GetResultAsync — ownership, then ExamStatus == ResultsPublished
//   • AnalyticsService.GetCandidateDashboardAsync — Submitted/AutoSubmitted + ResultsPublished
//
// Those two are working, protected code and are NOT edited by this feature, so the rule
// is stated once here rather than refactored into a shared helper. To stop the two
// definitions from silently drifting apart, LearningCoachServiceTests drives the SAME
// mixed attempt set through BOTH AnalyticsService and the coach and asserts they select
// an identical set of attempts. If either side ever changes, that test fails.

using ExamPlatform.Domain.Entities;
using ExamPlatform.Domain.Enums;

namespace ExamPlatform.BLL.Services
{
    public static class CoachAttemptRules
    {
        // Caps that bound what can be sent to Gemini (token cost + privacy surface).
        public const int MaxRecentAttempts = 5;
        public const int MaxTopics = 12;
        public const int MaxStrongTopics = 3;
        public const int MaxFocusAreas = 3;

        // A topic at or above this percentage counts as a strength; below it, it is a
        // candidate for focus. Chosen to line up with the platform's usual pass mark.
        public const decimal StrongTopicThreshold = 70m;

        /// <summary>
        /// True when the attempt has actually been evaluated AND the examiner has
        /// published the exam's results. An InProgress attempt has no score, and an
        /// attempt on an unpublished exam must never surface a score — not through
        /// /result, not through /analytics/me, and not through the AI coach.
        /// </summary>
        public static bool IsCoachable(Attempt attempt)
        {
            if (attempt is null) return false;

            var isEvaluated = attempt.AttemptStatus == AttemptStatus.Submitted ||
                              attempt.AttemptStatus == AttemptStatus.AutoSubmitted;

            return isEvaluated &&
                   attempt.Exam != null &&
                   attempt.Exam.ExamStatus == ExamStatus.ResultsPublished;
        }

        /// <summary>
        /// Defence in depth. Attempts are loaded through IAttemptRepository.GetByUserAsync,
        /// which is already scoped to one candidate, but the coach re-checks ownership so a
        /// future repository change can never widen the coach's data set by accident.
        /// </summary>
        public static bool IsOwnedBy(Attempt attempt, int userId)
            => attempt is not null && attempt.UserId == userId;

        /// <summary>Owned AND coachable — the only attempts the coach may use.</summary>
        public static bool IsUsableBy(Attempt attempt, int userId)
            => IsOwnedBy(attempt, userId) && IsCoachable(attempt);
    }
}
