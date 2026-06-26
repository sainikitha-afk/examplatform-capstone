using System.ComponentModel.DataAnnotations;

namespace ExamPlatform.API.DTOs.Auth
{
    // The JSON request body for POST /api/auth/register.
    //
    // The DataAnnotations attributes ([Required], [EmailAddress], etc.) are
    // checked by ASP.NET Core's MODEL BINDING + MODEL VALIDATION pipeline
    // BEFORE the controller action runs. If any rule fails, ASP.NET returns
    // a 400 Bad Request automatically with a JSON describing which field
    // failed — we don't write that handling code.
    //
    // This is "framework-level validation." It catches malformed input
    // before it reaches the service. The service then layers BUSINESS
    // validation on top (duplicate email check, etc.).
    public class RegisterDto
    {
        [Required(ErrorMessage = "Full name is required.")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Full name must be 2-100 characters.")]
        public string FullName { get; set; } = null!;

        [Required(ErrorMessage = "Username is required.")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "Username must be 3-50 characters.")]
        public string Username { get; set; } = null!;

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email format.")]
        [StringLength(150, ErrorMessage = "Email cannot exceed 150 characters.")]
        public string Email { get; set; } = null!;

        [Required(ErrorMessage = "Password is required.")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters.")]
        public string Password { get; set; } = null!;
    }
}