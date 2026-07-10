// QuestionService enforces the MCQ question-bank business rules:
// 4 options per question, exactly 1 correct, marks > 0, examiner ownership,
// soft-delete guard against live-exam usage, duplicate detection.
// Sits between the API layer (DTOs + HTTP) and the DAL layer (EF Core).

// CA1848: standard ILogger extension methods used deliberately — see CertificateService.cs
#pragma warning disable CA1848
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ExamPlatform.BLL.Exceptions;
using ExamPlatform.BLL.Interfaces;
using ExamPlatform.BLL.Models;
using ExamPlatform.DAL.Contexts;
using ExamPlatform.DAL.Interfaces;
using ExamPlatform.Domain.Entities;
using ExamPlatform.Domain.Enums;

namespace ExamPlatform.BLL.Services
{
    public class QuestionService : IQuestionService
    {
        // ───────────── DEPENDENCIES ─────────────
        private readonly IQuestionRepository _questionRepository;
        private readonly IQuestionOptionRepository _optionRepository;
        private readonly IExamCategoryRepository _categoryRepository;
        private readonly ExamPlatformContext _context;     // for bulk transaction only
        private readonly ILogger<QuestionService> _logger;

        // DI injects all dependencies. They share the same scoped DbContext
        // (one per HTTP request), so SaveChangesAsync on either commits
        // ALL pending changes. The context is injected separately for explicit
        // transaction orchestration in BulkCreateAsync.
        public QuestionService(
            IQuestionRepository questionRepository,
            IQuestionOptionRepository optionRepository,
            IExamCategoryRepository categoryRepository,
            ExamPlatformContext context,
            ILogger<QuestionService> logger)
        {
            _questionRepository = questionRepository;
            _optionRepository = optionRepository;
            _categoryRepository = categoryRepository;
            _context = context;
            _logger = logger;
        }

        // ════════════════════════════════════════════════════════════════
        //   GET ALL (with optional server-side filters)
        // ════════════════════════════════════════════════════════════════
        /// <summary>
        /// Admins see all questions; Examiners see only their own.
        /// All filters are pushed to the database via GetFilteredAsync.
        /// </summary>
        public async Task<IEnumerable<Question>> GetAllAsync(
            int requesterId,
            UserRole requesterRole,
            int? categoryId = null,
            string? difficulty = null,
            string? search = null)
        {
            // Admin: examinerId = null → no ownership filter, sees all questions.
            // Examiner: examinerId = requesterId → scoped to their active questions.
            var examinerId = requesterRole == UserRole.Admin ? (int?)null : requesterId;

            return await _questionRepository.GetFilteredAsync(examinerId, categoryId, difficulty, search);
        }

        // ════════════════════════════════════════════════════════════════
        //   GET ALL PAGED (with optional server-side filters)
        // ════════════════════════════════════════════════════════════════
        /// <summary>
        /// Paginated + filtered question list. pageSize clamped to [1, 100].
        /// </summary>
        public async Task<PagedResult<Question>> GetAllPagedAsync(
            int requesterId,
            UserRole requesterRole,
            int page,
            int pageSize,
            int? categoryId = null,
            string? difficulty = null,
            string? search = null)
        {
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 100);

            var examinerId = requesterRole == UserRole.Admin ? (int?)null : requesterId;

            _logger.LogInformation(
                "Question list page {Page} (pageSize={PageSize}) for user {UserId}.",
                page, pageSize, requesterId);

            var (items, totalCount) = await _questionRepository.GetFilteredPagedAsync(
                examinerId, categoryId, difficulty, search, page, pageSize);

            return new PagedResult<Question>
            {
                Items = items,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount
            };
        }

