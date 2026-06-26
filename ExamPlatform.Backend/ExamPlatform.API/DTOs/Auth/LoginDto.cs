using System.ComponentModel.DataAnnotations;

namespace ExamPlatform.API.DTOs.Auth
{
    // POST /api/auth/login request body.
    //
    // Accepts email OR username (per FR1.2). Just one string field.
    // The service decides which one it is (email if contains '@', else username).
    public class LoginDto
    {
        [Required(ErrorMessage = "Email or username is required.")]
        public string EmailOrUsername { get; set; } = null!;

        [Required(ErrorMessage = "Password is required.")]
        public string Password { get; set; } = null!;
    }
}