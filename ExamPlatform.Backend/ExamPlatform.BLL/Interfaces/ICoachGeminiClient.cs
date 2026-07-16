namespace ExamPlatform.BLL.Interfaces
{
    /// <summary>
    /// The AI Learning Coach's channel to Gemini.
    /// <para>
    /// Kept separate from <see cref="IAgentService"/> on purpose. IAgentService does one job
    /// — categorize exam questions for the analytics pipeline — and that pipeline is working
    /// and protected. The coach has different needs (system instruction, per-request
    /// cancellation, "unavailable" instead of "throw and fail the job"), so it gets its own
    /// thin client rather than growing the one the pipeline depends on.
    /// </para>
    /// </summary>
    public interface ICoachGeminiClient
    {
        /// <summary>
        /// Sends the system instruction plus the caller-built content and returns the model's
        /// raw JSON text (already isolated from markdown fences / stray prose).
        /// <para>
        /// Throws <see cref="Exceptions.AiCoachUnavailableException"/> for EVERY provider
        /// failure — missing key, timeout, 404, 429, 500, empty body, unparseable body — so
        /// callers have exactly one degraded path to handle. Cancellation requested by the
        /// caller propagates as <see cref="OperationCanceledException"/>.
        /// </para>
        /// </summary>
        Task<string> GenerateJsonAsync(
            string systemInstruction, string userContent, CancellationToken cancellationToken);
    }
}
