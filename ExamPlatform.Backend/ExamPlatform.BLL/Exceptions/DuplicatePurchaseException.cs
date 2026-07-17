namespace ExamPlatform.BLL.Exceptions
{
    // Thrown when a candidate attempts to purchase a premium exam they
    // already hold an Active purchase for. The DB unique index on
    // (UserId, ExamId) is the actual race-safe guard; this is the friendly
    // pre-check path. Maps to HTTP 409 Conflict.
    public class DuplicatePurchaseException : BusinessException
    {
        public DuplicatePurchaseException()
            : base("You have already purchased access to this exam.") { }
    }
}
