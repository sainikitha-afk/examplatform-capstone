// ExamCategoryServiceTests.cs — covers CreateAsync, BulkCreateAsync, UpdateAsync, DeleteAsync.

using ExamPlatform.BLL.Exceptions;
using ExamPlatform.BLL.Services;
using ExamPlatform.BLL.Tests.Helpers;
using ExamPlatform.DAL.Interfaces;
using ExamPlatform.Domain.Entities;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace ExamPlatform.BLL.Tests.Services
{
    [TestFixture]
    public class ExamCategoryServiceTests
    {
        private Mock<IExamCategoryRepository> _categoryRepoMock = null!;
        private Mock<ILogger<ExamCategoryService>> _loggerMock = null!;
        private ExamCategoryService _sut = null!;

        [SetUp]
        public void SetUp()
        {
            _categoryRepoMock = new Mock<IExamCategoryRepository>();
            _loggerMock = new Mock<ILogger<ExamCategoryService>>();

            _categoryRepoMock.Setup(r => r.GetAllAsync())
                             .ReturnsAsync(new List<ExamCategory>());
            _categoryRepoMock.Setup(r => r.AddAsync(It.IsAny<ExamCategory>()))
                             .ReturnsAsync((ExamCategory c) => c);
            _categoryRepoMock.Setup(r => r.SaveChangesAsync())
                             .ReturnsAsync(1);

            _sut = new ExamCategoryService(_categoryRepoMock.Object, _loggerMock.Object);
        }

        // ════════════════════════════════════════════════════════════════
        //   CreateAsync — HAPPY PATH
        // ════════════════════════════════════════════════════════════════

        [Test]
        public async Task CreateAsync_WithValidName_CreatesAndReturnsCategory()
        {
            var result = await _sut.CreateAsync("Mathematics", "Test description");

            result.CatName.Should().Be("Mathematics");
            _categoryRepoMock.Verify(r => r.AddAsync(It.IsAny<ExamCategory>()), Times.Once);
            _categoryRepoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Test]
        public async Task CreateAsync_WhitespaceInName_IsTrimmed()
        {
            var result = await _sut.CreateAsync("  Physics  ", null);
            result.CatName.Should().Be("Physics");
        }

        // ════════════════════════════════════════════════════════════════
        //   CreateAsync — VALIDATION FAILURES
        // ════════════════════════════════════════════════════════════════

        [TestCase("", TestName = "EmptyName")]
        [TestCase("   ", TestName = "WhitespaceName")]
        public async Task CreateAsync_WithEmptyOrWhitespaceName_ThrowsValidationException(string name)
        {
            var act = async () => await _sut.CreateAsync(name, null);
            await act.Should().ThrowAsync<ValidationException>();
            _categoryRepoMock.Verify(r => r.AddAsync(It.IsAny<ExamCategory>()), Times.Never);
        }

        [Test]
        public async Task CreateAsync_WithDuplicateName_ThrowsDuplicateCategoryException()
        {
            _categoryRepoMock.Setup(r => r.GetAllAsync())
                             .ReturnsAsync(new List<ExamCategory>
                             {
                                 TestDataBuilder.BuildCategory(1, "Mathematics")
                             });

            var act = async () => await _sut.CreateAsync("mathematics", null);
            await act.Should().ThrowAsync<DuplicateCategoryException>();
        }

        // ════════════════════════════════════════════════════════════════
        //   BulkCreateAsync — HAPPY PATH
        // ════════════════════════════════════════════════════════════════

        [Test]
        public async Task BulkCreateAsync_WithValidBatch_CreatesAllCategories()
        {
            var batch = new List<(string, string?)>
            {
                ("Physics", "Wave mechanics"),
                ("Chemistry", null),
                ("Biology", "Life sciences")
            };

            var result = await _sut.BulkCreateAsync(batch);

            result.Should().HaveCount(3);
            _categoryRepoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
            _categoryRepoMock.Verify(r => r.AddAsync(It.IsAny<ExamCategory>()), Times.Exactly(3));
        }

        // ════════════════════════════════════════════════════════════════
        //   BulkCreateAsync — VALIDATION FAILURES
        // ════════════════════════════════════════════════════════════════

        [Test]
        public async Task BulkCreateAsync_EmptyList_ThrowsValidationException()
        {
            var act = async () => await _sut.BulkCreateAsync(new List<(string, string?)>());
            await act.Should().ThrowAsync<ValidationException>();
        }

        [Test]
        public async Task BulkCreateAsync_IntraBatchDuplicate_ThrowsValidationException()
        {
            var batch = new List<(string, string?)>
            {
                ("Physics", null),
                ("physics", null) // same name, different case — intra-batch duplicate
            };

            var act = async () => await _sut.BulkCreateAsync(batch);
            await act.Should().ThrowAsync<ValidationException>()
                     .WithMessage("*more than once*");

            _categoryRepoMock.Verify(r => r.AddAsync(It.IsAny<ExamCategory>()), Times.Never);
        }

        [Test]
        public async Task BulkCreateAsync_NameAlreadyInDB_ThrowsDuplicateCategoryException()
        {
            _categoryRepoMock.Setup(r => r.GetAllAsync())
                             .ReturnsAsync(new List<ExamCategory>
                             {
                                 TestDataBuilder.BuildCategory(1, "Chemistry")
                             });

            var batch = new List<(string, string?)>
            {
                ("Physics", null),
                ("Chemistry", null) // already in DB
            };

            var act = async () => await _sut.BulkCreateAsync(batch);
            await act.Should().ThrowAsync<DuplicateCategoryException>();
        }

        // ════════════════════════════════════════════════════════════════
        //   UpdateAsync
        // ════════════════════════════════════════════════════════════════

        [Test]
        public async Task UpdateAsync_WithValidName_UpdatesCategory()
        {
            var existing = TestDataBuilder.BuildCategory(1, "OldName");
            _categoryRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(existing);
            _categoryRepoMock.Setup(r => r.GetAllAsync())
                             .ReturnsAsync(new List<ExamCategory> { existing });

            var result = await _sut.UpdateAsync(1, "NewName", "Updated desc");

            result.CatName.Should().Be("NewName");
            _categoryRepoMock.Verify(r => r.Update(It.IsAny<ExamCategory>()), Times.Once);
            _categoryRepoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Test]
        public async Task UpdateAsync_NonExistentCategory_ThrowsCategoryNotFoundException()
        {
            _categoryRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<int>()))
                             .ReturnsAsync((ExamCategory?)null);

            var act = async () => await _sut.UpdateAsync(999, "NewName", null);
            await act.Should().ThrowAsync<CategoryNotFoundException>();
        }

        // ════════════════════════════════════════════════════════════════
        //   DeleteAsync
        // ════════════════════════════════════════════════════════════════

        [Test]
        public async Task DeleteAsync_ExistingCategory_DeletesCategory()
        {
            var existing = TestDataBuilder.BuildCategory(1);
            _categoryRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(existing);
            _categoryRepoMock.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);

            await _sut.DeleteAsync(1);

            _categoryRepoMock.Verify(r => r.Delete(It.IsAny<ExamCategory>()), Times.Once);
            _categoryRepoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Test]
        public async Task DeleteAsync_NonExistentCategory_ThrowsCategoryNotFoundException()
        {
            _categoryRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<int>()))
                             .ReturnsAsync((ExamCategory?)null);

            var act = async () => await _sut.DeleteAsync(999);
            await act.Should().ThrowAsync<CategoryNotFoundException>();
        }

        // ════════════════════════════════════════════════════════════════
        //   GetAllAsync
        // ════════════════════════════════════════════════════════════════

        [Test]
        public async Task GetAllAsync_ReturnsAllCategories()
        {
            _categoryRepoMock.Setup(r => r.GetAllAsync())
                             .ReturnsAsync(new List<ExamCategory>
                             {
                                 TestDataBuilder.BuildCategory(1, "C#"),
                                 TestDataBuilder.BuildCategory(2, "SQL"),
                             });

            var result = await _sut.GetAllAsync();

            result.Should().HaveCount(2);
        }

        // ════════════════════════════════════════════════════════════════
        //   BulkCreateAsync — name length rule
        // ════════════════════════════════════════════════════════════════

        [Test]
        public async Task BulkCreateAsync_EmptyNameInBatch_ThrowsValidationException()
        {
            var act = async () => await _sut.BulkCreateAsync(
                new List<(string, string?)> { ("  ", null) });
            await act.Should().ThrowAsync<ValidationException>()
                     .WithMessage("*name is required*");
        }

        [Test]
        public async Task BulkCreateAsync_NameTooShort_ThrowsValidationException()
        {
            var act = async () => await _sut.BulkCreateAsync(
                new List<(string, string?)> { ("A", null) }); // 1 char < minimum 2
            await act.Should().ThrowAsync<ValidationException>()
                     .WithMessage("*between 2 and 100*");
        }

        // ════════════════════════════════════════════════════════════════
        //   UpdateAsync — remaining branches
        // ════════════════════════════════════════════════════════════════

        [Test]
        public async Task UpdateAsync_EmptyName_ThrowsValidationException()
        {
            var act = async () => await _sut.UpdateAsync(1, "  ", null);
            await act.Should().ThrowAsync<ValidationException>();
        }

        [Test]
        public async Task UpdateAsync_NewNameTakenByAnotherCategory_ThrowsDuplicateCategoryException()
        {
            var target = TestDataBuilder.BuildCategory(1, "C#");
            var other  = TestDataBuilder.BuildCategory(2, "SQL");
            _categoryRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(target);
            _categoryRepoMock.Setup(r => r.GetAllAsync())
                             .ReturnsAsync(new List<ExamCategory> { target, other });

            // Renaming category 1 to "sql" collides with category 2 (case-insensitive).
            var act = async () => await _sut.UpdateAsync(1, "sql", null);
            await act.Should().ThrowAsync<DuplicateCategoryException>();
        }

        [Test]
        public async Task UpdateAsync_SameNameDifferentCase_SkipsDuplicateCheckAndUpdates()
        {
            var target = TestDataBuilder.BuildCategory(1, "C#");
            _categoryRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(target);
            _categoryRepoMock.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);

            // "c#" equals its own current name case-insensitively — allowed.
            var result = await _sut.UpdateAsync(1, "c#", "new description");

            result.Description.Should().Be("new description");
            _categoryRepoMock.Verify(r => r.GetAllAsync(), Times.Never); // dup check skipped
        }
    }
}
