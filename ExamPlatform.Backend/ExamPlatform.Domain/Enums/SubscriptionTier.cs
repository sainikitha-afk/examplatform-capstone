namespace ExamPlatform.Domain.Enums
{
    // The three examiner-facing plan tiers (see SubscriptionPlan). Trial and FreeForever
    // are never purchased directly — Trial is auto-granted at examiner approval,
    // FreeForever is the automatic fallback after a trial-exhausted examiner declines
    // the upgrade offer twice. Professional is the only tier reachable via checkout.
    public enum SubscriptionTier
    {
        Trial,
        Professional,
        FreeForever
    }
}
