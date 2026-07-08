using System.ComponentModel.DataAnnotations;

namespace ExamPlatform.API.DTOs.ExaminerRequest
{
    // Payload for POST /api/examiner-requests (anonymous — no auth required).
    // Represents the form a prospective examiner fills in.
    public class SubmitExaminerRequestDto
    {
        [Required(ErrorMessage = "Full name is required.")]
        [MaxLength(100)]
        public string FullName { get; set; } = null!;

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "A valid email address is required.")]
        [MaxLength(150)]
        public string Email { get; set; } = null!;

        // Why they want to become an examiner — shown to the admin when
        // reviewing the request. Min 20 chars enforced at the service layer.
        [Required(ErrorMessage = "Purpose is required.")]
        [MaxLength(1000)]
        public string Purpose { get; set; } = null!;
    }
}
