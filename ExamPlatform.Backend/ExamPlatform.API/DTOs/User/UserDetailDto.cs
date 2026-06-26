namespace ExamPlatform.API.DTOs.User
{
    // Self-profile shape (current user). Same fields as summary, kept as a
    // separate type because future "detail" views may add more (e.g.,
    // certificate count, average score) without changing the summary.
    public class UserDetailDto
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