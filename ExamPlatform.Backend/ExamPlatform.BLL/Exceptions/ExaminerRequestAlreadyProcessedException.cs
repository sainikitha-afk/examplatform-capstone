namespace ExamPlatform.BLL.Exceptions
{
    // Thrown when an admin tries to approve or reject a request that has
    // already been processed (status is Approved or Rejected). Maps to
    // HTTP 409 Conflict — the current state of the resource conflicts with
    // the requested operation.
    public class ExaminerRequestAlreadyProcessedException : BusinessException
    {
        public ExaminerRequestAlreadyProcessedException(int requestId, string currentStatus)
            : base($"Examiner request {requestId} has already been processed (status: {currentStatus}).") { }
    }
}
