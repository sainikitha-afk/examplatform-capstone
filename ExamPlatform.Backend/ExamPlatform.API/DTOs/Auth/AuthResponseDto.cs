namespace ExamPlatform.API.DTOs.Auth
{
    // The JSON shape returned after successful Register OR Login.
    //
    // We deliberately include only what the frontend needs:
    //   - The JWT token (to attach to future Authorization headers)
    //   - Basic profile fields for immediate display
    //   - The role (so the Angular UI can decide what to show)
    //
    // We do NOT include PasswordHash, IsActive, internal IDs from other tables,
    // etc. The User entity has them; the DTO doesn't expose them.
    public class AuthResponseDto
    {
        public string Token { get; set; } = null!;
        public int UserId { get; set; }
        public string FullName { get; set; } = null!;
        public string Username { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string Role { get; set; } = null!;   // string for JSON friendliness
    }
}