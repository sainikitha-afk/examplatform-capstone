namespace ExamPlatform.Domain.Enums
{
    // Outcome of a simulated charge. Every value here must be reachable from
    // PaymentGatewayService's demo-card/random-outcome logic — see Part 5 of the design.
    public enum PaymentStatus
    {
        Pending,
        Processing,
        Success,
        Failed,
        Cancelled,
        Expired,
        Retrying,
        NetworkFailure,
        Timeout,
        Refunded
    }
}
