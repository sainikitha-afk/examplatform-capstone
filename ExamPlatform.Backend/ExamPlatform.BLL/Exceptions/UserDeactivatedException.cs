namespace ExamPlatform.BLL.Exceptions
{
    // Thrown on login attempt for a user whose IsActive = false.
    // Distinct from InvalidCredentialsException because the credentials
    // were CORRECT — the account is just disabled. Maps to HTTP 403 Forbidden.
    public class UserDeactivatedException : BusinessException
    {
        public UserDeactivatedException()
            : base("This account has been deactivated. Please contact support.") { }

        public UserDeactivatedException(string message) : base(message) { }
    }
}