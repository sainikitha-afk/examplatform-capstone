namespace ExamPlatform.API.DTOs.ExaminerRequest
{
    // Returned after a successful POST /api/examiner-requests/{id}/approve.
    // Includes the newly created examiner's credentials so the admin can
    // relay them manually if the automated email wasn't delivered
    // (e.g., SMTP not configured in development — NoOp email mode).
    public class ExaminerApprovalResultDto
    {
        public ExaminerRequestDto Request   { get; set; } = null!;
        public int    CreatedUserId         { get; set; }
        public string CreatedUsername       { get; set; } = null!;
        public string CreatedUserEmail      { get; set; } = null!;
        // Plain-text temp password — present in the response so admin can
        // share it if needed. Not stored anywhere beyond this response.
        public string TemporaryPassword     { get; set; } = null!;
    }
}
