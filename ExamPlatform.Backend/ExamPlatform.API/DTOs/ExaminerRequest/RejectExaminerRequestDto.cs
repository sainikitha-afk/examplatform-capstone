using System.ComponentModel.DataAnnotations;

namespace ExamPlatform.API.DTOs.ExaminerRequest
{
    // Payload for POST /api/examiner-requests/{id}/reject (Admin).
    // AdminNote is optional — rejection may or may not include a reason.
    public class RejectExaminerRequestDto
    {
        [MaxLength(500)]
        public string? AdminNote { get; set; }
    }
}
