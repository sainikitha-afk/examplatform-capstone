namespace ExamPlatform.Domain.Enums
{
    // Lifecycle of one examiner's subscription. TrialActive -> TrialExhausted happens the
    // moment the 8th lifetime exam is created; from there the examiner either upgrades
    // (-> ProfessionalActive, possibly with a one-time discount) or declines twice and
    // lands on FreeForever. ProfessionalActive can lapse into a short GracePeriod on a
    // missed (simulated) renewal before falling back to FreeForever — never back to
    // TrialActive, since the trial is a one-time, lifetime-tracked allowance.
    public enum ExaminerSubscriptionStatus
    {
        TrialActive,
        TrialExhausted,
        ProfessionalActive,
        GracePeriod,
        FreeForever,
        Cancelled
    }
}
