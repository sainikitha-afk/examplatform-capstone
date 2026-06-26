// CA1848: standard ILogger extension methods used deliberately — see CertificateService.cs
#pragma warning disable CA1848
using Microsoft.Extensions.Logging;
using ExamPlatform.BLL.Exceptions;
using ExamPlatform.BLL.Interfaces;
using ExamPlatform.DAL.Interfaces;
using ExamPlatform.Domain.Entities;

namespace ExamPlatform.BLL.Services
{
    public class ExamCategoryService : IExamCategoryService
    {
        private readonly IExamCategoryRepository _categoryRepository;

        // ILogger<T>: tagged with ExamCategoryService so log entries are filterable.
        // Injected by DI — no manual setup needed; the ASP.NET Core pipeline
        // registers ILogger<T> for all T automatically.
        private readonly ILogger<ExamCategoryService> _logger;

        public ExamCategoryService(
            IExamCategoryRepository categoryRepository,
            ILogger<ExamCategoryService> logger)
        {
            _categoryRepository = categoryRepository;
            _logger = logger;
        }

        // ───────────── READ ─────────────

        public async Task<IEnumerable<ExamCategory>> GetAllAsync()
        {
            return await _categoryRepository.GetAllAsync();
        }

        public async Task<ExamCategory> GetByIdAsync(int catId)
        {
            var category = await _categoryRepository.GetByIdAsync(catId);
            // Service contract: "I always return a valid entity or throw."
            if (category == null)
            {
                _logger.LogWarning("Category {CatId} not found.", catId);
                throw new CategoryNotFoundException(catId);
            }
            return category;
        }

        // ───────────── CREATE ─────────────

        public async Task<ExamCategory> CreateAsync(string catName, string? description)
        {
            // Guard clauses first.
            if (string.IsNullOrWhiteSpace(catName))
                throw new ValidationException("Category name is required.");

            // Normalize — strip whitespace. (We don't lowercase because
            // category names are display-facing: "C# Programming" should
            // stay capitalized.)
            var normalizedName = catName.Trim();

            // Uniqueness check before insert. Cleaner than letting the DB
            // unique constraint fire a raw exception.
            // NOTE: this uses GetAllAsync + LINQ. For a small reference table
            // that's fine. For larger sets, add IExamCategoryRepository
            // .ExistsByNameAsync. For now, KISS.
            var existing = await _categoryRepository.GetAllAsync();
            if (existing.Any(c => c.CatName.Equals(normalizedName, StringComparison.OrdinalIgnoreCase)))
            {
                _logger.LogWarning(
                    "Create category failed: '{CatName}' already exists.", normalizedName);
                throw new DuplicateCategoryException(normalizedName);
            }

            // Build and persist.
            var category = new ExamCategory
            {
                CatName = normalizedName,
                Description = description?.Trim()
            };

            await _categoryRepository.AddAsync(category);
            await _categoryRepository.SaveChangesAsync();

            _logger.LogInformation(
                "Category created: catId={CatId}, name='{CatName}'.",
                category.CatId, category.CatName);

            return category;
        }

        // ───────────── BULK CREATE ─────────────

