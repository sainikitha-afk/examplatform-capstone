namespace ExamPlatform.API.DTOs.Category
{
    // Response shape for category endpoints.
    // What we send TO the client.
    public class CategoryDto
    {
        public int CatId { get; set; }
        public string CatName { get; set; } = null!;
        public string? Description { get; set; }
    }
}