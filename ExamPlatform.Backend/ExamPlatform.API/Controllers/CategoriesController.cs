// CA1848: standard ILogger extension methods used deliberately — see CertificateService.cs
#pragma warning disable CA1848
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using ExamPlatform.API.DTOs.Category;
using ExamPlatform.BLL.Interfaces;
using ExamPlatform.Domain.Entities;

namespace ExamPlatform.API.Controllers
{
    // [Authorize] WITHOUT a role means "any authenticated user can access
    // these endpoints by default." We override per-action below for the
    // admin-only mutations (POST/PUT/DELETE).
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class CategoriesController : ControllerBase
    {
        private readonly IExamCategoryService _categoryService;
        private readonly ILogger<CategoriesController> _logger;

        public CategoriesController(
            IExamCategoryService categoryService,
            ILogger<CategoriesController> logger)
        {
            _categoryService = categoryService;
            _logger = logger;
        }

        // ════════════════════════════════════════════════════════════════
        //   GET /api/categories
        //   GET /api/categories/{id}
        //   — any authenticated user
        // ════════════════════════════════════════════════════════════════

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var categories = await _categoryService.GetAllWithExamCountsAsync();
            return Ok(categories.Select(c => ToDto(c.Category, c.PublishedExamCount)));
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var category = await _categoryService.GetByIdAsync(id);
            return Ok(ToDto(category));
        }

        // ════════════════════════════════════════════════════════════════
        //   POST /api/categories  — create a single category (Admin only)
        // ════════════════════════════════════════════════════════════════

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create([FromBody] CreateCategoryDto dto)
        {
            var category = await _categoryService.CreateAsync(dto.CatName, dto.Description);

            // 201 Created with a Location header pointing at GET /api/categories/{id}
            // CreatedAtAction builds the URL based on the named action.
            _logger.LogInformation(
                "POST /api/categories: created catId={CatId} ('{CatName}').",
                category.CatId, category.CatName);

            return CreatedAtAction(
                nameof(GetById),
                new { id = category.CatId },
                ToDto(category));
        }

        // ════════════════════════════════════════════════════════════════
        //   POST /api/categories/bulk  — create multiple categories at once
        //   — Admin only
        //
        //   MUST be declared BEFORE {id:int} to avoid ASP.NET routing ambiguity.
        //   Accepts a JSON body: { "categories": [ { "catName": "...", "description": "..." }, ... ] }
        //   All names validated upfront; first failure rejects the whole batch.
        //   Duplicate detection: against existing categories AND within the batch.
        //   Returns 200 with the array of created CategoryDto objects.
        //
        //   Example request body:
        //   {
        //     "categories": [
        //       { "catName": "Mathematics",  "description": "Algebra, calculus, statistics" },
        //       { "catName": "Physics",      "description": "Mechanics and thermodynamics" },
        //       { "catName": "Programming",  "description": null }
        //     ]
        //   }
        // ════════════════════════════════════════════════════════════════

        /// <summary>
        /// Creates multiple exam categories in a single atomic request.
        /// Validates all entries before any insert. Rejects the batch if any
        /// name is blank, too short/long, already exists, or appears twice in
        /// the same request.
        /// </summary>
        [HttpPost("bulk")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> BulkCreate([FromBody] BulkCreateCategoryDto dto)
        {
            if (dto.Categories == null || !dto.Categories.Any())
                return BadRequest(new { error = "At least one category is required." });

            // Project the DTO list into the tuple form the service expects.
            var items = dto.Categories
                .Select(c => (CatName: c.CatName, Description: c.Description));

            var created = await _categoryService.BulkCreateAsync(items);

            // 200 OK with the full list. Not 201 because we don't have a single
            // Location header to point to — multiple resources were created.
            return Ok(created.Select(ToDto));
        }

        // ════════════════════════════════════════════════════════════════
        //   PUT /api/categories/{id}
        //   — Admin only
        // ════════════════════════════════════════════════════════════════

        [HttpPut("{id:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateCategoryDto dto)
        {
            var category = await _categoryService.UpdateAsync(id, dto.CatName, dto.Description);
            return Ok(ToDto(category));
        }

        // ════════════════════════════════════════════════════════════════
        //   DELETE /api/categories/{id}
        //   — Admin only
        // ════════════════════════════════════════════════════════════════

        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            await _categoryService.DeleteAsync(id);
            // 204 No Content — successful delete, no body.
            return NoContent();
        }

        // ───────────── PRIVATE MAPPER ─────────────
        // Entity → DTO mapping. Lives here because no other controller needs it.
        // If reuse pops up, move to a Mappers/ folder.
        private static CategoryDto ToDto(ExamCategory entity, int publishedExamCount = 0) => new()
        {
            CatId = entity.CatId,
            CatName = entity.CatName,
            Description = entity.Description,
            PublishedExamCount = publishedExamCount
        };
    }
}