        // ════════════════════════════════════════════════════════════════
        //   GET BY ID WITH OPTIONS
        // ════════════════════════════════════════════════════════════════
        /// <summary>
        /// Returns the question with its 4 options eagerly loaded.
        /// Examiners can only view their own questions.
        /// </summary>
        public async Task<Question> GetByIdWithOptionsAsync(
            int questionId, int requesterId, UserRole requesterRole)
        {
            var question = await _questionRepository.GetByIdWithOptionsAsync(questionId);

            // Treat a missing question and an unauthorized question identically
            // (HTTP 404 for both). This prevents an examiner from discovering
            // that a question id exists even if they don't own it.
            if (question == null)
                throw new QuestionNotFoundException(questionId);

            if (requesterRole == UserRole.Examiner && question.ExaminerId != requesterId)
            {
                _logger.LogWarning(
                    "Examiner {RequesterId} attempted to access question {QuestionId} they don't own.",
                    requesterId, questionId);
                throw new QuestionNotFoundException(questionId); // same exception — no info leak
            }

            return question;
        }

        // ════════════════════════════════════════════════════════════════
        //   CREATE
        // ════════════════════════════════════════════════════════════════
        /// <summary>
        /// Validates MCQ rules + duplicate check, persists the question and its 4 options.
        /// </summary>
        public async Task<Question> CreateAsync(
            int examinerId,
            int catId,
            string questionText,
            decimal marks,
            string difficulty,
            IEnumerable<(string OptionText, bool IsCorrect)> options)
        {
            // ─── STEP 1: GUARD CLAUSES ────────────────────────────────────
            var optionList = options.ToList(); // materialise once; avoid multiple enumeration

            if (string.IsNullOrWhiteSpace(questionText))
                throw new ValidationException("Question text is required.");

            if (marks <= 0)
                throw new ValidationException("Marks must be greater than 0.");

            if (string.IsNullOrWhiteSpace(difficulty))
                throw new ValidationException("Difficulty is required.");

            // ─── STEP 2: MCQ OPTION VALIDATION ───────────────────────────
            // MCQ rule: exactly 4 options.
            if (optionList.Count != 4)
                throw new ValidationException("A question must have exactly 4 options.");

            // MCQ rule: exactly 1 option marked correct.
            var correctCount = optionList.Count(o => o.IsCorrect);
            if (correctCount != 1)
                throw new ValidationException("Exactly one option must be marked as correct.");

            // ─── STEP 3: DUPLICATE CHECK ──────────────────────────────────
            // Business rule: same examiner cannot create the same question
            // (case-insensitive, trimmed) in the same category twice.
            // Checked AFTER format validation so the error message is about the
            // duplicate, not a format issue.
            if (await _questionRepository.IsDuplicateAsync(examinerId, catId, questionText))
            {
                _logger.LogWarning(
                    "Examiner {ExaminerId} attempted to create a duplicate question in category {CatId}.",
                    examinerId, catId);
                throw new DuplicateQuestionException(questionText.Trim());
            }

            // ─── STEP 4: BUILD AND PERSIST THE QUESTION ──────────────────
            var question = new Question
            {
                ExaminerId = examinerId,
                CatId = catId,
                QuestionText = questionText.Trim(),
                Marks = marks,
                Difficulty = difficulty.Trim(),
                CreatedAt = DateTime.UtcNow, // always UTC for consistency
                IsActive = true              // new questions start active
            };

            await _questionRepository.AddAsync(question);
            await _questionRepository.SaveChangesAsync();
            // After SaveChangesAsync, EF Core back-fills question.QuestionId
            // from the DB-generated identity column. We need this Id to set
            // the FK on the options in the next step.

            // ─── STEP 5: PERSIST OPTIONS ─────────────────────────────────
            foreach (var (optionText, isCorrect) in optionList)
            {
                var option = new QuestionOption
                {
                    QuestionId = question.QuestionId, // FK to the question just saved
                    OptionText = optionText.Trim(),
                    IsCorrect = isCorrect
                };
                await _optionRepository.AddAsync(option);
            }
            await _optionRepository.SaveChangesAsync();
            // EF Core wraps all 4 AddAsync calls in one implicit DB transaction —
            // either all 4 options are inserted or none are.

            // ─── STEP 6: RELOAD WITH OPTIONS FOR THE RESPONSE ────────────
            // The question object we built doesn't have the Options nav prop populated
            // yet (EF only populated it if we loaded via Include). Re-fetch so the
            // controller can build the full response DTO.
            _logger.LogInformation(
                "Question {QuestionId} created by examiner {ExaminerId} in category {CatId}.",
                question.QuestionId, examinerId, catId);

            return await _questionRepository.GetByIdWithOptionsAsync(question.QuestionId)
                   ?? question; // fallback (shouldn't happen after successful insert)
        }

