namespace ExamPlatform.BLL.Exceptions
{
    // Thrown when a candidate tries to start/resume an attempt on a premium
    // exam without an Active CandidatePurchase row. Carries the exam id and
    // price so the frontend can route straight to checkout.
    // Maps to HTTP 402 Payment Required.
    public class PremiumAccessRequiredException : BusinessException
    {
        public int ExamId { get; }
        public decimal Price { get; }

        public PremiumAccessRequiredException(int examId, decimal price)
            : base("This is a premium certification exam. Purchase access to unlock your attempt.")
        {
            ExamId = examId;
            Price = price;
        }
    }
}
