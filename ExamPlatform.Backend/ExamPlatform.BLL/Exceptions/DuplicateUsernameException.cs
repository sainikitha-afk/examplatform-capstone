namespace ExamPlatform.BLL.Exceptions
{
    public class DuplicateUsernameException : BusinessException
    {
        public DuplicateUsernameException()
            : base("This username is already taken.") { }

        public DuplicateUsernameException(string message) : base(message) { }
    }
}