        // ════════════════════════════════════════════════════════════════
        //   UPDATE
        // ════════════════════════════════════════════════════════════════
        /// <summary>
        /// Updates question text/marks/difficulty and atomically replaces all options.
        /// </summary>
        public async Task<Question> UpdateAsync(
            int questionId,
            int requesterId,
            UserRole requesterRole,
            string questionText,
            decimal marks,
            string difficulty,
            IEnumerable<(string OptionText, bool IsCorrect)> options)
        {
            // ─── STEP 1: LOAD + OWNERSHIP CHECK ─────────────────────────
            var question = await _questionRepository.GetByIdWithOptionsAsync(questionId);

            // Admin bypasses the ownership check — they manage any question.
            // Examiners: same 404 for "not found" and "not yours" prevents id probing.
            if (question == null)
                throw new QuestionNotFoundException(questionId);

            if (requesterRole != UserRole.Admin && question.ExaminerId != requesterId)
            {
                _logger.LogWarning(
                    "User {RequesterId} (role={Role}) attempted to update question {QuestionId} they don't own.",
                    requesterId, requesterRole, questionId);
                throw new QuestionNotFoundException(questionId);
            }

            // ─── STEP 2: MCQ VALIDATION ─────────────────────────────────
            var optionList = options.ToList();
            if (string.IsNullOrWhiteSpace(questionText))
                throw new ValidationException("Question text is required.");
            if (marks <= 0)
                throw new ValidationException("Marks must be greater than 0.");
            if (optionList.Count != 4)
                throw new ValidationException("A question must have exactly 4 options.");
            if (optionList.Count(o => o.IsCorrect) != 1)
                throw new ValidationException("Exactly one option must be marked as correct.");

            // ─── STEP 3: UPDATE QUESTION FIELDS ─────────────────────────
            question.QuestionText = questionText.Trim();
            question.Marks = marks;
            question.Difficulty = difficulty.Trim();
            _questionRepository.Update(question); // marks entity as Modified

            // ─── STEP 4: REPLACE OPTIONS ATOMICALLY ─────────────────────
            // DeleteByQuestionAsync queues RemoveRange in the change tracker
            // WITHOUT committing. Then we add the 4 new options. Both the
            // deletes and inserts are committed together in the single
            // SaveChangesAsync below — one round-trip, one implicit DB transaction.
            await _optionRepository.DeleteByQuestionAsync(question.QuestionId);

            foreach (var (optionText, isCorrect) in optionList)
            {
                await _optionRepository.AddAsync(new QuestionOption
                {
                    QuestionId = question.QuestionId,
                    OptionText = optionText.Trim(),
                    IsCorrect = isCorrect
                });
            }

            // ─── STEP 5: SINGLE COMMIT ───────────────────────────────────
            // SaveChangesAsync flushes: UPDATE questions + DELETE old options
            // + INSERT new options — all in one transaction.
            await _questionRepository.SaveChangesAsync();

            // Reload to return the updated question with fresh option rows.
            _logger.LogInformation(
                "Question {QuestionId} updated by user {RequesterId} (role={Role}).",
                questionId, requesterId, requesterRole);

            return await _questionRepository.GetByIdWithOptionsAsync(question.QuestionId)
                   ?? question;
        }

