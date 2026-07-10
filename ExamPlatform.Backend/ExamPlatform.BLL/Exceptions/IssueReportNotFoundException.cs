namespace ExamPlatform.BLL.Exceptions
{
    // Thrown when an issue report cannot be found by id, OR when the requesting
    // examiner does not own the exam the report belongs to. We return 404 rather
    // than 403 to avoid leaking the existence of other examiners' reports.
    // Maps to HTTP 404 Not Found.
    public class IssueReportNotFoundException : BusinessException
    {
        public IssueReportNotFoundException()
            : base("Issue report not found.") { }

        public IssueReportNotFoundException(int id)
            : base($"Issue report with id {id} not found.") { }
    }
}
