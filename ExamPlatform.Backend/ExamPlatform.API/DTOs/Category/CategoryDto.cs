namespace ExamPlatform.API.DTOs.Category
{
    // Response shape for category endpoints.
    // What we send TO the client.
    public class CategoryDto
    {
        public int CatId { get; set; }
        public string CatName { get; set; } = null!;
        public string? Description { get; set; }

        // Populated only by GET /api/categories (the board/list view); 0 on
        // GET /api/categories/{id} where it isn't computed.
        public int PublishedExamCount { get; set; }
    }
}