using System.ComponentModel.DataAnnotations;
using ExamPlatform.BLL.Services;

namespace ExamPlatform.API.DTOs.Coach
{
    // Request body for POST /api/coach/ask and POST /api/coach/help.
    //
    // NOTE WHAT IS NOT HERE, deliberately: no userId, no candidateId, no role, no attemptId,
    // no examId, no topicScoresUrl, no file path, no scores. The caller's identity and role
    // come from the validated JWT and nothing else — there is simply no field on this DTO
    // through which a candidate could point the coach at somebody else's data.
    public class CoachAskRequestDto
    {
        // [Required]/[StringLength] give an automatic 400 before the action runs; the service
        // re-validates after trimming (same belt-and-braces pattern as CreateIssueReportDto).
        [Required(ErrorMessage = "Please enter a question for the AI Learning Coach.")]
        [StringLength(LearningCoachService.MaxMessageLength, MinimumLength = 1,
            ErrorMessage = "Your question must be between 1 and 500 characters.")]
        public string Message { get; set; } = null!;

        // Optional. The last couple of exchanges, echoed back by the browser so follow-ups
        // ("make that seven days instead") make sense. It is conversational context only:
        // it is never consulted for identity or authorization, and the service truncates it.
        public List<CoachTurnDto>? History { get; set; }
    }

    // One prior exchange in the conversation.
    public class CoachTurnDto
    {
        [StringLength(LearningCoachService.MaxHistoryTextLength)]
        public string Question { get; set; } = string.Empty;

        [StringLength(LearningCoachService.MaxHistoryTextLength)]
        public string Answer { get; set; } = string.Empty;
    }
}
