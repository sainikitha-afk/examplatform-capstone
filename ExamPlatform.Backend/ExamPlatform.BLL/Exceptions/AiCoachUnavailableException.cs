namespace ExamPlatform.BLL.Exceptions
{
    // Why the AI call failed. Used for LOGGING ONLY — the category never reaches the user,
    // because telling a candidate "Gemini returned 429" is noise to them and detail to an
    // attacker. The public Message is identical for every category.
    public enum CoachFailureCategory
    {
        Misconfigured,   // no API key configured
        Timeout,         // provider did not answer in time
        HttpStatus,      // provider returned 4xx/5xx (404 model gone, 429 quota, 500 outage)
        EmptyContent,    // 200 OK but no text in the response
        MalformedJson    // 200 OK but the text is not the JSON contract we asked for
    }

    // Thrown when the AI Learning Coach cannot produce an answer.
    //
    // Maps to HTTP 503 in GlobalExceptionMiddleware. The rule this encodes: an AI outage is
    // NOT an application outage. Exam-taking, submission, analytics and the performance
    // chart are all unaffected, and the message says so — so a candidate who sees this knows
    // their results are safe. We never fabricate a coaching answer to paper over a failure.
    public class AiCoachUnavailableException : BusinessException
    {
        public const string UserMessage =
            "The AI Learning Coach is temporarily unavailable. " +
            "Your exam data and existing analytics are unaffected. Please try again shortly.";

        public CoachFailureCategory Category { get; }

        public AiCoachUnavailableException(CoachFailureCategory category)
            : base(UserMessage)
        {
            Category = category;
        }

        public AiCoachUnavailableException(CoachFailureCategory category, Exception innerException)
            : base(UserMessage)
        {
            Category = category;
            // Kept for the server-side log only; GlobalExceptionMiddleware sends Message,
            // never the inner exception, to the client.
            InnerFailure = innerException;
        }

        public Exception? InnerFailure { get; }
    }
}
