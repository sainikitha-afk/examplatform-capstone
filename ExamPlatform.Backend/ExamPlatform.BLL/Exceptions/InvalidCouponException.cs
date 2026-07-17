namespace ExamPlatform.BLL.Exceptions
{
    // Thrown for any coupon redemption failure: not found, inactive, expired,
    // scope mismatch, redemption limit reached, or already redeemed by this
    // user. The message is always specific to which of those applies.
    // Maps to HTTP 400 Bad Request.
    public class InvalidCouponException : BusinessException
    {
        public InvalidCouponException(string reason) : base(reason) { }
    }
}
