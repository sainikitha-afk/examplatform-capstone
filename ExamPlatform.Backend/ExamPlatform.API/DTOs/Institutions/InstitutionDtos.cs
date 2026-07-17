using System.ComponentModel.DataAnnotations;

namespace ExamPlatform.API.DTOs.Institutions
{
    public class SubmitInstitutionDto
    {
        [Required]
        [StringLength(150, MinimumLength = 2)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string? LogoUrl { get; set; }
    }

    public class InstitutionDto
    {
        public int InstitutionId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? LogoUrl { get; set; }
        public string VerificationStatus { get; set; } = string.Empty;
        public DateTime RequestedAt { get; set; }
        public DateTime? VerifiedAt { get; set; }

        // Admin queue only — who owns this request.
        public int? OwnerUserId { get; set; }
        public string? OwnerName { get; set; }
    }
}
