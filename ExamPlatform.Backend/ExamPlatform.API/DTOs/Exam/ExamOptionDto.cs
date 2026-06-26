namespace ExamPlatform.API.DTOs.Exam
{
    // Option DTO for Examiners and Admins — includes the IsCorrect flag.
    // Candidates receive ExamOptionForCandidateDto which omits it.
    public class ExamOptionDto
    {
        public int OptionId { get; set; }
        public string OptionText { get; set; } = null!;
        public bool IsCorrect { get; set; } // answer key — visible to examiner/admin only
    }
}