        public async Task<IEnumerable<ExamCategory>> BulkCreateAsync(
            IEnumerable<(string CatName, string? Description)> categories)
        {
            var list = categories.ToList(); // materialise once — avoid multiple enumeration
            if (!list.Any())
                throw new ValidationException("At least one category is required.");

            // ─── PHASE 1: VALIDATE ALL NAMES UPFRONT ─────────────────
            // Load existing categories once so we can check uniqueness without
            // hitting the DB per-item. For a small reference table this is fine.
            var existing = (await _categoryRepository.GetAllAsync())
                .Select(c => c.CatName.ToLowerInvariant())
                .ToHashSet(); // O(1) lookup

            // Track names we've already seen within this batch (catch intra-batch duplicates).
            var seenInBatch = new HashSet<string>();

            for (int i = 0; i < list.Count; i++)
            {
                var (name, _) = list[i];

                if (string.IsNullOrWhiteSpace(name))
                    throw new ValidationException($"Category {i + 1}: name is required.");

                var trimmed = name.Trim();
                if (trimmed.Length < 2 || trimmed.Length > 100)
                    throw new ValidationException(
                        $"Category {i + 1}: name must be between 2 and 100 characters.");

                var key = trimmed.ToLowerInvariant();

                // Check against already-persisted categories.
                if (existing.Contains(key))
                {
                    _logger.LogWarning(
                        "Bulk create: category '{CatName}' already exists in DB.", trimmed);
                    throw new DuplicateCategoryException(trimmed);
                }

                // Check within the current batch — catch duplicates in the same request.
                if (!seenInBatch.Add(key))
                    throw new ValidationException(
                        $"Category '{trimmed}' appears more than once in the request.");
            }

            // ─── PHASE 2: BUILD AND INSERT ALL ────────────────────────
            // EF Core's implicit transaction wraps all pending AddAsync calls in one
            // BEGIN/COMMIT — if any row fails a DB constraint, all are rolled back.
            var entities = list.Select(item => new ExamCategory
            {
                CatName = item.CatName.Trim(),
                Description = item.Description?.Trim()
            }).ToList();

            foreach (var entity in entities)
                await _categoryRepository.AddAsync(entity);

            // Single SaveChangesAsync: all inserts in one round-trip + one implicit TX.
            await _categoryRepository.SaveChangesAsync();

            _logger.LogInformation(
                "Bulk category creation succeeded: {Count} categories inserted.",
                entities.Count);

            return entities;
        }

        // ───────────── UPDATE ─────────────

        public async Task<ExamCategory> UpdateAsync(int catId, string catName, string? description)
        {
            if (string.IsNullOrWhiteSpace(catName))
                throw new ValidationException("Category name is required.");

            // Load — throws if not found.
            var category = await GetByIdAsync(catId);

            var normalizedName = catName.Trim();

            // If the name is changing, ensure the new name isn't taken by
            // ANOTHER category. Same name as itself is fine.
            if (!category.CatName.Equals(normalizedName, StringComparison.OrdinalIgnoreCase))
            {
                var all = await _categoryRepository.GetAllAsync();
                if (all.Any(c => c.CatId != catId
                            && c.CatName.Equals(normalizedName, StringComparison.OrdinalIgnoreCase)))
                {
                    _logger.LogWarning(
                        "Update category {CatId} failed: name '{CatName}' already taken.",
                        catId, normalizedName);
                    throw new DuplicateCategoryException(normalizedName);
                }
            }

            // Mutate the tracked entity. EF Core's change tracker notices
            // the modification; SaveChanges issues an UPDATE.
            var oldName = category.CatName;
            category.CatName = normalizedName;
            category.Description = description?.Trim();

            _categoryRepository.Update(category);
            await _categoryRepository.SaveChangesAsync();

            _logger.LogInformation(
                "Category {CatId} updated: '{OldName}' → '{NewName}'.",
                catId, oldName, normalizedName);

            return category;
        }

        // ───────────── DELETE ─────────────

        public async Task DeleteAsync(int catId)
        {
            var category = await GetByIdAsync(catId);

            // BUSINESS RULE: We configured OnDelete(DeleteBehavior.Restrict)
            // for Question.CatId and Exam.CatId. If this category is referenced,
            // PostgreSQL will REJECT the DELETE with a foreign key violation.
            //
            // For now we let that raw exception surface — the middleware will
            // catch it as a 500. In a more polished version we'd pre-check
            // for references and throw a friendlier exception. KISS for now.
            _categoryRepository.Delete(category);
            await _categoryRepository.SaveChangesAsync();

            _logger.LogInformation(
                "Category {CatId} ('{CatName}') deleted.", catId, category.CatName);
        }
    }
}
