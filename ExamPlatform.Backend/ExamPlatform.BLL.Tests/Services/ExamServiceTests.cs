// ExamServiceTests.cs — covers Create, Update, Delete, Publish, Unpublish, PublishResults guards.

using ExamPlatform.BLL.Exceptions;
using ExamPlatform.BLL.Interfaces;
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
    public class ExamServiceTests
    {
        private Mock<IExamRepository>           _examRepoMock     = null!;
        private Mock<IExamQuestionRepository>   _examQRepoMock    = null!;
        private Mock<IQuestionRepository>       _questionRepoMock = null!;
        private Mock<IAttemptRepository>        _attemptRepoMock  = null!;
        private Mock<ICertificateRepository>    _certRepoMock     = null!;
        private Mock<INotificationRepository>   _notifRepoMock    = null!;
        private Mock<INotificationEmailService> _emailMock        = null!;
        private Mock<ILogger<ExamService>>      _loggerMock       = null!;
        private ExamPlatformContext _dbContext = null!;
        private ExamService _sut = null!;

        [SetUp]
        public void SetUp()
        {
            _examRepoMock     = new Mock<IExamRepository>();
            _examQRepoMock    = new Mock<IExamQuestionRepository>();
            _questionRepoMock = new Mock<IQuestionRepository>();
            _attemptRepoMock  = new Mock<IAttemptRepository>();
            _certRepoMock     = new Mock<ICertificateRepository>();
            _notifRepoMock    = new Mock<INotificationRepository>();
            _emailMock        = new Mock<INotificationEmailService>();
            _loggerMock       = new Mock<ILogger<ExamService>>();

            var options = new DbContextOptionsBuilder<ExamPlatformContext>()
                .UseInMemoryDatabase($"ExamSvcDb_{Guid.NewGuid()}")
                .Options;
            _dbContext = new ExamPlatformContext(options);

            _sut = new ExamService(
                _examRepoMock.Object, _examQRepoMock.Object, _questionRepoMock.Object,
                _attemptRepoMock.Object, _certRepoMock.Object, _notifRepoMock.Object,
                _emailMock.Object, _dbContext, _loggerMock.Object);
        }

        [TearDown]
        public void TearDown() => _dbContext.Dispose();

        // ════════════════════════════════════════════════════════════════
        //   CreateAsync — HAPPY PATH
        // ════════════════════════════════════════════════════════════════

        [Test]
        public async Task CreateAsync_WithValidInput_CreatesExamInDraftStatus()
        {
            _examRepoMock.Setup(r => r.AddAsync(It.IsAny<Exam>()))
                         .ReturnsAsync((Exam e) => e);
            _examRepoMock.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);

            var result = await _sut.CreateAsync(10, 1, "Test Exam", null, 60, 50m, 30m,
                DateTime.UtcNow.AddHours(1), DateTime.UtcNow.AddHours(3));

            result.ExamStatus.Should().Be(ExamStatus.Draft);
            result.Title.Should().Be("Test Exam");
            _examRepoMock.Verify(r => r.AddAsync(It.IsAny<Exam>()), Times.Once);
        }

        // ════════════════════════════════════════════════════════════════
        //   CreateAsync — VALIDATION FAILURES
        // ════════════════════════════════════════════════════════════════

        [Test]
        public async Task CreateAsync_EmptyTitle_ThrowsValidationException()
        {
            var act = async () => await _sut.CreateAsync(10, 1, "", null, 60, 50m, 30m,
                DateTime.UtcNow.AddHours(1), DateTime.UtcNow.AddHours(3));
            await act.Should().ThrowAsync<ValidationException>();
        }

        [Test]
        public async Task CreateAsync_StartTimeAfterEndTime_ThrowsValidationException()
        {
            var act = async () => await _sut.CreateAsync(10, 1, "Title", null, 60, 50m, 30m,
                DateTime.UtcNow.AddHours(3), DateTime.UtcNow.AddHours(1));
            await act.Should().ThrowAsync<ValidationException>();
        }

        [Test]
        public async Task CreateAsync_PassingMarksGreaterThanTotalMarks_ThrowsValidationException()
        {
            var act = async () => await _sut.CreateAsync(10, 1, "Title", null, 60,
                totalMarks: 40m, passingMarks: 50m,
                DateTime.UtcNow.AddHours(1), DateTime.UtcNow.AddHours(3));
            await act.Should().ThrowAsync<ValidationException>();
        }

        // ════════════════════════════════════════════════════════════════
        //   UpdateAsync — OWNERSHIP + STATUS GUARD
        // ════════════════════════════════════════════════════════════════

        [Test]
        public async Task UpdateAsync_WrongExaminer_ThrowsExamNotFoundException()
        {
            var exam = TestDataBuilder.BuildDraftExam(1, examinerId: 10);
            _examRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(exam);

            var act = async () => await _sut.UpdateAsync(1, 99, "Title", null, 60, 50m, 30m,
                DateTime.UtcNow.AddHours(1), DateTime.UtcNow.AddHours(3));
            await act.Should().ThrowAsync<ExamNotFoundException>();
        }

        [Test]
        public async Task UpdateAsync_ExamNotInDraft_ThrowsExamNotInDraftException()
        {
            var exam = TestDataBuilder.BuildPublishedExam(1, examinerId: 10);
            _examRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(exam);

            var act = async () => await _sut.UpdateAsync(1, 10, "Title", null, 60, 50m, 30m,
                DateTime.UtcNow.AddHours(1), DateTime.UtcNow.AddHours(3));
            await act.Should().ThrowAsync<ExamNotInDraftException>();
        }

        // ════════════════════════════════════════════════════════════════
        //   DeleteAsync
        // ════════════════════════════════════════════════════════════════

        [Test]
        public async Task DeleteAsync_OwnedDraftExam_Deletes()
        {
            var exam = TestDataBuilder.BuildDraftExam(1, examinerId: 10);
            _examRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(exam);
            _examRepoMock.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);

            await _sut.DeleteAsync(1, 10);

            _examRepoMock.Verify(r => r.Delete(It.IsAny<Exam>()), Times.Once);
        }

        // ════════════════════════════════════════════════════════════════
        //   PublishAsync — PRE-CONDITIONS
        // ════════════════════════════════════════════════════════════════

        [Test]
        public async Task PublishAsync_NoQuestionsAttached_ThrowsExamNotPublishableException()
        {
            var exam = TestDataBuilder.BuildDraftExam(1, 10);
            exam.ExamQuestions = new List<ExamQuestion>();
            _examRepoMock.Setup(r => r.GetByIdWithQuestionsAsync(1)).ReturnsAsync(exam);

            var act = async () => await _sut.PublishAsync(1, 10);
            await act.Should().ThrowAsync<ExamNotPublishableException>()
                     .WithMessage("*no questions*");
        }

        [Test]
        public async Task PublishAsync_TotalMarksMismatch_ThrowsExamNotPublishableException()
        {
            var exam = TestDataBuilder.BuildDraftExam(1, 10, totalMarks: 50m);
            var q = TestDataBuilder.BuildQuestion(1, marks: 30m);
            exam.ExamQuestions = new List<ExamQuestion>
            {
                new() { ExamId = 1, QuestionId = 1, Question = q, QuestionOrder = 1 }
            };
            _examRepoMock.Setup(r => r.GetByIdWithQuestionsAsync(1)).ReturnsAsync(exam);

            var act = async () => await _sut.PublishAsync(1, 10);
            await act.Should().ThrowAsync<ExamNotPublishableException>()
                     .WithMessage("*TotalMarks*");
        }

        [Test]
        public async Task PublishAsync_AlreadyPublished_ThrowsExamAlreadyPublishedException()
        {
            var exam = TestDataBuilder.BuildPublishedExam(1, 10);
            _examRepoMock.Setup(r => r.GetByIdWithQuestionsAsync(1)).ReturnsAsync(exam);

            var act = async () => await _sut.PublishAsync(1, 10);
            await act.Should().ThrowAsync<ExamAlreadyPublishedException>();
        }

        // ════════════════════════════════════════════════════════════════
        //   UnpublishAsync — GUARD CLAUSES
        // ════════════════════════════════════════════════════════════════

        [Test]
        public async Task UnpublishAsync_ResultsAlreadyPublished_ThrowsExamAlreadyPublishedException()
        {
            var exam = TestDataBuilder.BuildPublishedExam(1, 10);
            exam.ExamStatus = ExamStatus.ResultsPublished;
            _examRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(exam);

            var act = async () => await _sut.UnpublishAsync(1, 10, false);
            await act.Should().ThrowAsync<ExamAlreadyPublishedException>();
        }

        [Test]
        public async Task UnpublishAsync_ExamIsDraft_ThrowsExamNotInDraftException()
        {
            var exam = TestDataBuilder.BuildDraftExam(1, 10);
            _examRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(exam);

            var act = async () => await _sut.UnpublishAsync(1, 10, false);
            await act.Should().ThrowAsync<ExamNotInDraftException>();
        }

        [Test]
        public async Task UnpublishAsync_ExamHasAttempts_ThrowsExamHasAttemptsException()
        {
            var exam = TestDataBuilder.BuildPublishedExam(1, 10);
            _examRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(exam);
            _attemptRepoMock.Setup(r => r.HasAnyForExamAsync(1)).ReturnsAsync(true);

            var act = async () => await _sut.UnpublishAsync(1, 10, false);
            await act.Should().ThrowAsync<ExamHasAttemptsException>();
        }

        [Test]
        public async Task UnpublishAsync_ValidPublishedExamNoAttempts_RevertsToDraft()
        {
            var exam = TestDataBuilder.BuildPublishedExam(1, 10);
            _examRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(exam);
            _attemptRepoMock.Setup(r => r.HasAnyForExamAsync(1)).ReturnsAsync(false);
            _examRepoMock.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);

            var result = await _sut.UnpublishAsync(1, 10, false);

            result.ExamStatus.Should().Be(ExamStatus.Draft);
            _examRepoMock.Verify(r => r.Update(It.IsAny<Exam>()), Times.Once);
        }

        // ════════════════════════════════════════════════════════════════
        //   AttachQuestionsAsync — CROSS-EXAMINER GUARD
        // ════════════════════════════════════════════════════════════════

        [Test]
        public async Task AttachQuestionsAsync_CrossExaminerQuestion_ThrowsValidationException()
        {
            var exam = TestDataBuilder.BuildDraftExam(1, examinerId: 10);
            _examRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(exam);

            // Question belongs to examiner 20, not 10.
            var q = TestDataBuilder.BuildQuestion(1, examinerId: 20);
            _questionRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(q);
            _examQRepoMock.Setup(r => r.GetByExamIdAsync(1))
                          .ReturnsAsync(new List<ExamQuestion>());

            var act = async () => await _sut.AttachQuestionsAsync(1, new[] { 1 }, 10);
            await act.Should().ThrowAsync<ValidationException>()
                     .WithMessage("*not created by you*");
        }

        // ════════════════════════════════════════════════════════════════
        //   PublishResultsAsync — GUARD CLAUSES
        // ════════════════════════════════════════════════════════════════

        [Test]
        public async Task PublishResultsAsync_ExamNotFound_ThrowsExamNotFoundException()
        {
            _examRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<int>()))
                         .ReturnsAsync((Exam?)null);

            var act = async () => await _sut.PublishResultsAsync(999, 10);
            await act.Should().ThrowAsync<ExamNotFoundException>();
        }

        [Test]
        public async Task PublishResultsAsync_ExamNotPublished_ThrowsExamAlreadyPublishedException()
        {
            // Draft exam — only Published exams can have results published.
            var exam = TestDataBuilder.BuildDraftExam(1, 10);
            _examRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(exam);

            var act = async () => await _sut.PublishResultsAsync(1, 10);
            await act.Should().ThrowAsync<ExamAlreadyPublishedException>();
        }
    }
}
