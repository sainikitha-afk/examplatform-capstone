namespace ExamPlatform.BLL.Exceptions
{
    // Thrown when a Free Forever examiner tries to create a second exam
    // before their rolling cooldown (1 exam / N days) has elapsed. Carries
    // NextEligibleAt so the frontend can render a countdown.
    // Maps to HTTP 429 Too Many Requests (rate-limited by design, not by error).
    public class CooldownActiveException : BusinessException
    {
        public DateTime NextEligibleAt { get; }

        public CooldownActiveException(DateTime nextEligibleAt)
            : base("Your Free Forever plan allows one new exam every 3 days. " +
                   $"You can create your next exam after {nextEligibleAt:u}.")
        {
            NextEligibleAt = nextEligibleAt;
        }
    }
}
