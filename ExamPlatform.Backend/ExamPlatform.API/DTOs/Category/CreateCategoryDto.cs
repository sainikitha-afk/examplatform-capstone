using System.ComponentModel.DataAnnotations;

namespace ExamPlatform.API.DTOs.Category
{
    // POST /api/categories request body.
    public class CreateCategoryDto
    {
        [Required(ErrorMessage = "Category name is required.")]
        [StringLength(100, MinimumLength = 2)]
        public string CatName { get; set; } = null!;

        [StringLength(500)]
        public string? Description { get; set; }
    }
}