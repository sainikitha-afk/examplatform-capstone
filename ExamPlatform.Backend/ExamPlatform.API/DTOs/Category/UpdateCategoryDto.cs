using System.ComponentModel.DataAnnotations;

namespace ExamPlatform.API.DTOs.Category
{
    // PUT /api/categories/{id} request body.
    // Currently identical to Create. We keep them separate types because:
    //   1. The two endpoints document differently in Swagger.
    //   2. In future, Create might require fields Update doesn't (e.g.,
    //      a creator-id), so keeping them separated avoids retro-coupling.
    public class UpdateCategoryDto
    {
        [Required]
        [StringLength(100, MinimumLength = 2)]
        public string CatName { get; set; } = null!;

        [StringLength(500)]
        public string? Description { get; set; }
    }
}