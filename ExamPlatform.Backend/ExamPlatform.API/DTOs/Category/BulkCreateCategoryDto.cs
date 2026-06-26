using System.ComponentModel.DataAnnotations;

namespace ExamPlatform.API.DTOs.Category
{
    // POST /api/categories/bulk request body.
    // Wraps a list of CreateCategoryDto so the Admin can submit many categories
    // in a single request rather than making N sequential POST /api/categories calls.
    public class BulkCreateCategoryDto
    {
        // Each item reuses CreateCategoryDto validation attributes ([Required], [StringLength]).
        // ASP.NET model binding validates every element before the action runs.
        [Required]
        [MinLength(1, ErrorMessage = "At least one category is required.")]
        public List<CreateCategoryDto> Categories { get; set; } = new();
    }
}
