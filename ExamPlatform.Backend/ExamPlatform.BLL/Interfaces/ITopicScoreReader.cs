namespace ExamPlatform.BLL.Interfaces
{
    /// <summary>
    /// Reads the per-topic score files that AnalyticsProcessor writes after an attempt is
    /// submitted (Assets/candidates/{userId}/attempt_{attemptId}_scores.json).
    /// <para>
    /// SECURITY: the implementation builds the path from the ids of an attempt row the
    /// caller has ALREADY verified the candidate owns. It never accepts a URL, a filename,
    /// or a path from the client — <c>Attempt.TopicScoresUrl</c> is a client-visible value
    /// and is therefore treated as untrusted for file access.
    /// </para>
    /// </summary>
    public interface ITopicScoreReader
    {
        /// <summary>
        /// Returns the topic → percentage map for one owned attempt, or <c>null</c> when the
        /// analytics file does not exist yet, cannot be read, or is malformed. Returning null
        /// (rather than throwing) is deliberate: missing analytics is a normal state — the
        /// background processor may still be running — and must degrade the coach's answer,
        /// not break it.
        /// </summary>
        Task<IReadOnlyDictionary<string, decimal>?> ReadAsync(
            int userId, int attemptId, CancellationToken cancellationToken);
    }
}
