namespace ExamPlatform.API.DTOs.ExaminerRequest
{
    // Read DTO — returned for all admin read operations
    // (GET /api/examiner-requests and GET /api/examiner-requests/{id}).
    public class ExaminerRequestDto
    {
        public int    ExaminerRequestId { get; set; }
        public string FullName          { get; set; } = null!;
        public string Email             { get; set; } = null!;
        public string Purpose           { get; set; } = null!;
        public string Status            { get; set; } = null!;  // "Pending" / "Approved" / "Rejected"
        public DateTime  RequestedAt    { get; set; }
        public DateTime? ProcessedAt    { get; set; }
        public string?   AdminNote      { get; set; }
    }
}
