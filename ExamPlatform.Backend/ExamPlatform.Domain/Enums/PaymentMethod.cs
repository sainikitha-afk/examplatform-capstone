namespace ExamPlatform.Domain.Enums
{
    // Simulated payment method chosen at checkout. No real gateway integration exists
    // anywhere in the system — this only controls which UI tab/fields were shown.
    public enum PaymentMethod
    {
        CreditCard,
        DebitCard,
        UPI,
        NetBanking,
        Wallet,
        DemoTestCard
    }
}
