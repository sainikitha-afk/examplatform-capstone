using System.ComponentModel.DataAnnotations;

namespace ExamPlatform.API.DTOs.Auth
{
    // Request body for POST /api/auth/change-password.
    //
    // Deliberately carries NO user identifier: the account whose password changes
    // is always the one in the caller's JWT. A userId field here would be an
    // invitation to change someone else's password.
    public class ChangePasswordDto
    {
        [Required(ErrorMessage = "Current password is required.")]
        public string CurrentPassword { get; set; } = null!;

        // Same rule as registration ([StringLength] gives an automatic 400 before
        // the action runs; the service re-checks after trimming).
        [Required(ErrorMessage = "New password is required.")]
        [StringLength(100, MinimumLength = 6,
            ErrorMessage = "New password must be at least 6 characters.")]
        public string NewPassword { get; set; } = null!;
    }
}
