namespace ExamPlatform.BLL.Exceptions
{
    // Thrown when the service is asked to act on an Institution verification
    // request that does not exist. Maps to HTTP 404 Not Found.
    public class InstitutionNotFoundException : BusinessException
    {
        public InstitutionNotFoundException(int institutionId)
            : base($"Institution verification request {institutionId} not found.") { }
    }
}
