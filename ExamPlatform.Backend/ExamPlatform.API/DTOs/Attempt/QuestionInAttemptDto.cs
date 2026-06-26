using ExamPlatform.API.DTOs.Exam;

namespace ExamPlatform.API.DTOs.Attempt
{
    // Represents one question within the AttemptResumeDto.
    // Uses ExamOptionForCandidateDto (no IsCorrect) — candidates must not see answers.
    // Also tracks the candidate's current answer (SelectedOptionId) for UI restoration.
    public class QuestionInAttemptDto
    {
        public int QuestionId { get; set; }
        public string QuestionText { get; set; } = null!;
        public decimal Marks { get; set; }
        public string Difficulty { get; set; } = null!;
        // Options without the correct-answer flag.
        public List<ExamOptionForCandidateDto> Options { get; set; } = new();
        // The answer the candidate saved so far (null if not yet answered).
        public int? SelectedOptionId { get; set; }
    }
}
