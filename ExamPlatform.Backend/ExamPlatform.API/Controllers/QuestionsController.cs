// QuestionsController exposes the Examiner-managed MCQ question bank.
// Examiners manage their own questions; Admins can view all questions and
// perform governance actions (update/delete any question).
// Candidates never interact with this controller (exam questions are
// delivered through the ExamsController/AttemptsController instead).

// CA1848: standard ILogger extension methods used deliberately — see CertificateService.cs
#pragma warning disable CA1848
using System.Security.Claims;
using ClosedXML.Excel;          // still used by GetImportTemplate
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using ExamPlatform.API.DTOs.Common;
using ExamPlatform.API.DTOs.Question;
using ExamPlatform.BLL.Interfaces;
using ExamPlatform.Domain.Entities;
using ExamPlatform.Domain.Enums;

namespace ExamPlatform.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // every endpoint in this controller requires a valid JWT
    public class QuestionsController : ControllerBase
    {
        private readonly IQuestionService _questionService;
        // Category service: the import template lists the real category names
        // so examiners never have to know database ids.
        private readonly IExamCategoryService _categoryService;
        private readonly ILogger<QuestionsController> _logger;

        public QuestionsController(
            IQuestionService questionService,
            IExamCategoryService categoryService,
            ILogger<QuestionsController> logger)
        {
            _questionService = questionService;
            _categoryService = categoryService;
            _logger = logger;
        }

        // ════════════════════════════════════════════════════════════════
        //   GET /api/questions/stats  — question bank statistics
        //
        //   MUST be declared before {id:int} so ASP.NET routing resolves
        //   the literal segment "stats" before trying to parse it as int.
        //   (With {id:int} the router only matches integers, so "stats"
        //    wouldn't match anyway — but explicit ordering is clearer.)
        //
        //   Examiner: returns counts for their own question bank only.
        //   Admin:    returns platform-wide counts.
        // ════════════════════════════════════════════════════════════════
        [HttpGet("stats")]
        [Authorize(Roles = "Examiner,Admin")]
        public async Task<IActionResult> GetStats()
        {
            var (userId, role) = ExtractCaller();
            var stats = await _questionService.GetStatsAsync(userId, role);

            // Map BLL value object → response DTO.
            return Ok(new QuestionStatsDto
            {
                TotalQuestions = stats.TotalQuestions,
                Easy           = stats.Easy,
                Medium         = stats.Medium,
                Hard           = stats.Hard,
                ByCategory     = stats.ByCategory.Select(c => new CategoryQuestionCountDto
                {
                    CategoryId   = c.CategoryId,
                    CategoryName = c.CategoryName,
                    Count        = c.Count
                }).ToList()
            });
        }

        // ════════════════════════════════════════════════════════════════
        //   GET /api/questions/import-template  — download Excel template
        //
        //   Returns a pre-formatted .xlsx the examiner fills offline and
        //   POSTs back to /api/questions/import.
        //   MUST be declared before {id:int} (same reason as /stats).
        // ════════════════════════════════════════════════════════════════
        [HttpGet("import-template")]
        [Authorize(Roles = "Examiner,Admin")]
        public async Task<IActionResult> GetImportTemplate()
        {
            // Build the template workbook in memory; no server-side storage needed.
            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Questions");

            // ── Header row ──────────────────────────────────────────────
            // Column A takes the category NAME (what examiners see in the app).
            // A numeric id is still accepted for backwards compatibility.
            ws.Cell(1, 1).Value = "Category";
            ws.Cell(1, 2).Value = "QuestionText";
            ws.Cell(1, 3).Value = "Marks";
            ws.Cell(1, 4).Value = "Difficulty";   // Easy | Medium | Hard
            ws.Cell(1, 5).Value = "OptionA";
            ws.Cell(1, 6).Value = "OptionB";
            ws.Cell(1, 7).Value = "OptionC";
            ws.Cell(1, 8).Value = "OptionD";
            ws.Cell(1, 9).Value = "CorrectOption"; // A | B | C | D

            // Bold the header so it's visually distinct.
            ws.Row(1).Style.Font.Bold = true;

            // ── Example row using a REAL category from this database ────
            var categories = (await _categoryService.GetAllAsync()).ToList();
            var sampleCategory = categories.FirstOrDefault()?.CatName ?? "Web Development";

            ws.Cell(2, 1).Value = sampleCategory;
            ws.Cell(2, 2).Value = "What is Dependency Injection primarily used for?";
            ws.Cell(2, 3).Value = 10;
            ws.Cell(2, 4).Value = "Medium";
            ws.Cell(2, 5).Value = "Decoupling object creation from usage";
            ws.Cell(2, 6).Value = "Compressing database queries";
            ws.Cell(2, 7).Value = "Validating HTTP requests";
            ws.Cell(2, 8).Value = "Encrypting configuration files";
            ws.Cell(2, 9).Value = "A";

            // Auto-fit column widths for readability.
            ws.Columns().AdjustToContents();

            // ── Second sheet: every valid category, so examiners can copy
            //    the exact names without ever knowing database ids ────────
            var catSheet = workbook.Worksheets.Add("Categories");
            catSheet.Cell(1, 1).Value = "Category Name (use this in column A)";
            catSheet.Cell(1, 2).Value = "Description";
            catSheet.Row(1).Style.Font.Bold = true;
            for (int i = 0; i < categories.Count; i++)
            {
                catSheet.Cell(i + 2, 1).Value = categories[i].CatName;
                catSheet.Cell(i + 2, 2).Value = categories[i].Description ?? string.Empty;
            }
            catSheet.Columns().AdjustToContents();

            // Serialise to a byte array and return as a file download.
            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            var bytes = stream.ToArray();

            return File(
                bytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "questions_template.xlsx");
        }

        // ════════════════════════════════════════════════════════════════
        //   GET /api/questions  — paginated + filtered question list
        //   Supports: ?page=1 &pageSize=20 &categoryId=N &difficulty=Easy &search=keyword
        //   Examiner: own questions only.  Admin: all questions.
        //   All filtering + pagination happen at the database layer (no in-memory).
        //   pageSize is clamped to [1, 100] inside the service — values > 100 silently
        //   become 100 rather than returning a 400 (a convenience, not a bug).
        // ════════════════════════════════════════════════════════════════
        [HttpGet]
        [Authorize(Roles = "Examiner,Admin")]
        public async Task<IActionResult> GetAll(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] int? categoryId = null,
            [FromQuery] string? difficulty = null,
            [FromQuery] string? search = null)
        {
            var (userId, role) = ExtractCaller();

            _logger.LogInformation(
                "GET /api/questions: user {UserId} page {Page} pageSize {PageSize}.",
                userId, page, pageSize);

            var paged = await _questionService.GetAllPagedAsync(
                userId, role, page, pageSize, categoryId, difficulty, search);

            // Map BLL PagedResult<Question> → PagedResultDto<QuestionDto>.
            return Ok(new PagedResultDto<QuestionDto>
            {
                Items          = paged.Items.Select(ToDto),
                Page           = paged.Page,
                PageSize       = paged.PageSize,
                TotalCount     = paged.TotalCount,
                TotalPages     = paged.TotalPages,
                HasNextPage    = paged.HasNextPage,
                HasPreviousPage = paged.HasPreviousPage
            });
        }

        // ════════════════════════════════════════════════════════════════
        //   GET /api/questions/{id}  — single question with options
        // ════════════════════════════════════════════════════════════════
        [HttpGet("{id:int}")]
        [Authorize(Roles = "Examiner,Admin")]
        public async Task<IActionResult> GetById(int id)
        {
            var (userId, role) = ExtractCaller();

            // Service enforces ownership for Examiner callers.
            var question = await _questionService.GetByIdWithOptionsAsync(id, userId, role);
            return Ok(ToDto(question));
        }

        // ════════════════════════════════════════════════════════════════
        //   POST /api/questions  — create a new MCQ question
        // ════════════════════════════════════════════════════════════════
        [HttpPost]
        [Authorize(Roles = "Examiner")]
        public async Task<IActionResult> Create([FromBody] CreateQuestionDto dto)
        {
            // ASP.NET validates [Required], [MaxLength], [Range] from the DTO
            // BEFORE this action runs. If invalid, framework returns 400 automatically.

            var (userId, _) = ExtractCaller();
            _logger.LogInformation(
                "POST /api/questions: examiner {UserId} creating question in category {CatId}.",
                userId, dto.CatId);

            var options = dto.Options.Select(o => (o.OptionText, o.IsCorrect));

            var question = await _questionService.CreateAsync(
                userId,
                dto.CatId,
                dto.QuestionText,
                dto.Marks,
                dto.Difficulty,
                options);

            // 201 Created with Location header pointing to the new resource.
            return CreatedAtAction(nameof(GetById), new { id = question.QuestionId }, ToDto(question));
        }

        // ════════════════════════════════════════════════════════════════
        //   POST /api/questions/bulk  — create multiple MCQ questions (JSON)
        //
        //   Accepts a JSON array of CreateQuestionDto objects.
        //   MUST be declared before /import to avoid routing ambiguity
        //   (both are POST with a literal second segment; order matters
        //   only if two routes could match the same literal — they can't
        //   here, but explicit ordering documents intent).
        // ════════════════════════════════════════════════════════════════
        /// <summary>
        /// Creates multiple questions in a single atomic request.
        /// All validated before any inserts; rolls back on any failure.
        /// Includes intra-batch and database duplicate detection.
        /// </summary>
        [HttpPost("bulk")]
        [Authorize(Roles = "Examiner,Admin")]
        public async Task<IActionResult> BulkCreate([FromBody] List<CreateQuestionDto> dtos)
        {
            if (dtos == null || !dtos.Any())
                return BadRequest(new { error = "At least one question is required." });

            var (userId, _) = ExtractCaller();

            var questionData = dtos.Select(dto => (
                CatId: dto.CatId,
                QuestionText: dto.QuestionText,
                Marks: dto.Marks,
                Difficulty: dto.Difficulty,
                Options: dto.Options.Select(o => (o.OptionText, o.IsCorrect))
            )).ToList();

            var questions = await _questionService.BulkCreateAsync(userId, questionData);

            return Ok(questions.Select(ToDto));
        }

        // ════════════════════════════════════════════════════════════════
        //   POST /api/questions/import  — import questions from Excel file
        //
        //   Accepts multipart/form-data with the field name "file" = .xlsx workbook.
        //   Expected column layout (row 1 = header, row 2+ = data):
        //     A: CategoryId | B: QuestionText | C: Marks | D: Difficulty
        //     E: OptionA | F: OptionB | G: OptionC | H: OptionD
        //     I: CorrectOption (A | B | C | D)
        //
        //   Uses continue-on-error semantics: every row is attempted independently.
        //   Per-row failures are collected; the caller sees SuccessCount + Errors list.
        //
        //   WHY ExcelImportDto instead of bare IFormFile?
        //   Swashbuckle 10.x crashes generating swagger.json when IFormFile is a bare
        //   [FromForm] parameter. Wrapping it as a class property (ExcelImportDto.File)
        //   lets Swashbuckle auto-generate the multipart/form-data schema correctly.
        // ════════════════════════════════════════════════════════════════
        /// <summary>
        /// Imports questions from an Excel workbook (.xlsx).
        /// Returns ImportResultDto with SuccessCount, FailureCount, Errors, and
        /// CreatedQuestionIds.  Individual row failures do NOT stop the import.
        /// </summary>
        [HttpPost("import")]
        [Consumes("multipart/form-data")] // tells ASP.NET + Swashbuckle: this is a file upload
        [Authorize(Roles = "Examiner,Admin")]
        [ProducesResponseType(typeof(ImportResultDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ImportFromExcel([FromForm] ExcelImportDto dto)
        {
            // ─── GUARD: null / empty / wrong extension ─────────────────
            if (dto.File == null || dto.File.Length == 0)
                return BadRequest(new { error = "No file uploaded." });

            if (!dto.File.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
                return BadRequest(new { error = "Only .xlsx files are supported." });

            var (userId, _) = ExtractCaller();

            _logger.LogInformation(
                "POST /api/questions/import: user {UserId} uploading '{FileName}' ({Bytes} bytes).",
                userId, dto.File.FileName, dto.File.Length);

            // ─── DELEGATE TO SERVICE ───────────────────────────────────
            // The service owns all Excel parsing and business-rule validation.
            // It opens the stream, reads each row, and collects per-row errors
            // rather than throwing on the first failure (continue-on-error semantics).
            using var stream = dto.File.OpenReadStream();
            var importResult = await _questionService.ImportFromExcelAsync(userId, stream);

            _logger.LogInformation(
                "POST /api/questions/import: user {UserId} result — {Success} created, {Fail} failed.",
                userId, importResult.SuccessCount, importResult.FailureCount);

            // ─── MAP TO DTO ────────────────────────────────────────────
            return Ok(new ImportResultDto
            {
                SuccessCount       = importResult.SuccessCount,
                FailureCount       = importResult.FailureCount,
                Errors             = importResult.Errors,
                CreatedQuestionIds = importResult.CreatedQuestionIds
            });
        }

        // ════════════════════════════════════════════════════════════════
        //   PUT /api/questions/{id}  — replace question text + all options
        //   Examiner: own questions only.
        //   Admin: any question (governance / content moderation).
        // ════════════════════════════════════════════════════════════════
        [HttpPut("{id:int}")]
        [Authorize(Roles = "Examiner,Admin")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateQuestionDto dto)
        {
            // Extract both userId and role — service uses role to decide ownership check.
            var (userId, role) = ExtractCaller();

            var options = dto.Options.Select(o => (o.OptionText, o.IsCorrect));

            var question = await _questionService.UpdateAsync(
                id, userId, role, dto.QuestionText, dto.Marks, dto.Difficulty, options);

            return Ok(ToDto(question));
        }

        // ════════════════════════════════════════════════════════════════
        //   DELETE /api/questions/{id}  — soft-delete (IsActive = false)
        //   Examiner: own questions only.
        //   Admin: any question (governance / content moderation).
        // ════════════════════════════════════════════════════════════════
        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Examiner,Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            // Extract both userId and role — service uses role to decide ownership check.
            var (userId, role) = ExtractCaller();

            await _questionService.SoftDeleteAsync(id, userId, role);

            // 204 No Content — the resource is logically deleted; nothing to return.
            return NoContent();
        }

        // ───────────── PRIVATE HELPERS ─────────────

        /// <summary>
        /// Extracts the caller's UserId (from "sub" claim) and UserRole
        /// (from ClaimTypes.Role) out of the JWT that ASP.NET already validated.
        /// </summary>
        private (int UserId, UserRole Role) ExtractCaller()
        {
            // The JWT generator stores UserId as the "sub" (Subject) claim.
            // ASP.NET maps "sub" to ClaimTypes.NameIdentifier during token validation.
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                            ?? User.FindFirst("sub")?.Value
                            ?? User.FindFirst("nameid")?.Value;

            if (!int.TryParse(userIdStr, out var userId))
                throw new UnauthorizedAccessException("Invalid token: missing user id claim.");

            // Role claim was stored as the enum name string ("Admin", "Examiner", etc.)
            var roleStr = User.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty;
            var role = Enum.TryParse<UserRole>(roleStr, out var parsedRole)
                ? parsedRole
                : UserRole.Candidate; // safe default — service will restrict access anyway

            return (userId, role);
        }

        /// <summary>
        /// Maps a Question entity (with Options loaded) to a QuestionDto.
        /// Private static — no instance state needed; static prevents accidental
        /// use of injected services inside the mapper.
        /// </summary>
        private static QuestionDto ToDto(Question q) => new()
        {
            QuestionId = q.QuestionId,
            ExaminerId = q.ExaminerId,
            CatId = q.CatId,
            QuestionText = q.QuestionText,
            Marks = q.Marks,
            Difficulty = q.Difficulty,
            CreatedAt = q.CreatedAt,
            IsActive = q.IsActive,
            // Options may be null if the entity was loaded without Include.
            // In that case we return an empty list rather than crashing.
            Options = q.Options?.Select(o => new QuestionOptionDto
            {
                OptionId = o.OptionId,
                OptionText = o.OptionText,
                IsCorrect = o.IsCorrect
            }).ToList() ?? new List<QuestionOptionDto>()
        };
    }
}
