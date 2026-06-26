namespace ExamPlatform.BLL.Exceptions
{
    // The base class for ALL custom exceptions thrown by our business layer.
    //
    // Why a base class?
    //   - Lets the global middleware do `catch (BusinessException ex)` to
    //     handle every domain-driven exception in one place.
    //   - Separates OUR exceptions (business rules) from system exceptions
    //     (DB down, network error, NullReferenceException etc.) which we
    //     handle differently (log + return generic 500).
    //
    // 'abstract' means: never thrown directly. Always thrown as a specific
    // subclass like InvalidCredentialsException. This keeps every thrown
    // exception semantically precise.
    public abstract class BusinessException : Exception
    {
        // Calling base(message) passes the message up to Exception's
        // constructor, which stores it. Exception.Message will return it.
        protected BusinessException(string message) : base(message) { }
    }
}