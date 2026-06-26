using System.ComponentModel.DataAnnotations;

namespace ExamPlatform.API.DTOs.User
{
    // PATCH /api/users/{id}/active — admin sets active/inactive.
    public class UpdateActiveStatusDto
    {
        [Required]
        public bool IsActive { get; set; }
    }
}