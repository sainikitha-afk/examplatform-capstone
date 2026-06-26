namespace ExamPlatform.BLL.Exceptions
{
    // Thrown when a service cannot find an exam by the given id (or when the
    // requesting examiner does not own it — we return 404 rather than 403 to
    // avoid leaking information about other examiners' exams).
    // Maps to HTTP 404 Not Found.
    public class ExamNotFoundException : BusinessException
    {
        public ExamNotFoundException()
            : base("Exam not found.") { }

        public ExamNotFoundException(int examId)
            : base($"Exam with id {examId} not found.") { }

        public ExamNotFoundException(string message)
            : base(message) { }
    }
}
