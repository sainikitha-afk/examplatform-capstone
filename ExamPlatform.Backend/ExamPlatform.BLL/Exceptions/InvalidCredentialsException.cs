namespace ExamPlatform.BLL.Exceptions
{
    // Thrown by AuthService.Login when email is wrong OR password is wrong.
    //
    // Important security choice: we DON'T tell the caller which one was wrong.
    // A "user not found" vs "wrong password" distinction would let an attacker
    // enumerate valid emails. Same exception for both protects against that.
    public class InvalidCredentialsException : BusinessException
    {
        public InvalidCredentialsException()
            : base("Invalid email or password.") { }

        public InvalidCredentialsException(string message) : base(message) { }
    }
}