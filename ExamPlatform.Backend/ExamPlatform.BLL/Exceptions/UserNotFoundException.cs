    namespace ExamPlatform.BLL.Exceptions
    {
        // Thrown when a service expects a user to exist but the repository
        // returns null. Maps to HTTP 404 Not Found.
        public class UserNotFoundException : BusinessException
        {
            public UserNotFoundException()
                : base("User not found.") { }

            public UserNotFoundException(int userId)
                : base($"User with id {userId} not found.") { }

            public UserNotFoundException(string message) : base(message) { }
        }
    }
    /*
    Note the constructor overloading — UserNotFoundException(int userId) is a more specific version that builds the message for you. 
    Calling throw new UserNotFoundException(7) produces "User with id 7 not found." Convenience + type safety in one. This pattern 
    (multiple constructors for the same exception with different parameter shapes) is straight from your library project's exceptions.
    */

