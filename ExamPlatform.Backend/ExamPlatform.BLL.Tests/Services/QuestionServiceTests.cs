// QuestionServiceTests.cs — covers MCQ validation, ownership, duplicate detection,
// soft-delete, ImportFromExcelAsync (per-row error collection), and GetAllPagedAsync.
// Key: AddAsync returns Task<T>, SaveChangesAsync returns Task<int>,
//      DeleteByQuestionAsync returns Task (uses ExecuteUpdateAsync-style void return).

using ClosedXML.Excel;
using ExamPlatform.BLL.Exceptions;
using ExamPlatform.BLL.Services;
using ExamPlatform.BLL.Tests.Helpers;
using ExamPlatform.DAL.Contexts;
using ExamPlatform.DAL.Interfaces;
using ExamPlatform.Domain.Entities;
using ExamPlatform.Domain.Enums;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace ExamPlatform.BLL.Tests.Services
{
    [TestFixture]
    public class QuestionServiceTests
    {
        private Mock<IQuestionRepository>      _questionRepoMock = null!;
        private Mock<IQuestionOptionRepository> _optionRepoMock  = null!;
        private Mock<IExamCategoryRepository>  _categoryRepoMock = null!;
        private Mock<ILogger<QuestionService>> _loggerMock       = null!;
        private ExamPlatformContext _dbContext = null!;
        private QuestionService _sut = null!;

        [SetUp]
        public void SetUp()
        {
            _questionRepoMock = new Mock<IQuestionRepository>();
            _optionRepoMock   = new Mock<IQuestionOptionRepository>();
            _categoryRepoMock = new Mock<IExamCategoryRepository>();
            _loggerMock       = new Mock<ILogger<QuestionService>>();

            var options = new DbContextOptionsBuilder<ExamPlatformContext>()
                .UseInMemoryDatabase($"QSvcDb_{Guid.NewGuid()}")
                .Options;
            _dbContext = new ExamPlatformContext(options);

            // Happy-path defaults.
            _questionRepoMock.Setup(r => r.IsDuplicateAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>()))
                             .ReturnsAsync(false);
            _questionRepoMock.Setup(r => r.AddAsync(It.IsAny<Question>()))
                             .ReturnsAsync((Question q) => q);
            _questionRepoMock.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);
            // AddAsync for options returns Task<QuestionOption>
            _optionRepoMock.Setup(r => r.AddAsync(It.IsAny<QuestionOption>()))
                           .ReturnsAsync((QuestionOption o) => o);
            _optionRepoMock.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);
            // DeleteByQuestionAsync returns Task (void-like)
            _optionRepoMock.Setup(r => r.DeleteByQuestionAsync(It.IsAny<int>()))
                           .Returns(Task.CompletedTask);
            _categoryRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<int>()))
                             .ReturnsAsync(TestDataBuilder.BuildCategory(1));

            _sut = new QuestionService(
                _questionRepoMock.Object,
                _optionRepoMock.Object,
                _categoryRepoMock.Object,
                _dbContext,
                _loggerMock.Object);
        }

        [TearDown]
        public void TearDown() => _dbContext.Dispose();

        // ════════════════════════════════════════════════════════════════
        //   GetByIdWithOptionsAsync
        // ════════════════════════════════════════════════════════════════

        [Test]
        public async Task GetByIdWithOptionsAsync_OwnedQuestion_ReturnsQuestion()
        {
            var question = TestDataBuilder.BuildQuestion(1, examinerId: 10);
            _questionRepoMock.Setup(r => r.GetByIdWithOptionsAsync(1)).ReturnsAsync(question);

            var result = await _sut.GetByIdWithOptionsAsync(1, requesterId: 10, UserRole.Examiner);

            result.QuestionId.Should().Be(1);
        }

        [Test]
        public async Task GetByIdWithOptionsAsync_NotOwnedByExaminer_ThrowsQuestionNotFoundException()
        {
            var question = TestDataBuilder.BuildQuestion(1, examinerId: 10);
            _questionRepoMock.Setup(r => r.GetByIdWithOptionsAsync(1)).ReturnsAsync(question);

            var act = async () => await _sut.GetByIdWithOptionsAsync(1, requesterId: 99, UserRole.Examiner);
            await act.Should().ThrowAsync<QuestionNotFoundException>();
            _loggerMock.VerifyWarning(Times.AtLeastOnce());
        }

        [Test]
        public async Task GetByIdWithOptionsAsync_AdminCanViewAnyQuestion()
        {
            var question = TestDataBuilder.BuildQuestion(1, examinerId: 10);
            _questionRepoMock.Setup(r => r.GetByIdWithOptionsAsync(1)).ReturnsAsync(question);

            var result = await _sut.GetByIdWithOptionsAsync(1, requesterId: 99, UserRole.Admin);
            result.QuestionId.Should().Be(1);
        }

        [Test]
        public async Task GetByIdWithOptionsAsync_NonExistentQuestion_ThrowsQuestionNotFoundException()
        {
            _questionRepoMock.Setup(r => r.GetByIdWithOptionsAsync(It.IsAny<int>()))
                             .ReturnsAsync((Question?)null);

            var act = async () => await _sut.GetByIdWithOptionsAsync(999, 10, UserRole.Examiner);
            await act.Should().ThrowAsync<QuestionNotFoundException>();
        }

        // ════════════════════════════════════════════════════════════════
        //   CreateAsync — HAPPY PATH
        // ════════════════════════════════════════════════════════════════

        [Test]
        public async Task CreateAsync_WithValidInput_CreatesQuestionAndOptions()
        {
            var created = TestDataBuilder.BuildQuestion(1, examinerId: 10);
            _questionRepoMock.Setup(r => r.GetByIdWithOptionsAsync(It.IsAny<int>()))
                             .ReturnsAsync(created);

            var result = await _sut.CreateAsync(10, 1, "What is DI?", 10m, "Easy",
                TestDataBuilder.BuildFourOptionTuples());

            result.Should().NotBeNull();
            _questionRepoMock.Verify(r => r.AddAsync(It.IsAny<Question>()), Times.Once);
            _optionRepoMock.Verify(r => r.AddAsync(It.IsAny<QuestionOption>()), Times.Exactly(4));
        }

        // ════════════════════════════════════════════════════════════════
        //   CreateAsync — MCQ VALIDATION
        // ════════════════════════════════════════════════════════════════

        [Test]
        public async Task CreateAsync_WithEmptyQuestionText_ThrowsValidationException()
        {
            var act = async () => await _sut.CreateAsync(10, 1, "", 10m, "Easy",
                TestDataBuilder.BuildFourOptionTuples());
            await act.Should().ThrowAsync<ValidationException>();
            _questionRepoMock.Verify(r => r.AddAsync(It.IsAny<Question>()), Times.Never);
        }

        [TestCase(0, TestName = "ZeroMarks")]
        [TestCase(-5, TestName = "NegativeMarks")]
        public async Task CreateAsync_WithInvalidMarks_ThrowsValidationException(decimal marks)
        {
            var act = async () => await _sut.CreateAsync(10, 1, "Q?", marks, "Easy",
                TestDataBuilder.BuildFourOptionTuples());
            await act.Should().ThrowAsync<ValidationException>();
        }

        [Test]
        public async Task CreateAsync_WithThreeOptions_ThrowsValidationException()
        {
            var threeOptions = new List<(string, bool)> { ("A", true), ("B", false), ("C", false) };

            var act = async () => await _sut.CreateAsync(10, 1, "Q?", 10m, "Easy", threeOptions);
            await act.Should().ThrowAsync<ValidationException>().WithMessage("*exactly 4*");
        }

        [Test]
        public async Task CreateAsync_WithTwoCorrectOptions_ThrowsValidationException()
        {
            var twoCorrect = new List<(string, bool)>
                { ("A", true), ("B", true), ("C", false), ("D", false) };

            var act = async () => await _sut.CreateAsync(10, 1, "Q?", 10m, "Easy", twoCorrect);
            await act.Should().ThrowAsync<ValidationException>().WithMessage("*one option*");
        }

        [Test]
        public async Task CreateAsync_DuplicateQuestion_ThrowsDuplicateQuestionException()
        {
            _questionRepoMock.Setup(r => r.IsDuplicateAsync(10, 1, It.IsAny<string>()))
                             .ReturnsAsync(true);

            var act = async () => await _sut.CreateAsync(10, 1, "Duplicate?", 10m, "Easy",
                TestDataBuilder.BuildFourOptionTuples());
            await act.Should().ThrowAsync<DuplicateQuestionException>();
            _loggerMock.VerifyWarning(Times.AtLeastOnce());
        }

        // ════════════════════════════════════════════════════════════════
        //   UpdateAsync — OWNERSHIP
        // ════════════════════════════════════════════════════════════════

        [Test]
        public async Task UpdateAsync_ExaminerUpdatesOwnQuestion_Succeeds()
        {
            var question = TestDataBuilder.BuildQuestion(1, examinerId: 10);
            _questionRepoMock.Setup(r => r.GetByIdWithOptionsAsync(1)).ReturnsAsync(question);
            _questionRepoMock.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);

            await _sut.UpdateAsync(1, 10, UserRole.Examiner, "Updated?", 15m, "Hard",
                TestDataBuilder.BuildFourOptionTuples());

            _questionRepoMock.Verify(r => r.Update(It.IsAny<Question>()), Times.Once);
            _optionRepoMock.Verify(r => r.DeleteByQuestionAsync(1), Times.Once);
        }

        [Test]
        public async Task UpdateAsync_ExaminerUpdatesOtherExaminersQuestion_ThrowsNotFound()
        {
            var question = TestDataBuilder.BuildQuestion(1, examinerId: 10);
            _questionRepoMock.Setup(r => r.GetByIdWithOptionsAsync(1)).ReturnsAsync(question);

            var act = async () => await _sut.UpdateAsync(1, 99, UserRole.Examiner,
                "Q?", 10m, "Easy", TestDataBuilder.BuildFourOptionTuples());
            await act.Should().ThrowAsync<QuestionNotFoundException>();
        }

        [Test]
        public async Task UpdateAsync_AdminUpdatesAnyQuestion_Succeeds()
        {
            var question = TestDataBuilder.BuildQuestion(1, examinerId: 10);
            _questionRepoMock.Setup(r => r.GetByIdWithOptionsAsync(1)).ReturnsAsync(question);
            _questionRepoMock.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);

            await _sut.UpdateAsync(1, 99, UserRole.Admin, "Updated?", 10m, "Easy",
                TestDataBuilder.BuildFourOptionTuples());

            _questionRepoMock.Verify(r => r.Update(It.IsAny<Question>()), Times.Once);
        }

        // ════════════════════════════════════════════════════════════════
        //   SoftDeleteAsync
        // ════════════════════════════════════════════════════════════════

        [Test]
        public async Task SoftDeleteAsync_ActiveOwnedQuestion_SetsIsActiveFalse()
        {
            var question = TestDataBuilder.BuildQuestion(1, examinerId: 10);
            _questionRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(question);
            _questionRepoMock.Setup(r => r.IsUsedInPublishedExamAsync(1)).ReturnsAsync(false);
            _questionRepoMock.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);

            await _sut.SoftDeleteAsync(1, 10, UserRole.Examiner);

            question.IsActive.Should().BeFalse();
            _questionRepoMock.Verify(r => r.Update(question), Times.Once);
        }

        [Test]
        public async Task SoftDeleteAsync_QuestionUsedInPublishedExam_ThrowsQuestionInUseException()
        {
            var question = TestDataBuilder.BuildQuestion(1, examinerId: 10);
            _questionRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(question);
            _questionRepoMock.Setup(r => r.IsUsedInPublishedExamAsync(1)).ReturnsAsync(true);

            var act = async () => await _sut.SoftDeleteAsync(1, 10, UserRole.Examiner);
            await act.Should().ThrowAsync<QuestionInUseException>();
            question.IsActive.Should().BeTrue(); // delete was blocked
            _loggerMock.VerifyWarning(Times.AtLeastOnce());
        }

        [Test]
        public async Task SoftDeleteAsync_ExaminerDeletesOtherExaminersQuestion_ThrowsNotFound()
        {
            var question = TestDataBuilder.BuildQuestion(1, examinerId: 10);
            _questionRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(question);

            var act = async () => await _sut.SoftDeleteAsync(1, 99, UserRole.Examiner);
            await act.Should().ThrowAsync<QuestionNotFoundException>();
        }

        // ════════════════════════════════════════════════════════════════
        //   GetAllPagedAsync — pagination + filters
        // ════════════════════════════════════════════════════════════════

        [Test]
        public async Task GetAllPagedAsync_ExaminerFirstPage_ReturnsPagedResult()
        {
            // ─── ARRANGE ──────────────────────────────────────────────
            var questions = Enumerable.Range(1, 5)
                .Select(i => TestDataBuilder.BuildQuestion(i, examinerId: 10))
                .ToList();

            _questionRepoMock
                .Setup(r => r.GetFilteredPagedAsync(10, null, null, null, 1, 20))
                .ReturnsAsync(((IEnumerable<Question>)questions, 5));

            // ─── ACT ──────────────────────────────────────────────────
            var result = await _sut.GetAllPagedAsync(10, UserRole.Examiner, 1, 20);

            // ─── ASSERT ───────────────────────────────────────────────
            result.Items.Should().HaveCount(5);
            result.TotalCount.Should().Be(5);
            result.Page.Should().Be(1);
            result.HasNextPage.Should().BeFalse();   // only 1 page
        }

        [Test]
        public async Task GetAllPagedAsync_PageSizeOver100_ClampsTo100()
        {
            // ─── ARRANGE ──────────────────────────────────────────────
            var items = new List<Question> { TestDataBuilder.BuildQuestion(1, examinerId: 10) };

            // Service must clamp 500 → 100 before calling the repository.
            _questionRepoMock
                .Setup(r => r.GetFilteredPagedAsync(null, null, null, null, 1, 100))
                .ReturnsAsync(((IEnumerable<Question>)items, 1));

            // ─── ACT ──────────────────────────────────────────────────
            var result = await _sut.GetAllPagedAsync(99, UserRole.Admin, 1, 500);

            // ─── ASSERT ───────────────────────────────────────────────
            result.PageSize.Should().Be(100);
            _questionRepoMock.Verify(
                r => r.GetFilteredPagedAsync(null, null, null, null, 1, 100), Times.Once);
        }

        // ════════════════════════════════════════════════════════════════
        //   ImportFromExcelAsync
        // ════════════════════════════════════════════════════════════════

        // Builds an in-memory .xlsx stream for ImportFromExcelAsync tests.
        // Row 1 = header (CategoryId | QuestionText | Marks | Difficulty |
        //                 OptionA | OptionB | OptionC | OptionD | CorrectOption).
        // Row 2+ = data rows supplied by the caller.
        private static Stream BuildExcelStream(
            IEnumerable<(int CatId, string Text, decimal Marks, string Diff,
                          string OptA, string OptB, string OptC, string OptD,
                          string Correct)> rows)
        {
            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Questions");

            // Header row (matches the template layout documented in QuestionService).
            ws.Cell(1, 1).Value = "CategoryId";
            ws.Cell(1, 2).Value = "QuestionText";
            ws.Cell(1, 3).Value = "Marks";
            ws.Cell(1, 4).Value = "Difficulty";
            ws.Cell(1, 5).Value = "OptionA";
            ws.Cell(1, 6).Value = "OptionB";
            ws.Cell(1, 7).Value = "OptionC";
            ws.Cell(1, 8).Value = "OptionD";
            ws.Cell(1, 9).Value = "CorrectOption";

            int r = 2;
            foreach (var row in rows)
            {
                ws.Cell(r, 1).Value = row.CatId;
                ws.Cell(r, 2).Value = row.Text;
                ws.Cell(r, 3).Value = row.Marks;
                ws.Cell(r, 4).Value = row.Diff;
                ws.Cell(r, 5).Value = row.OptA;
                ws.Cell(r, 6).Value = row.OptB;
                ws.Cell(r, 7).Value = row.OptC;
                ws.Cell(r, 8).Value = row.OptD;
                ws.Cell(r, 9).Value = row.Correct;
                r++;
            }

            // Save to a MemoryStream and reset the position to 0 so the
            // caller can read from the beginning.
            var ms = new MemoryStream();
            wb.SaveAs(ms);
            ms.Position = 0;
            return ms;
        }

        [Test]
        public async Task ImportFromExcelAsync_AllValidRows_CreatesAllQuestions()
        {
            // ─── ARRANGE ──────────────────────────────────────────────
            // 3 distinct questions — unique texts to avoid the duplicate check firing.
            var rows = new[]
            {
                (1, "What is DI?",   10m, "Medium", "A1", "B1", "C1", "D1", "A"),
                (1, "What is OOP?",  10m, "Easy",   "A2", "B2", "C2", "D2", "B"),
                (1, "What is LINQ?", 10m, "Hard",   "A3", "B3", "C3", "D3", "C"),
            };

            // Return a pre-built question entity from GetByIdWithOptionsAsync so
            // CreateAsync's reload-after-save doesn't return null.
            _questionRepoMock
                .Setup(r => r.GetByIdWithOptionsAsync(It.IsAny<int>()))
                .ReturnsAsync(TestDataBuilder.BuildQuestion(1, examinerId: 10));

            using var stream = BuildExcelStream(rows);

            // ─── ACT ──────────────────────────────────────────────────
            var result = await _sut.ImportFromExcelAsync(examinerId: 10, excelStream: stream);

            // ─── ASSERT ───────────────────────────────────────────────
            result.SuccessCount.Should().Be(3);
            result.FailureCount.Should().Be(0);
            result.Errors.Should().BeEmpty();
            // Each row triggered exactly one CreateAsync → one AddAsync call per question.
            _questionRepoMock.Verify(r => r.AddAsync(It.IsAny<Question>()), Times.Exactly(3));
        }

        [Test]
        public async Task ImportFromExcelAsync_MixedRows_CollectsErrorsAndContinues()
        {
            // ─── ARRANGE ──────────────────────────────────────────────
            // Row 2: valid.
            // Row 3: invalid marks (0) — pre-validation catch, AddAsync never called.
            // Row 4: valid.
            var rows = new[]
            {
                (1, "Valid Q1",   10m, "Medium", "A", "B", "C", "D", "A"),  // valid
                (1, "Bad Marks Q", 0m, "Easy",   "A", "B", "C", "D", "A"),  // invalid — marks = 0
                (1, "Valid Q2",   15m, "Hard",   "A", "B", "C", "D", "B"),  // valid
            };

            _questionRepoMock
                .Setup(r => r.GetByIdWithOptionsAsync(It.IsAny<int>()))
                .ReturnsAsync(TestDataBuilder.BuildQuestion(1, examinerId: 10));

            using var stream = BuildExcelStream(rows);

            // ─── ACT ──────────────────────────────────────────────────
            var result = await _sut.ImportFromExcelAsync(examinerId: 10, excelStream: stream);

            // ─── ASSERT ───────────────────────────────────────────────
            result.SuccessCount.Should().Be(2);
            result.FailureCount.Should().Be(1);
            result.Errors.Should().HaveCount(1);
            result.Errors[0].Should().Contain("Row 3"); // the bad-marks row
        }

        [Test]
        public async Task ImportFromExcelAsync_EmptyWorkbook_ReturnsZeroSuccesses()
        {
            // ─── ARRANGE ──────────────────────────────────────────────
            // A workbook with a sheet but NO data rows (only the header at row 1).
            // LastRowUsed returns row 1, so the for-loop body never executes.
            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Questions");
            ws.Cell(1, 1).Value = "CategoryId"; // just the header

            var ms = new MemoryStream();
            wb.SaveAs(ms);
            ms.Position = 0;

            // ─── ACT ──────────────────────────────────────────────────
            var result = await _sut.ImportFromExcelAsync(examinerId: 10, excelStream: ms);

            // ─── ASSERT ───────────────────────────────────────────────
            result.SuccessCount.Should().Be(0);
            result.FailureCount.Should().Be(0);
            result.Errors.Should().BeEmpty();
            _questionRepoMock.Verify(r => r.AddAsync(It.IsAny<Question>()), Times.Never);
        }

        [Test]
        public async Task ImportFromExcelAsync_DuplicateRow_RecordsErrorAndContinues()
        {
            // ─── ARRANGE ──────────────────────────────────────────────
            // Row 2: duplicate (IsDuplicateAsync returns true → DuplicateQuestionException).
            // Row 3: valid.
            var rows = new[]
            {
                (1, "Duplicate Q", 10m, "Easy",   "A", "B", "C", "D", "A"),  // will be duplicate
                (1, "Unique Q",    10m, "Medium",  "A", "B", "C", "D", "B"),  // valid
            };

            // Override the default "false" setup so the first question text triggers a duplicate.
            _questionRepoMock
                .Setup(r => r.IsDuplicateAsync(10, 1, It.Is<string>(t => t.Contains("Duplicate"))))
                .ReturnsAsync(true);

            _questionRepoMock
                .Setup(r => r.GetByIdWithOptionsAsync(It.IsAny<int>()))
                .ReturnsAsync(TestDataBuilder.BuildQuestion(1, examinerId: 10));

            using var stream = BuildExcelStream(rows);

            // ─── ACT ──────────────────────────────────────────────────
            var result = await _sut.ImportFromExcelAsync(examinerId: 10, excelStream: stream);

            // ─── ASSERT ───────────────────────────────────────────────
            result.SuccessCount.Should().Be(1);
            result.FailureCount.Should().Be(1);
            result.Errors[0].Should().Contain("Row 2"); // the duplicate row
        }
    }
}
