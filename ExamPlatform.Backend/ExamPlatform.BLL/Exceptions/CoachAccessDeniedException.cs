namespace ExamPlatform.BLL.Exceptions
{
    // Thrown when a non-Candidate asks for PERSONALIZED coaching.
    //
    // Maps to HTTP 403 Forbidden. The controller already carries
    // [Authorize(Roles = "Candidate")], so this is defence in depth: the role is re-checked
    // inside the business layer, where the data actually gets loaded. If someone later adds
    // a second caller (a background job, a new controller) that forgets the attribute, the
    // service still refuses.
    //
    // The message is deliberately role-appropriate rather than a flat "denied": an Examiner
    // or Admin is not doing anything wrong by opening the coach — personalized coaching is
    // simply built on candidate performance data, which they do not have.
    public class CoachAccessDeniedException : BusinessException
    {
        public CoachAccessDeniedException()
            : base("Personalized learning coaching is available to candidates only. " +
                   "General platform guidance is available to all users.") { }

        public CoachAccessDeniedException(string message) : base(message) { }
    }
}
