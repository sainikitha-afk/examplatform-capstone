// Value objects for the AI Learning Coach.
// These are BLL-layer models (not DTOs, not entities) — same role as AnalyticsModels.cs:
// they carry the deterministic, authorization-filtered view of a candidate's performance
// that the API layer maps to response DTOs.
//
// Design rule that runs through this whole feature: every NUMBER in these records is
// computed in C# from the candidate's own rows. Gemini is never asked to calculate,
// rank, or recall a score — only to explain and plan around what we already computed.

namespace ExamPlatform.BLL.Models
{
    // Which coaching mode the caller asked for.
    //   Personalized — grounded in the authenticated candidate's own results (Candidate only).
    //   General      — platform guidance from the curated knowledge file (any authenticated role).
    public enum CoachMode
    {
        Personalized,
        General
    }

    // How urgently a weak topic needs attention. Assigned by the backend from score
    // bands (see CoachContextBuilder.PriorityFor) — never by the model.
    public enum FocusPriority
    {
        High,
        Medium,
        Low
    }

    // One topic percentage, e.g. ("Exception Handling", 40).
    public record TopicScore(string Topic, decimal Score);

    // A completed, results-published attempt reduced to only what coaching needs.
    // Deliberately carries no ExamId/AttemptId/UserId — nothing that identifies a row
    // in the database ever needs to reach the model.
    public record CoachAttempt(
        string ExamTitle,
        DateTime SubmittedAt,
        decimal Score,
        bool IsPassed,
        IReadOnlyList<TopicScore> Topics);

    // A topic the candidate should work on. Score and Priority are backend-computed;
    // only Reason may be rewritten by the model (and it is length-capped).
    public record FocusArea(
        string Topic,
        decimal Score,
        FocusPriority Priority,
        string Reason);

    // One day of a study plan. Day numbers are renumbered 1..n by the backend.
    public record StudyPlanDay(
        int Day,
        string Focus,
        IReadOnlyList<string> Tasks);

    // The deterministic snapshot: what the backend knows about this candidate before
    // Gemini is involved at all. Doubles as the /snapshot response (welcome + no-data
    // states are answered from this without spending a single token).
    public record CoachSnapshot(
        string FirstName,
        bool HasData,
        int TotalAttempts,
        double AverageScore,
        double PassRate,
        int CertificatesEarned,
        IReadOnlyList<CoachAttempt> RecentAttempts,
        IReadOnlyList<TopicScore> Topics,
        IReadOnlyList<TopicScore> StrongTopics,
        IReadOnlyList<FocusArea> FocusAreas);

    // One prior exchange, echoed back by the client so follow-ups ("make it 7 days
    // instead") work. Untrusted and length-capped — it is conversational context only
    // and is never consulted for identity or authorization.
    public record CoachTurn(string Question, string Answer);

    // A request to the coach. UserId and Role come from the validated JWT in the
    // controller — NEVER from the request body.
    public record CoachQuery(
        int UserId,
        string Role,
        CoachMode Mode,
        string Message,
        IReadOnlyList<CoachTurn> History);

    // The coach's answer. Everything here has passed backend normalization and grounding.
    public record CoachAnswer(
        string Answer,
        string Summary,
        IReadOnlyList<TopicScore> StrongTopics,
        IReadOnlyList<FocusArea> FocusAreas,
        IReadOnlyList<StudyPlanDay> StudyPlan,
        IReadOnlyList<string> SuggestedQuestions,
        bool HasPerformanceData,
        DateTime GeneratedAt);
}
