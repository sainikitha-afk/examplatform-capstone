using ExamPlatform.BLL.Models;

namespace ExamPlatform.BLL.Interfaces
{
    /// <summary>
    /// The ExamHub AI Learning Coach.
    /// <para>
    /// Turns a candidate's own published results into personalized learning guidance. Every
    /// number it reports is computed by <see cref="Services.CoachContextBuilder"/>; Gemini is
    /// used only to explain those numbers and build a study plan around them.
    /// </para>
    /// <para>
    /// AUTHORIZATION: <c>userId</c> and <c>role</c> are supplied by the controller from the
    /// validated JWT — never from a request body. Both methods re-check the role themselves,
    /// so the rule holds even if a future caller forgets the [Authorize] attribute.
    /// </para>
    /// </summary>
    public interface ILearningCoachService
    {
        /// <summary>
        /// The candidate's deterministic performance snapshot — stats, topics, strengths,
        /// focus areas. Involves NO AI call, so the UI can render its welcome and no-data
        /// states for free. Candidate role only.
        /// </summary>
        Task<CoachSnapshot> GetSnapshotAsync(int userId, string role, CancellationToken cancellationToken);

        /// <summary>
        /// Answers a question.
        /// <para>
        /// <see cref="CoachMode.Personalized"/> — grounded in the candidate's own authorized
        /// results (Candidate role only; otherwise <see cref="Exceptions.CoachAccessDeniedException"/>).
        /// If the candidate has no published results, returns a no-data answer WITHOUT calling
        /// Gemini at all.
        /// </para>
        /// <para>
        /// <see cref="CoachMode.General"/> — platform guidance grounded in the curated
        /// knowledge file. Available to any authenticated role, and touches no user data.
        /// </para>
        /// </summary>
        Task<CoachAnswer> AskAsync(CoachQuery query, CancellationToken cancellationToken);
    }
}
