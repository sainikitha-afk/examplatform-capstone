namespace ExamPlatform.API
{
    // Named rate-limit policies.
    //
    // Only endpoints that opt in with [EnableRateLimiting(...)] are limited. There is NO
    // global limiter, so every existing endpoint — auth, exams, attempts, questions,
    // analytics, certificates, notifications — keeps its current behaviour exactly.
    public static class RateLimitPolicies
    {
        // Applied to CoachController. Every AI request costs money and provider quota, so the
        // coach is the one place in this app that needs a limit.
        public const string AiCoach = "ai-coach";
    }
}
