using System.ComponentModel.DataAnnotations;

namespace ExamPlatform.API.DTOs.User
{
    // POST /api/users/examiners — admin creates an examiner account.
    public class CreateExaminerDto
    {
        [Required, StringLength(100, MinimumLength = 2)]
        public string FullName { get; set; } = null!;

        [Required, StringLength(50, MinimumLength = 3)]
        public string Username { get; set; } = null!;

        [Required, EmailAddress, StringLength(150)]
        public string Email { get; set; } = null!;

        [Required, StringLength(100, MinimumLength = 6)]
        public string Password { get; set; } = null!;
    }
}