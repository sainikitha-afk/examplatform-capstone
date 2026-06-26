namespace ExamPlatform.API.DTOs.Exam
{
    // Option DTO for candidates — deliberately omits IsCorrect so the
    // answer key is never transmitted to the candidate's browser.
    // Also reused in AttemptResumeDto (QuestionInAttemptDto).
    public class ExamOptionForCandidateDto
    {
        public int OptionId { get; set; }
        public string OptionText { get; set; } = null!;
    }
}
