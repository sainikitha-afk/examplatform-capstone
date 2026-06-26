namespace ExamPlatform.API.DTOs.User
{
    // Compact shape for list views (admin sees all users).
    public class UserSummaryDto
    {
        public int UserId { get; set; }
        public string FullName { get; set; } = null!;
        public string Username { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string Role { get; set; } = null!;
        public bool IsActive { get; set; }
        public DateTime DateOfJoining { get; set; }
    }
}