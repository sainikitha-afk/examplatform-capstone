namespace ExamPlatform.BLL.Exceptions
{
    // Thrown when the service is asked to act on an ExaminerRequest that
    // does not exist in the database. Maps to HTTP 404 Not Found.
    public class ExaminerRequestNotFoundException : BusinessException
    {
        public ExaminerRequestNotFoundException()
            : base("Examiner request not found.") { }

        public ExaminerRequestNotFoundException(int requestId)
            : base($"Examiner request with id {requestId} not found.") { }
    }
}
