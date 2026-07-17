namespace ExamPlatform.Domain.Enums
{
    // What a Payment row is for. Drives which child record (ExaminerSubscription vs
    // CandidatePurchase) the payment is expected to unlock on Success.
    public enum PaymentPurpose
    {
        SubscriptionPurchase,
        SubscriptionRenewal,
        ExamCertificationPurchase
    }
}