        // ════════════════════════════════════════════════════════════════
        //   SOFT DELETE
        // ════════════════════════════════════════════════════════════════
        /// <summary>
        /// Sets IsActive = false on the question (soft-delete).
        /// Throws if the question is used in a Published exam.
        /// </summary>
        public async Task SoftDeleteAsync(int questionId, int requesterId, UserRole requesterRole)
        {
            // ─── STEP 1: LOAD + OWNERSHIP ────────────────────────────────
            var question = await _questionRepository.GetByIdAsync(questionId);

            // Admin can soft-delete any question (governance/moderation role).
            // Examiners are restricted to their own questions only.
            if (question == null)
                throw new QuestionNotFoundException(questionId);

            if (requesterRole != UserRole.Admin && question.ExaminerId != requesterId)
            {
                _logger.LogWarning(
                    "User {RequesterId} (role={Role}) attempted to delete question {QuestionId} they don't own.",
                    requesterId, requesterRole, questionId);
                throw new QuestionNotFoundException(questionId);
            }

            // ─── STEP 2: IN-USE GUARD ────────────────────────────────────
            // Business rule: cannot delete a question that is part of a
            // Published exam — removing it would corrupt a live exam.
            if (await _questionRepository.IsUsedInPublishedExamAsync(questionId))
            {
                _logger.LogWarning(
                    "Soft-delete blocked: question {QuestionId} is used in a Published exam.",
                    questionId);
                throw new QuestionInUseException(questionId);
            }

            // ─── STEP 3: SOFT DELETE ─────────────────────────────────────
            // We set the flag rather than calling _questionRepository.Delete()
            // because Delete() queues a hard DELETE (removes the row entirely).
            // Soft-delete preserves the row for audit and for exams in other
            // statuses (Draft/ResultsPublished/Archived) that still reference it.
            question.IsActive = false;
            _questionRepository.Update(question); // queues UPDATE ... SET is_active = false
            await _questionRepository.SaveChangesAsync();

            _logger.LogInformation(
                "Question {QuestionId} soft-deleted by user {RequesterId} (role={Role}).",
                questionId, requesterId, requesterRole);
        }

        // ════════════════════════════════════════════════════════════════
        //   BULK CREATE (TRANSACTIONAL)
        // ════════════════════════════════════════════════════════════════
        /// <summary>
        /// Creates multiple questions in a single transactional batch.
        /// Phase 1: validates ALL questions (format + duplicates) before any DB ops.
        /// Phase 2: transactional insert — rolls back everything on any failure.
        /// Shared by POST /api/questions/bulk and the Excel import endpoint.
        /// </summary>
        public async Task<IEnumerable<Question>> BulkCreateAsync(
            int examinerId,
            IEnumerable<(int CatId, string QuestionText, decimal Marks, string Difficulty, IEnumerable<(string OptionText, bool IsCorrect)> Options)> questions)
        {
            var questionList = questions.ToList(); // materialise once
            if (!questionList.Any())
                throw new ValidationException("At least one question is required.");

            // ─── PHASE 1: VALIDATE ALL QUESTIONS BEFORE ANY DB OPS ────────
            // Two-level duplicate detection:
            //   a) Within this batch (HashSet — O(1) per lookup, no DB round-trip).
            //   b) Against the database (IsDuplicateAsync — one query per item).
            // We validate all items upfront so the client gets a single, complete
            // error rather than partial-insert behaviour.

            // Intra-batch key: (normalised catId, normalised questionText).
            var seenInBatch = new HashSet<(int, string)>();

            var validatedQuestions = new List<(Question Entity, IEnumerable<(string Text, bool IsCorrect)> Options)>();

            for (int i = 0; i < questionList.Count; i++)
            {
                var (catId, questionText, marks, difficulty, options) = questionList[i];
                var optionList = options.ToList();
                var label = $"Question {i + 1}"; // 1-based for user-facing messages

                // ── Format validation ──
                if (string.IsNullOrWhiteSpace(questionText))
                    throw new ValidationException($"{label}: Question text is required.");

                if (marks <= 0)
                    throw new ValidationException($"{label}: Marks must be greater than 0.");

                if (string.IsNullOrWhiteSpace(difficulty))
                    throw new ValidationException($"{label}: Difficulty is required.");

                // ── Category existence ──
                var category = await _categoryRepository.GetByIdAsync(catId);
                if (category == null)
                    throw new ValidationException($"{label}: Category with id {catId} does not exist.");

                // ── MCQ rules ──
                if (optionList.Count != 4)
                    throw new ValidationException($"{label}: A question must have exactly 4 options.");

                if (optionList.Count(o => o.IsCorrect) != 1)
                    throw new ValidationException($"{label}: Exactly one option must be marked as correct.");

                // ── Intra-batch duplicate detection ──
                // Catches "Question 1 == Question 2" within this very request
                // before we even touch the database.
                var batchKey = (catId, questionText.Trim().ToLowerInvariant());
                if (!seenInBatch.Add(batchKey))
                    throw new ValidationException(
                        $"{label}: Duplicate question text '{questionText.Trim()}' for category {catId} within this batch.");

                // ── Database duplicate detection ──
                // Checks against existing active questions for this examiner.
                if (await _questionRepository.IsDuplicateAsync(examinerId, catId, questionText))
                    throw new DuplicateQuestionException(questionText.Trim());

                // All checks passed — build the entity but don't insert yet.
                var questionEntity = new Question
                {
                    ExaminerId = examinerId,
                    CatId = catId,
                    QuestionText = questionText.Trim(),
                    Marks = marks,
                    Difficulty = difficulty.Trim(),
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                };

                validatedQuestions.Add((questionEntity, optionList));
            }

            // ─── PHASE 2: TRANSACTIONAL INSERT ────────────────────────────
            // Explicit transaction: if ANY insert fails (DB constraint, etc.),
            // ALL inserts from both question and option tables are rolled back.
            await using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // Insert all questions first (we need their DB-generated IDs
                // to set the FK on the option rows).
                foreach (var (question, _) in validatedQuestions)
                    await _questionRepository.AddAsync(question);

                // One SaveChanges → EF Core back-fills all QuestionIds from the DB.
                await _questionRepository.SaveChangesAsync();

                // Insert all options for all questions.
                foreach (var (question, options) in validatedQuestions)
                {
                    foreach (var (optionText, isCorrect) in options)
                    {
                        await _optionRepository.AddAsync(new QuestionOption
                        {
                            QuestionId = question.QuestionId, // now populated
                            OptionText = optionText.Trim(),
                            IsCorrect = isCorrect
                        });
                    }
                }

                // One SaveChanges → all option rows inserted in a single round-trip.
                await _optionRepository.SaveChangesAsync();

                await transaction.CommitAsync();

                _logger.LogInformation(
                    "BulkCreateAsync created {Count} questions for examiner {ExaminerId}.",
                    validatedQuestions.Count, examinerId);
            }
            catch
            {
                // Any failure rolls back EVERYTHING inserted in this transaction.
                await transaction.RollbackAsync();
                throw;
            }

            // ─── PHASE 3: RELOAD WITH OPTIONS FOR THE RESPONSE ────────────
            var results = new List<Question>();
            foreach (var (question, _) in validatedQuestions)
            {
                var reloaded = await _questionRepository.GetByIdWithOptionsAsync(question.QuestionId);
                if (reloaded != null)
                    results.Add(reloaded);
            }

            return results;
        }

        // ════════════════════════════════════════════════════════════════
        //   GET STATS
        // ════════════════════════════════════════════════════════════════
        /// <summary>
        /// Returns counts by difficulty and by category for the visible question bank.
        /// Examiner: scoped to their own active questions.
        /// Admin: platform-wide active questions.
        /// </summary>
        public async Task<QuestionStats> GetStatsAsync(int requesterId, UserRole requesterRole)
        {
            // GetForStatsAsync eagerly loads Category so we can read CatName
            // without triggering N+1 queries in the LINQ grouping below.
            var examinerId = requesterRole == UserRole.Admin ? (int?)null : requesterId;
            var questions = (await _questionRepository.GetForStatsAsync(examinerId)).ToList();

            // Difficulty counts. ToLower() comparison is case-insensitive so
            // "Easy", "easy", "EASY" all count in the same bucket.
            var easy   = questions.Count(q => q.Difficulty.ToLowerInvariant() == "easy");
            var medium = questions.Count(q => q.Difficulty.ToLowerInvariant() == "medium");
            var hard   = questions.Count(q => q.Difficulty.ToLowerInvariant() == "hard");

            // Per-category breakdown. Category nav is eagerly loaded so
            // accessing q.Category.CatName is safe (no lazy-load hit).
            var byCategory = questions
                .GroupBy(q => new { q.CatId, q.Category.CatName })
                .Select(g => new CategoryQuestionCount(g.Key.CatId, g.Key.CatName, g.Count()))
                .OrderByDescending(c => c.Count) // most questions first
                .ToList();

            return new QuestionStats(
                TotalQuestions: questions.Count,
                Easy: easy,
                Medium: medium,
                Hard: hard,
                ByCategory: byCategory);
        }

        // ════════════════════════════════════════════════════════════════
        //   IMPORT FROM EXCEL
        // ════════════════════════════════════════════════════════════════
        /// <summary>
        /// Imports questions from an Excel (.xlsx) stream.
        ///
        /// Expected column layout (row 1 = header, row 2+ = data):
        ///   A: Category     (category NAME, e.g. "Web Development" — or its id;
        ///                    names are what examiners actually know, ids are
        ///                    still accepted for backwards compatibility)
        ///   B: QuestionText (string)
        ///   C: Marks        (decimal)
        ///   D: Difficulty   (Easy | Medium | Hard)
        ///   E: OptionA      (string)
        ///   F: OptionB      (string)
        ///   G: OptionC      (string)
        ///   H: OptionD      (string)
        ///   I: CorrectOption (A | B | C | D)
        ///
        /// Uses continue-on-error semantics: each row is attempted independently.
        /// Per-row failures are collected into ImportResult.Errors ("Row N: reason").
        /// Succeeds even if some rows fail; the caller inspects SuccessCount/FailureCount.
        /// </summary>
        public async Task<ImportResult> ImportFromExcelAsync(int examinerId, Stream excelStream)
        {
            _logger.LogInformation(
                "ImportFromExcelAsync started for examiner {ExaminerId}.", examinerId);

            var result = new ImportResult();

            // ClosedXML reads the .xlsx from the provided stream (no temp file on disk).
            using var workbook = new XLWorkbook(excelStream);
            var ws = workbook.Worksheets.FirstOrDefault();

            // An empty workbook or one with no data rows is a no-op (not an error).
            if (ws == null)
            {
                _logger.LogWarning(
                    "ImportFromExcelAsync: workbook has no worksheets for examiner {ExaminerId}.",
                    examinerId);
                return result;
            }

            var lastRow = ws.LastRowUsed()?.RowNumber() ?? 1;

            // Load all categories ONCE for the whole import: column A is resolved
            // against them by name (case-insensitive) or by id, and existence is
            // validated up front instead of failing later on the FK constraint.
            var allCategories = (await _categoryRepository.GetAllAsync()).ToList();
            var categoryByName = allCategories
                .GroupBy(c => c.CatName.Trim(), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First().CatId, StringComparer.OrdinalIgnoreCase);
            var categoryIds = allCategories.Select(c => c.CatId).ToHashSet();

            // Row 1 is the header; data starts at row 2.
            for (int rowNum = 2; rowNum <= lastRow; rowNum++)
            {
                // Helper: read a cell as a trimmed string (empty string if null).
                string Cell(int col) =>
                    ws.Cell(rowNum, col).GetString()?.Trim() ?? string.Empty;

                var rowLabel = $"Row {rowNum}";

                try
                {
                    // ── Column A: Category (name preferred, id accepted) ──
                    var rawCategory = Cell(1);
                    int catId;
                    if (int.TryParse(rawCategory, out var parsedId))
                    {
                        // Numeric value — treat as a category id (legacy format).
                        if (!categoryIds.Contains(parsedId))
                        {
                            AddRowError(result, rowLabel,
                                $"CategoryId {parsedId} does not exist. " +
                                "Tip: you can use the category NAME instead of its id.");
                            continue;
                        }
                        catId = parsedId;
                    }
                    else if (categoryByName.TryGetValue(rawCategory, out var resolvedId))
                    {
                        // Human-friendly path: the category name, as shown in the app.
                        catId = resolvedId;
                    }
                    else
                    {
                        AddRowError(result, rowLabel,
                            string.IsNullOrWhiteSpace(rawCategory)
                                ? "Category is required (use the category name, e.g. \"Web Development\")."
                                : $"Category '{rawCategory}' does not exist. Use the exact category " +
                                  "name from the app (see the Categories sheet in the template).");
                        continue;
                    }

                    // ── Column B: QuestionText ───────────────────────────
                    var questionText = Cell(2);
                    if (string.IsNullOrWhiteSpace(questionText))
                    {
                        AddRowError(result, rowLabel, "QuestionText is required.");
                        continue;
                    }

                    // ── Column C: Marks ──────────────────────────────────
                    if (!decimal.TryParse(Cell(3), out var marks) || marks <= 0)
                    {
                        AddRowError(result, rowLabel, "Marks must be a positive number.");
                        continue;
                    }

                    // ── Column D: Difficulty ─────────────────────────────
                    var difficulty = Cell(4);
                    if (!new[] { "easy", "medium", "hard" }
                            .Contains(difficulty.ToLowerInvariant()))
                    {
                        AddRowError(result, rowLabel, "Difficulty must be Easy, Medium, or Hard.");
                        continue;
                    }

                    // ── Columns E-H: Option texts ────────────────────────
                    var optionA = Cell(5);
                    var optionB = Cell(6);
                    var optionC = Cell(7);
                    var optionD = Cell(8);

                    if (string.IsNullOrWhiteSpace(optionA) || string.IsNullOrWhiteSpace(optionB) ||
                        string.IsNullOrWhiteSpace(optionC) || string.IsNullOrWhiteSpace(optionD))
                    {
                        AddRowError(result, rowLabel,
                            "All four options (A, B, C, D) are required.");
                        continue;
                    }

                    // ── Column I: CorrectOption (A/B/C/D) ────────────────
                    var correctLetter = Cell(9).ToUpperInvariant();
                    if (correctLetter is not ("A" or "B" or "C" or "D"))
                    {
                        AddRowError(result, rowLabel, "CorrectOption must be A, B, C, or D.");
                        continue;
                    }

                    // Map the letter to IsCorrect flags on each option.
                    var options = new (string OptionText, bool IsCorrect)[]
                    {
                        (optionA, correctLetter == "A"),
                        (optionB, correctLetter == "B"),
                        (optionC, correctLetter == "C"),
                        (optionD, correctLetter == "D"),
                    };

                    // ── Delegate to CreateAsync ──────────────────────────
                    // Reuses the same validation and persistence logic used by the
                    // single-question POST endpoint — no duplication of business rules.
                    // CreateAsync throws on duplicate detection or category-not-found.
                    // We catch and convert to a row error instead of stopping the import.
                    var question = await CreateAsync(examinerId, catId, questionText, marks,
                        difficulty, options);

                    result.SuccessCount++;
                    result.CreatedQuestionIds.Add(question.QuestionId);
                }
                catch (Exception ex) when (ex is ValidationException or DuplicateQuestionException
                                                or CategoryNotFoundException)
                {
                    // Known business-rule failure — treat as a row error, continue.
                    AddRowError(result, rowLabel, ex.Message);
                    _logger.LogWarning(
                        "ImportFromExcelAsync: {Row} failed for examiner {ExaminerId}: {Message}.",
                        rowLabel, examinerId, ex.Message);
                }
                catch (Exception ex)
                {
                    // Unexpected error on this row — log at ERROR level and record it,
                    // but continue processing remaining rows rather than crashing the import.
                    AddRowError(result, rowLabel, $"Unexpected error: {ex.Message}");
                    _logger.LogError(
                        "ImportFromExcelAsync: unexpected error on {Row} for examiner {ExaminerId}.",
                        rowLabel, examinerId);
                }
            }

            result.FailureCount = result.Errors.Count;

            _logger.LogInformation(
                "ImportFromExcelAsync completed for examiner {ExaminerId}: " +
                "{SuccessCount} created, {FailureCount} failed.",
                examinerId, result.SuccessCount, result.FailureCount);

            return result;
        }

        // Adds a per-row error in "Row N: reason" format.
        // Private static: no instance state needed; called only from ImportFromExcelAsync.
        private static void AddRowError(ImportResult result, string rowLabel, string reason)
        {
            result.Errors.Add($"{rowLabel}: {reason}");
        }
    }
}
