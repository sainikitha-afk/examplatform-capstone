// ExamServiceTests.cs — covers Create, Update, Delete, Publish, Unpublish, PublishResults guards.

using ExamPlatform.BLL.Exceptions;
using ExamPlatform.BLL.Interfaces;
using ExamPlatform.BLL.Models;
using ExamPlatform.BLL.Services;
using ExamPlatform.BLL.Tests.Helpers;
using ExamPlatform.DAL.Contexts;
using ExamPlatform.DAL.Interfaces;
using ExamPlatform.Domain.Entities;
using ExamPlatform.Domain.Enums;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
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
        private Mock<IUserRepository>           _userRepoMock     = null!;
        private Mock<IExaminerSubscriptionService> _subscriptionServiceMock = null!;
        private Mock<IInstitutionRepository>    _institutionRepoMock = null!;
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
            _userRepoMock     = new Mock<IUserRepository>();
            _subscriptionServiceMock = new Mock<IExaminerSubscriptionService>();
            _loggerMock       = new Mock<ILogger<ExamService>>();

            // PublishAsync announces to candidates — default to an empty list so
            // existing publish tests don't need to care about notifications.
            _userRepoMock.Setup(r => r.GetAllByRoleAsync(UserRole.Candidate))
                         .ReturnsAsync(new List<User>());

            // Default: subscription gate always allows exam creation and never
            // blocks premium — individual CreateAsync tests override this when
            // they specifically exercise the subscription/premium guard clauses.
            _subscriptionServiceMock
                .Setup(s => s.CanCreateExamAsync(It.IsAny<int>(), It.IsAny<DateTime?>()))
                .ReturnsAsync(new ExamCreationEligibility(
                    true, ExaminerSubscriptionStatus.ProfessionalActive, 0, null, 0, null, true));
            _subscriptionServiceMock
                .Setup(s => s.RecordExamCreatedAsync(It.IsAny<int>(), It.IsAny<DateTime?>()))
                .Returns(Task.CompletedTask);

            _institutionRepoMock = new Mock<IInstitutionRepository>();
            _institutionRepoMock
                .Setup(r => r.GetByOwnerUserIdAsync(It.IsAny<int>()))
                .ReturnsAsync((Institution?)null);

            var options = new DbContextOptionsBuilder<ExamPlatformContext>()
                .UseInMemoryDatabase($"ExamSvcDb_{Guid.NewGuid()}")
                // PublishResultsAsync opens an explicit transaction; the InMemory provider
                // doesn't support transactions, so ignore the warning (no-op transaction).
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;
            _dbContext = new ExamPlatformContext(options);

            _sut = new ExamService(
                _examRepoMock.Object, _examQRepoMock.Object, _questionRepoMock.Object,
                _attemptRepoMock.Object, _certRepoMock.Object, _notifRepoMock.Object,
                _emailMock.Object, _userRepoMock.Object, _dbContext,
                _subscriptionServiceMock.Object, _institutionRepoMock.Object, _loggerMock.Object);
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
        public async Task CreateAsync_InvalidDuration_ThrowsValidationException()
        {
            var act = async () => await _sut.CreateAsync(10, 1, "Title", null, 0, 50m, 30m,
                DateTime.UtcNow.AddHours(1), DateTime.UtcNow.AddHours(3));
            await act.Should().ThrowAsync<ValidationException>();
        }

        [Test]
        public async Task CreateAsync_InvalidTotalMarks_ThrowsValidationException()
        {
            var act = async () => await _sut.CreateAsync(10, 1, "Title", null, 60, 0m, 30m,
                DateTime.UtcNow.AddHours(1), DateTime.UtcNow.AddHours(3));
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
        //   CreateAsync — SUBSCRIPTION GATE (Part 2/3/9)
        // ════════════════════════════════════════════════════════════════

        [Test]
        public async Task CreateAsync_TrialExhausted_ThrowsTrialExhaustedException()
        {
            _subscriptionServiceMock
                .Setup(s => s.CanCreateExamAsync(10, It.IsAny<DateTime?>()))
                .ReturnsAsync(new ExamCreationEligibility(
                    false, ExaminerSubscriptionStatus.TrialExhausted, 8, 8, 0, null, false));

            var act = async () => await _sut.CreateAsync(10, 1, "Title", null, 60, 50m, 30m,
                DateTime.UtcNow.AddHours(1), DateTime.UtcNow.AddHours(3));

            await act.Should().ThrowAsync<TrialExhaustedException>();
            _examRepoMock.Verify(r => r.AddAsync(It.IsAny<Exam>()), Times.Never);
        }

        [Test]
        public async Task CreateAsync_FreeForeverCooldownActive_ThrowsCooldownActiveException()
        {
            var nextEligible = DateTime.UtcNow.AddDays(1);
            _subscriptionServiceMock
                .Setup(s => s.CanCreateExamAsync(10, It.IsAny<DateTime?>()))
                .ReturnsAsync(new ExamCreationEligibility(
                    false, ExaminerSubscriptionStatus.FreeForever, 0, null, 2, nextEligible, false));

            var act = async () => await _sut.CreateAsync(10, 1, "Title", null, 60, 50m, 30m,
                DateTime.UtcNow.AddHours(1), DateTime.UtcNow.AddHours(3));

            await act.Should().ThrowAsync<CooldownActiveException>();
        }

        [Test]
        public async Task CreateAsync_PremiumRequestedButNotAllowed_ThrowsValidationException()
        {
            _subscriptionServiceMock
                .Setup(s => s.CanCreateExamAsync(10, It.IsAny<DateTime?>()))
                .ReturnsAsync(new ExamCreationEligibility(
                    true, ExaminerSubscriptionStatus.FreeForever, 0, null, 2, null, false));

            var act = async () => await _sut.CreateAsync(10, 1, "Title", null, 60, 50m, 30m,
                DateTime.UtcNow.AddHours(1), DateTime.UtcNow.AddHours(3),
                isPremium: true, price: 999m);

            await act.Should().ThrowAsync<ValidationException>();
        }

        [Test]
        public async Task CreateAsync_PremiumWithoutPrice_ThrowsValidationException()
        {
            var act = async () => await _sut.CreateAsync(10, 1, "Title", null, 60, 50m, 30m,
                DateTime.UtcNow.AddHours(1), DateTime.UtcNow.AddHours(3),
                isPremium: true, price: null);

            await act.Should().ThrowAsync<ValidationException>();
        }

        [Test]
        public async Task CreateAsync_PremiumAllowedWithPrice_CreatesPremiumExam()
        {
            _examRepoMock.Setup(r => r.AddAsync(It.IsAny<Exam>())).ReturnsAsync((Exam e) => e);
            _examRepoMock.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);

            var result = await _sut.CreateAsync(10, 1, "Title", null, 60, 50m, 30m,
                DateTime.UtcNow.AddHours(1), DateTime.UtcNow.AddHours(3),
                isPremium: true, price: 999m);

            result.IsPremium.Should().BeTrue();
            result.Price.Should().Be(999m);
        }

        [Test]
        public async Task CreateAsync_PremiumExam_PinsMaxAttemptsToOne()
        {
            // A premium certification is always one attempt — a client-sent
            // maxAttempts is silently ignored, never an error.
            _examRepoMock.Setup(r => r.AddAsync(It.IsAny<Exam>())).ReturnsAsync((Exam e) => e);
            _examRepoMock.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);

            var result = await _sut.CreateAsync(10, 1, "Title", null, 60, 50m, 30m,
                DateTime.UtcNow.AddHours(1), DateTime.UtcNow.AddHours(3),
                isPremium: true, price: 999m, maxAttempts: 7);

            result.MaxAttempts.Should().Be(1);
        }

        [Test]
        public async Task CreateAsync_NonPremiumWithMaxAttempts_StoresTheLimit()
        {
            _examRepoMock.Setup(r => r.AddAsync(It.IsAny<Exam>())).ReturnsAsync((Exam e) => e);
            _examRepoMock.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);

            var result = await _sut.CreateAsync(10, 1, "Title", null, 60, 50m, 30m,
                DateTime.UtcNow.AddHours(1), DateTime.UtcNow.AddHours(3),
                maxAttempts: 3);

            result.MaxAttempts.Should().Be(3);
        }

        [Test]
        public async Task CreateAsync_MaxAttemptsOutOfRange_ThrowsValidationException()
        {
            var act = async () => await _sut.CreateAsync(10, 1, "Title", null, 60, 50m, 30m,
                DateTime.UtcNow.AddHours(1), DateTime.UtcNow.AddHours(3),
                maxAttempts: 11);

            await act.Should().ThrowAsync<ValidationException>()
                     .WithMessage("*between 1 and 10*");
        }

        [Test]
        public async Task CreateAsync_Success_RecordsExamCreatedOnSubscriptionService()
        {
            _examRepoMock.Setup(r => r.AddAsync(It.IsAny<Exam>())).ReturnsAsync((Exam e) => e);
            _examRepoMock.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);

            await _sut.CreateAsync(10, 1, "Title", null, 60, 50m, 30m,
                DateTime.UtcNow.AddHours(1), DateTime.UtcNow.AddHours(3));

            _subscriptionServiceMock.Verify(
                s => s.RecordExamCreatedAsync(10, It.IsAny<DateTime?>()), Times.Once);
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

        // ════════════════════════════════════════════════════════════════
        //   READ METHODS (delegation + paging clamps)
        // ════════════════════════════════════════════════════════════════

        [Test]
        public async Task GetPublishedAsync_DelegatesToRepository()
        {
            var exams = new List<Exam> { TestDataBuilder.BuildPublishedExam(1, 10) };
            _examRepoMock.Setup(r => r.GetPublishedAsync(null)).ReturnsAsync(exams);

            var result = await _sut.GetPublishedAsync(null);

            result.Should().HaveCount(1);
        }

        [Test]
        public async Task GetPublishedPagedAsync_ClampsPageAndPageSize()
        {
            // page 0 → 1, pageSize 500 → 100.
            _examRepoMock.Setup(r => r.GetPublishedPagedAsync(null, 1, 100))
                         .ReturnsAsync((new List<Exam>(), 0));

            var result = await _sut.GetPublishedPagedAsync(null, page: 0, pageSize: 500);

            result.Page.Should().Be(1);
            result.PageSize.Should().Be(100);
            _examRepoMock.Verify(r => r.GetPublishedPagedAsync(null, 1, 100), Times.Once);
        }

        [Test]
        public async Task GetAvailableForCandidatePagedAsync_QueriesWithTheCandidatesUserId()
        {
            // The candidate browse list is per-user: exams already completed by THIS
            // candidate are excluded in the repository query, so the userId must flow
            // through — a hardcoded or missing id would show completed exams again
            // (or hide someone else's).
            _examRepoMock.Setup(r => r.GetAvailableForCandidatePagedAsync(6, 3, 1, 20))
                         .ReturnsAsync((new List<Exam> { TestDataBuilder.BuildPublishedExam(1) }, 1));

            var result = await _sut.GetAvailableForCandidatePagedAsync(
                userId: 6, categoryId: 3, page: 1, pageSize: 20);

            result.TotalCount.Should().Be(1);
            _examRepoMock.Verify(r => r.GetAvailableForCandidatePagedAsync(6, 3, 1, 20), Times.Once);
        }

        [Test]
        public async Task GetAvailableForCandidatePagedAsync_ClampsPageAndPageSize()
        {
            _examRepoMock.Setup(r => r.GetAvailableForCandidatePagedAsync(6, null, 1, 100))
                         .ReturnsAsync((new List<Exam>(), 0));

            var result = await _sut.GetAvailableForCandidatePagedAsync(
                userId: 6, categoryId: null, page: -3, pageSize: 999);

            result.Page.Should().Be(1);
            result.PageSize.Should().Be(100);
        }

        [Test]
        public async Task GetMineAsync_DelegatesToRepository()
        {
            _examRepoMock.Setup(r => r.GetByExaminerAsync(10))
                         .ReturnsAsync(new List<Exam> { TestDataBuilder.BuildDraftExam(1, 10) });

            var result = await _sut.GetMineAsync(10);

            result.Should().HaveCount(1);
        }

        [Test]
        public async Task GetMinePagedAsync_ReturnsPagedResult()
        {
            _examRepoMock.Setup(r => r.GetByExaminerPagedAsync(10, 2, 5))
                         .ReturnsAsync((new List<Exam> { TestDataBuilder.BuildDraftExam(1, 10) }, 11));

            var result = await _sut.GetMinePagedAsync(10, page: 2, pageSize: 5);

            result.TotalCount.Should().Be(11);
            result.Page.Should().Be(2);
            result.Items.Should().HaveCount(1);
        }

        [Test]
        public async Task GetByIdWithQuestionsAndOptionsAsync_Found_ReturnsExam()
        {
            var exam = TestDataBuilder.BuildPublishedExam(1, 10);
            _examRepoMock.Setup(r => r.GetByIdWithQuestionsAndOptionsAsync(1)).ReturnsAsync(exam);

            var result = await _sut.GetByIdWithQuestionsAndOptionsAsync(1);

            result.ExamId.Should().Be(1);
        }

        [Test]
        public async Task GetByIdWithQuestionsAndOptionsAsync_NotFound_ThrowsExamNotFoundException()
        {
            _examRepoMock.Setup(r => r.GetByIdWithQuestionsAndOptionsAsync(999))
                         .ReturnsAsync((Exam?)null);

            var act = async () => await _sut.GetByIdWithQuestionsAndOptionsAsync(999);
            await act.Should().ThrowAsync<ExamNotFoundException>();
        }

        // ════════════════════════════════════════════════════════════════
        //   UpdateAsync — HAPPY PATH + VALIDATION
        // ════════════════════════════════════════════════════════════════

        [Test]
        public async Task UpdateAsync_ValidInput_UpdatesAllFields()
        {
            var exam = TestDataBuilder.BuildDraftExam(1, examinerId: 10);
            _examRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(exam);
            _examRepoMock.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);

            var result = await _sut.UpdateAsync(1, 10, "  New Title  ", " notes ", 90, 100m, 40m,
                DateTime.UtcNow.AddHours(1), DateTime.UtcNow.AddHours(4));

            result.Title.Should().Be("New Title");          // trimmed
            result.Instructions.Should().Be("notes");        // trimmed
            result.DurationMinutes.Should().Be(90);
            _examRepoMock.Verify(r => r.Update(exam), Times.Once);
        }

        // (title, duration, totalMarks, passingMarks, startOffsetHours, endOffsetHours)
        [TestCase("",      60, 50, 30, 1, 3, TestName = "Update_EmptyTitle")]
        [TestCase("Title",  0, 50, 30, 1, 3, TestName = "Update_ZeroDuration")]
        [TestCase("Title", 60,  0, 30, 1, 3, TestName = "Update_ZeroTotalMarks")]
        [TestCase("Title", 60, 50, 60, 1, 3, TestName = "Update_PassingExceedsTotal")]
        [TestCase("Title", 60, 50, 30, 3, 1, TestName = "Update_StartAfterEnd")]
        public async Task UpdateAsync_InvalidInput_ThrowsValidationException(
            string title, int duration, decimal total, decimal passing,
            int startOffset, int endOffset)
        {
            var exam = TestDataBuilder.BuildDraftExam(1, examinerId: 10);
            _examRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(exam);

            var act = async () => await _sut.UpdateAsync(1, 10, title, null, duration, total, passing,
                DateTime.UtcNow.AddHours(startOffset), DateTime.UtcNow.AddHours(endOffset));
            await act.Should().ThrowAsync<ValidationException>();
        }

        // ════════════════════════════════════════════════════════════════
        //   AttachQuestionsAsync — HAPPY PATH + GUARDS
        // ════════════════════════════════════════════════════════════════

        [Test]
        public async Task AttachQuestionsAsync_OwnActiveQuestion_AttachesWithNextOrder()
        {
            var exam = TestDataBuilder.BuildDraftExam(1, examinerId: 10);
            _examRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(exam);
            _examRepoMock.Setup(r => r.GetByIdWithQuestionsAsync(1)).ReturnsAsync(exam);

            var q = TestDataBuilder.BuildQuestion(5, examinerId: 10);
            _questionRepoMock.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(q);
            _examQRepoMock.Setup(r => r.GetByExamIdAsync(1))
                          .ReturnsAsync(new List<ExamQuestion>()); // 0 already attached
            _examQRepoMock.Setup(r => r.ExistsAsync(1, 5)).ReturnsAsync(false);
            _examQRepoMock.Setup(r => r.AddAsync(It.IsAny<ExamQuestion>()))
                          .ReturnsAsync((ExamQuestion eq) => eq);
            _examQRepoMock.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);

            await _sut.AttachQuestionsAsync(1, new[] { 5 }, 10);

            _examQRepoMock.Verify(r => r.AddAsync(
                It.Is<ExamQuestion>(eq => eq.QuestionId == 5 && eq.QuestionOrder == 1)), Times.Once);
        }

        [Test]
        public async Task AttachQuestionsAsync_AlreadyAttached_SkipsDuplicate()
        {
            var exam = TestDataBuilder.BuildDraftExam(1, examinerId: 10);
            _examRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(exam);
            _examRepoMock.Setup(r => r.GetByIdWithQuestionsAsync(1)).ReturnsAsync(exam);

            var q = TestDataBuilder.BuildQuestion(5, examinerId: 10);
            _questionRepoMock.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(q);
            _examQRepoMock.Setup(r => r.GetByExamIdAsync(1))
                          .ReturnsAsync(new List<ExamQuestion>());
            _examQRepoMock.Setup(r => r.ExistsAsync(1, 5)).ReturnsAsync(true); // duplicate
            _examQRepoMock.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);

            await _sut.AttachQuestionsAsync(1, new[] { 5 }, 10);

            _examQRepoMock.Verify(r => r.AddAsync(It.IsAny<ExamQuestion>()), Times.Never);
        }

        [Test]
        public async Task AttachQuestionsAsync_InactiveQuestion_ThrowsValidationException()
        {
            var exam = TestDataBuilder.BuildDraftExam(1, examinerId: 10);
            _examRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(exam);

            var q = TestDataBuilder.BuildQuestion(5, examinerId: 10, isActive: false);
            _questionRepoMock.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(q);
            _examQRepoMock.Setup(r => r.GetByExamIdAsync(1))
                          .ReturnsAsync(new List<ExamQuestion>());

            var act = async () => await _sut.AttachQuestionsAsync(1, new[] { 5 }, 10);
            await act.Should().ThrowAsync<ValidationException>()
                     .WithMessage("*does not exist or is inactive*");
        }

        // ════════════════════════════════════════════════════════════════
        //   AttachQuestionsAsync — MARKS-CAP RULE
        // ════════════════════════════════════════════════════════════════

        // Wires the standard happy-path mocks for attach tests.
        private void SetUpAttachMocks(Exam exam)
        {
            _examRepoMock.Setup(r => r.GetByIdAsync(exam.ExamId)).ReturnsAsync(exam);
            _examRepoMock.Setup(r => r.GetByIdWithQuestionsAsync(exam.ExamId)).ReturnsAsync(exam);
            _examQRepoMock.Setup(r => r.ExistsAsync(exam.ExamId, It.IsAny<int>())).ReturnsAsync(false);
            _examQRepoMock.Setup(r => r.GetByExamIdAsync(exam.ExamId))
                          .ReturnsAsync(new List<ExamQuestion>());
            _examQRepoMock.Setup(r => r.AddAsync(It.IsAny<ExamQuestion>()))
                          .ReturnsAsync((ExamQuestion eq) => eq);
            _examQRepoMock.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);
        }

        [Test]
        public async Task AttachQuestionsAsync_ProjectedTotalExceedsExamMarks_ThrowsAndAttachesNothing()
        {
            var exam = TestDataBuilder.BuildDraftExam(1, examinerId: 10, totalMarks: 20m);
            SetUpAttachMocks(exam);

            // 15 + 10 = 25 > 20 — the whole request must be rejected atomically.
            _questionRepoMock.Setup(r => r.GetByIdAsync(1))
                             .ReturnsAsync(TestDataBuilder.BuildQuestion(1, examinerId: 10, marks: 15m));
            _questionRepoMock.Setup(r => r.GetByIdAsync(2))
                             .ReturnsAsync(TestDataBuilder.BuildQuestion(2, examinerId: 10, marks: 10m));

            var act = async () => await _sut.AttachQuestionsAsync(1, new[] { 1, 2 }, 10);

            await act.Should().ThrowAsync<ValidationException>()
                     .WithMessage("*would exceed*20 marks*");
            _examQRepoMock.Verify(r => r.AddAsync(It.IsAny<ExamQuestion>()), Times.Never);
        }

        [Test]
        public async Task AttachQuestionsAsync_CountsAlreadyAttachedMarksTowardTheCap()
        {
            var exam = TestDataBuilder.BuildDraftExam(1, examinerId: 10, totalMarks: 50m);
            SetUpAttachMocks(exam);

            // 40 marks already attached; adding a 20-mark question → 60 > 50.
            var attached = TestDataBuilder.BuildQuestion(9, examinerId: 10, marks: 40m);
            _examQRepoMock.Setup(r => r.GetByExamIdAsync(1))
                          .ReturnsAsync(new List<ExamQuestion>
                          {
                              new() { ExamId = 1, QuestionId = 9, Question = attached, QuestionOrder = 1 }
                          });
            _questionRepoMock.Setup(r => r.GetByIdAsync(2))
                             .ReturnsAsync(TestDataBuilder.BuildQuestion(2, examinerId: 10, marks: 20m));

            var act = async () => await _sut.AttachQuestionsAsync(1, new[] { 2 }, 10);

            await act.Should().ThrowAsync<ValidationException>()
                     .WithMessage("*only 10 marks remain*");
        }

        [Test]
        public async Task AttachQuestionsAsync_ExactlyFillingTheTotal_IsAllowed()
        {
            var exam = TestDataBuilder.BuildDraftExam(1, examinerId: 10, totalMarks: 20m);
            SetUpAttachMocks(exam);

            _questionRepoMock.Setup(r => r.GetByIdAsync(1))
                             .ReturnsAsync(TestDataBuilder.BuildQuestion(1, examinerId: 10, marks: 15m));
            _questionRepoMock.Setup(r => r.GetByIdAsync(2))
                             .ReturnsAsync(TestDataBuilder.BuildQuestion(2, examinerId: 10, marks: 5m));

            await _sut.AttachQuestionsAsync(1, new[] { 1, 2 }, 10);

            _examQRepoMock.Verify(r => r.AddAsync(It.IsAny<ExamQuestion>()), Times.Exactly(2));
        }

        [Test]
        public async Task AttachQuestionsAsync_DuplicateIdsInOneRequest_AttachOnlyOnce()
        {
            var exam = TestDataBuilder.BuildDraftExam(1, examinerId: 10, totalMarks: 50m);
            SetUpAttachMocks(exam);

            _questionRepoMock.Setup(r => r.GetByIdAsync(5))
                             .ReturnsAsync(TestDataBuilder.BuildQuestion(5, examinerId: 10, marks: 10m));

            // Same id twice — without deduping this would violate the DB
            // unique index on (exam_id, question_id).
            await _sut.AttachQuestionsAsync(1, new[] { 5, 5 }, 10);

            _examQRepoMock.Verify(r => r.AddAsync(
                It.Is<ExamQuestion>(eq => eq.QuestionId == 5)), Times.Once);
        }

        [Test]
        public async Task AttachQuestionsAsync_QuestionsFromDifferentCategories_AreAllAttached()
        {
            var exam = TestDataBuilder.BuildDraftExam(1, examinerId: 10, totalMarks: 30m);
            SetUpAttachMocks(exam);

            // Three questions from three different categories — multi-category
            // exams are explicitly supported.
            _questionRepoMock.Setup(r => r.GetByIdAsync(1))
                             .ReturnsAsync(TestDataBuilder.BuildQuestion(1, examinerId: 10, catId: 1));
            _questionRepoMock.Setup(r => r.GetByIdAsync(2))
                             .ReturnsAsync(TestDataBuilder.BuildQuestion(2, examinerId: 10, catId: 2));
            _questionRepoMock.Setup(r => r.GetByIdAsync(3))
                             .ReturnsAsync(TestDataBuilder.BuildQuestion(3, examinerId: 10, catId: 7));

            await _sut.AttachQuestionsAsync(1, new[] { 1, 2, 3 }, 10);

            _examQRepoMock.Verify(r => r.AddAsync(It.IsAny<ExamQuestion>()), Times.Exactly(3));
        }

        // ════════════════════════════════════════════════════════════════
        //   PublishAsync — CANDIDATE EMAILS
        // ════════════════════════════════════════════════════════════════

        // Publishable exam + a mixed candidate list (2 active, 1 inactive).
        private Exam SetUpPublishableExamWithCandidates()
        {
            var exam = TestDataBuilder.BuildDraftExam(1, 10, totalMarks: 50m);
            var q = TestDataBuilder.BuildQuestion(1, marks: 50m);
            exam.ExamQuestions = new List<ExamQuestion>
            {
                new() { ExamId = 1, QuestionId = 1, Question = q, QuestionOrder = 1 }
            };
            _examRepoMock.Setup(r => r.GetByIdWithQuestionsAsync(1)).ReturnsAsync(exam);
            _examRepoMock.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);
            _userRepoMock.Setup(r => r.GetAllByRoleAsync(UserRole.Candidate))
                         .ReturnsAsync(new List<User>
                         {
                             TestDataBuilder.BuildCandidate(1),
                             TestDataBuilder.BuildCandidate(2),
                             TestDataBuilder.BuildCandidate(3, isActive: false),
                         });
            _notifRepoMock.Setup(r => r.AddAsync(It.IsAny<Notification>()))
                          .ReturnsAsync((Notification n) => n);
            _notifRepoMock.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);
            return exam;
        }

        [Test]
        public async Task PublishAsync_EmailsEveryActiveCandidate_ButNotInactiveOnes()
        {
            SetUpPublishableExamWithCandidates();

            await _sut.PublishAsync(1, 10);

            // Exactly 2 individual emails (active candidates only) with the
            // exam title in the subject line.
            _emailMock.Verify(e => e.SendAsync(
                It.IsAny<string>(),
                It.Is<string>(s => s.Contains("New Exam Available")),
                It.IsAny<string>(), true), Times.Exactly(2));
            _emailMock.Verify(e => e.SendAsync(
                "candidate3@test.com", It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()),
                Times.Never);
        }

        [Test]
        public async Task PublishAsync_EmailFailure_DoesNotUndoThePublishOrBlockOtherEmails()
        {
            SetUpPublishableExamWithCandidates();
            _emailMock.Setup(e => e.SendAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()))
                      .ThrowsAsync(new InvalidOperationException("SMTP down"));

            var result = await _sut.PublishAsync(1, 10);

            // Both sends were attempted (per-candidate try/catch) and the
            // publish stands.
            result.ExamStatus.Should().Be(ExamStatus.Published);
            _emailMock.Verify(e => e.SendAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()),
                Times.Exactly(2));
        }

        [Test]
        public async Task PublishAsync_EmailFailure_AlertsAdmins()
        {
            SetUpPublishableExamWithCandidates();
            // There is one active admin to receive the escalation.
            _userRepoMock.Setup(r => r.GetAllByRoleAsync(UserRole.Admin))
                         .ReturnsAsync(new List<User> { TestDataBuilder.BuildAdmin(99) });
            _emailMock.Setup(e => e.SendAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()))
                      .ThrowsAsync(new InvalidOperationException("SMTP down"));

            await _sut.PublishAsync(1, 10);

            // The admin (id 99) receives a SystemAlert about the failed emails.
            _notifRepoMock.Verify(r => r.AddAsync(It.Is<Notification>(n =>
                n.UserId == 99 && n.NotifType == NotificationType.SystemAlert)), Times.Once);
        }

        // ════════════════════════════════════════════════════════════════
        //   DetachQuestionAsync
        // ════════════════════════════════════════════════════════════════

        [Test]
        public async Task DetachQuestionAsync_OwnedDraftExam_Detaches()
        {
            var exam = TestDataBuilder.BuildDraftExam(1, examinerId: 10);
            _examRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(exam);
            _examRepoMock.Setup(r => r.GetByIdWithQuestionsAsync(1)).ReturnsAsync(exam);
            _examQRepoMock.Setup(r => r.DeleteByExamAndQuestionAsync(1, 5))
                          .Returns(Task.CompletedTask);

            await _sut.DetachQuestionAsync(1, 5, 10);

            _examQRepoMock.Verify(r => r.DeleteByExamAndQuestionAsync(1, 5), Times.Once);
        }

        // ════════════════════════════════════════════════════════════════
        //   PublishAsync — HAPPY PATH + REMAINING RULES
        // ════════════════════════════════════════════════════════════════

        [Test]
        public async Task PublishAsync_ValidDraftExam_TransitionsToPublished()
        {
            var exam = TestDataBuilder.BuildDraftExam(1, 10, totalMarks: 50m);
            var q = TestDataBuilder.BuildQuestion(1, marks: 50m); // marks sum == TotalMarks
            exam.ExamQuestions = new List<ExamQuestion>
            {
                new() { ExamId = 1, QuestionId = 1, Question = q, QuestionOrder = 1 }
            };
            _examRepoMock.Setup(r => r.GetByIdWithQuestionsAsync(1)).ReturnsAsync(exam);
            _examRepoMock.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);

            var result = await _sut.PublishAsync(1, 10);

            result.ExamStatus.Should().Be(ExamStatus.Published);
            _examRepoMock.Verify(r => r.Update(exam), Times.Once);
        }

        [Test]
        public async Task CreateAsync_UnspecifiedKindDates_AreStoredAsUtc()
        {
            // datetime-local browser inputs arrive with Kind=Unspecified;
            // Postgres timestamptz rejects those — the service must normalize.
            _examRepoMock.Setup(r => r.AddAsync(It.IsAny<Exam>()))
                         .ReturnsAsync((Exam e) => e);
            _examRepoMock.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);

            var start = DateTime.SpecifyKind(DateTime.UtcNow.AddHours(1), DateTimeKind.Unspecified);
            var end   = DateTime.SpecifyKind(DateTime.UtcNow.AddHours(3), DateTimeKind.Unspecified);

            var result = await _sut.CreateAsync(10, 1, "Exam", null, 60, 50m, 30m, start, end);

            result.StartTime.Kind.Should().Be(DateTimeKind.Utc);
            result.EndTime.Kind.Should().Be(DateTimeKind.Utc);
        }

        [Test]
        public async Task PublishAsync_NotifiesEveryActiveCandidate()
        {
            var exam = TestDataBuilder.BuildDraftExam(1, 10, totalMarks: 50m);
            var q = TestDataBuilder.BuildQuestion(1, marks: 50m);
            exam.ExamQuestions = new List<ExamQuestion>
            {
                new() { ExamId = 1, QuestionId = 1, Question = q, QuestionOrder = 1 }
            };
            _examRepoMock.Setup(r => r.GetByIdWithQuestionsAsync(1)).ReturnsAsync(exam);
            _examRepoMock.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);

            // Two active candidates + one deactivated (must be skipped).
            _userRepoMock.Setup(r => r.GetAllByRoleAsync(UserRole.Candidate))
                         .ReturnsAsync(new List<User>
                         {
                             TestDataBuilder.BuildCandidate(1),
                             TestDataBuilder.BuildCandidate(2),
                             TestDataBuilder.BuildCandidate(3, isActive: false),
                         });
            _notifRepoMock.Setup(r => r.AddAsync(It.IsAny<Notification>()))
                          .ReturnsAsync((Notification n) => n);
            _notifRepoMock.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);

            await _sut.PublishAsync(1, 10);

            _notifRepoMock.Verify(r => r.AddAsync(
                It.Is<Notification>(n => n.NotifType == NotificationType.ExamPublished)),
                Times.Exactly(2));
        }

        [Test]
        public async Task PublishAsync_NotificationFailure_DoesNotUndoThePublish()
        {
            var exam = TestDataBuilder.BuildDraftExam(1, 10, totalMarks: 50m);
            var q = TestDataBuilder.BuildQuestion(1, marks: 50m);
            exam.ExamQuestions = new List<ExamQuestion>
            {
                new() { ExamId = 1, QuestionId = 1, Question = q, QuestionOrder = 1 }
            };
            _examRepoMock.Setup(r => r.GetByIdWithQuestionsAsync(1)).ReturnsAsync(exam);
            _examRepoMock.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);
            _userRepoMock.Setup(r => r.GetAllByRoleAsync(UserRole.Candidate))
                         .ThrowsAsync(new InvalidOperationException("users table down"));

            var result = await _sut.PublishAsync(1, 10);

            // The announcement is best-effort; the publish itself must stand.
            result.ExamStatus.Should().Be(ExamStatus.Published);
        }

        [Test]
        public async Task PublishAsync_PassingMarksExceedTotal_ThrowsExamNotPublishableException()
        {
            var exam = TestDataBuilder.BuildDraftExam(1, 10, totalMarks: 50m, passingMarks: 60m);
            var q = TestDataBuilder.BuildQuestion(1, marks: 50m);
            exam.ExamQuestions = new List<ExamQuestion>
            {
                new() { ExamId = 1, QuestionId = 1, Question = q, QuestionOrder = 1 }
            };
            _examRepoMock.Setup(r => r.GetByIdWithQuestionsAsync(1)).ReturnsAsync(exam);

            var act = async () => await _sut.PublishAsync(1, 10);
            await act.Should().ThrowAsync<ExamNotPublishableException>()
                     .WithMessage("*PassingMarks*");
        }

        [Test]
        public async Task PublishAsync_StartTimeAfterEndTime_ThrowsExamNotPublishableException()
        {
            var exam = TestDataBuilder.BuildDraftExam(1, 10, totalMarks: 50m);
            exam.StartTime = DateTime.UtcNow.AddHours(5);
            exam.EndTime   = DateTime.UtcNow.AddHours(1);
            var q = TestDataBuilder.BuildQuestion(1, marks: 50m);
            exam.ExamQuestions = new List<ExamQuestion>
            {
                new() { ExamId = 1, QuestionId = 1, Question = q, QuestionOrder = 1 }
            };
            _examRepoMock.Setup(r => r.GetByIdWithQuestionsAsync(1)).ReturnsAsync(exam);

            var act = async () => await _sut.PublishAsync(1, 10);
            await act.Should().ThrowAsync<ExamNotPublishableException>()
                     .WithMessage("*StartTime*");
        }

        [Test]
        public async Task PublishAsync_WrongOwner_ThrowsExamNotFoundException()
        {
            var exam = TestDataBuilder.BuildDraftExam(1, examinerId: 10);
            _examRepoMock.Setup(r => r.GetByIdWithQuestionsAsync(1)).ReturnsAsync(exam);

            var act = async () => await _sut.PublishAsync(1, 99);
            await act.Should().ThrowAsync<ExamNotFoundException>();
        }

        // ════════════════════════════════════════════════════════════════
        //   PublishResultsAsync — FULL FLOW
        // ════════════════════════════════════════════════════════════════

        // Builds a Published exam plus one passing and one failing attempt (with Users),
        // and wires the default mocks for the happy path.
        private Exam SetUpPublishResultsScenario(bool certAlreadyExists = false)
        {
            var exam = TestDataBuilder.BuildPublishedExam(1, 10, totalMarks: 50m, passingMarks: 30m);
            _examRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(exam);
            _examRepoMock.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);

            var passer = TestDataBuilder.BuildCandidate(1);
            var failer = TestDataBuilder.BuildCandidate(2);

            // Passer has TWO attempts (30 and 40) — the best one (40) must win.
            var passLow  = TestDataBuilder.BuildSubmittedAttempt(1, userId: 1, examId: 1, score: 30m);
            var passHigh = TestDataBuilder.BuildSubmittedAttempt(2, userId: 1, examId: 1, score: 40m);
            var fail     = TestDataBuilder.BuildSubmittedAttempt(3, userId: 2, examId: 1, score: 10m, isPassed: false);
            passLow.User = passer; passHigh.User = passer; fail.User = failer;

            _attemptRepoMock.Setup(r => r.GetByExamIdAsync(1))
                            .ReturnsAsync(new List<Attempt> { passLow, passHigh, fail });

            _certRepoMock.Setup(r => r.ExistsForUserAndExamAsync(It.IsAny<int>(), 1))
                         .ReturnsAsync(certAlreadyExists);
            _certRepoMock.Setup(r => r.AddAsync(It.IsAny<Certificate>()))
                         .ReturnsAsync((Certificate c) => c);
            _certRepoMock.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);
            _notifRepoMock.Setup(r => r.AddAsync(It.IsAny<Notification>()))
                          .ReturnsAsync((Notification n) => n);

            return exam;
        }

        [Test]
        public async Task PublishResultsAsync_HappyPath_IssuesCertificatesNotificationsAndEmails()
        {
            var exam = SetUpPublishResultsScenario();

            var result = await _sut.PublishResultsAsync(1, 10);

            result.ExamStatus.Should().Be(ExamStatus.ResultsPublished);

            // One NEW certificate — for the passer's best attempt only.
            _certRepoMock.Verify(r => r.AddAsync(
                It.Is<Certificate>(c => c.UserId == 1 && c.AttemptId == 2)), Times.Once);

            // Notifications: 1 CertificateIssued + 2 ResultPublished (pass + fail).
            _notifRepoMock.Verify(r => r.AddAsync(It.IsAny<Notification>()), Times.Exactly(3));

            // Emails: ResultPublished ×2 + YouPassed ×1 + CertificateAvailable ×1 = 4.
            _emailMock.Verify(e => e.SendAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), true), Times.Exactly(4));
        }

        [Test]
        public async Task PublishResultsAsync_PremiumExamVerifiedInstitution_SnapshotsBrandingOntoCertificate()
        {
            var exam = SetUpPublishResultsScenario();
            exam.IsPremium = true;
            exam.Price = 999m;
            _institutionRepoMock.Setup(r => r.GetByOwnerUserIdAsync(10)).ReturnsAsync(new Institution
            {
                InstitutionId = 1, OwnerUserId = 10, Name = "Acme Institute", LogoUrl = "https://cdn/acme.png",
                VerificationStatus = InstitutionVerificationStatus.Verified, RequestedAt = DateTime.UtcNow
            });

            await _sut.PublishResultsAsync(1, 10);

            _certRepoMock.Verify(r => r.AddAsync(It.Is<Certificate>(c =>
                c.IsPremium && c.InstituteName == "Acme Institute" && c.InstituteLogoUrl == "https://cdn/acme.png")),
                Times.Once);
        }

        [Test]
        public async Task PublishResultsAsync_PremiumExamUnverifiedInstitution_NoInstituteBrandingSnapshotted()
        {
            var exam = SetUpPublishResultsScenario();
            exam.IsPremium = true;
            exam.Price = 999m;
            _institutionRepoMock.Setup(r => r.GetByOwnerUserIdAsync(10)).ReturnsAsync(new Institution
            {
                InstitutionId = 1, OwnerUserId = 10, Name = "Acme Institute",
                VerificationStatus = InstitutionVerificationStatus.Pending, RequestedAt = DateTime.UtcNow
            });

            await _sut.PublishResultsAsync(1, 10);

            _certRepoMock.Verify(r => r.AddAsync(It.Is<Certificate>(c =>
                c.IsPremium && c.InstituteName == null && c.InstituteLogoUrl == null)),
                Times.Once);
        }

        [Test]
        public async Task PublishResultsAsync_NonPremiumExam_CertificateIsPremiumFalse()
        {
            SetUpPublishResultsScenario(); // exam.IsPremium defaults to false

            await _sut.PublishResultsAsync(1, 10);

            _certRepoMock.Verify(r => r.AddAsync(It.Is<Certificate>(c => !c.IsPremium)), Times.Once);
            _institutionRepoMock.Verify(r => r.GetByOwnerUserIdAsync(It.IsAny<int>()), Times.Never);
        }

        [Test]
        public async Task PublishResultsAsync_CertificateAlreadyExists_DoesNotIssueAgain()
        {
            SetUpPublishResultsScenario(certAlreadyExists: true);

            await _sut.PublishResultsAsync(1, 10);

            _certRepoMock.Verify(r => r.AddAsync(It.IsAny<Certificate>()), Times.Never);
            // Only the 2 ResultPublished notifications — no CertificateIssued.
            _notifRepoMock.Verify(r => r.AddAsync(It.IsAny<Notification>()), Times.Exactly(2));
        }

        [Test]
        public async Task PublishResultsAsync_EmailFailures_DoNotFailThePublish()
        {
            SetUpPublishResultsScenario();
            _emailMock.Setup(e => e.SendAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()))
                      .ThrowsAsync(new InvalidOperationException("SMTP down"));

            var result = await _sut.PublishResultsAsync(1, 10);

            // All three email try/catch blocks swallow the failure.
            result.ExamStatus.Should().Be(ExamStatus.ResultsPublished);
        }

        [Test]
        public async Task PublishResultsAsync_SaveFailure_RollsBackAndRethrows()
        {
            SetUpPublishResultsScenario();
            _examRepoMock.Setup(r => r.SaveChangesAsync())
                         .ThrowsAsync(new InvalidOperationException("DB down"));

            var act = async () => await _sut.PublishResultsAsync(1, 10);

            await act.Should().ThrowAsync<InvalidOperationException>();
            // No emails go out when the transaction fails.
            _emailMock.Verify(e => e.SendAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()), Times.Never);
        }

        // ════════════════════════════════════════════════════════════════
        //   UnpublishAsync — remaining branches
        // ════════════════════════════════════════════════════════════════

        [Test]
        public async Task UnpublishAsync_AdminBypassesOwnershipCheck()
        {
            var exam = TestDataBuilder.BuildPublishedExam(1, examinerId: 10);
            _examRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(exam);
            _attemptRepoMock.Setup(r => r.HasAnyForExamAsync(1)).ReturnsAsync(false);
            _examRepoMock.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);

            // Caller 99 is NOT the owner but IS admin.
            var result = await _sut.UnpublishAsync(1, callerId: 99, isAdmin: true);

            result.ExamStatus.Should().Be(ExamStatus.Draft);
        }

        [Test]
        public async Task UnpublishAsync_ExamNotFound_ThrowsExamNotFoundException()
        {
            _examRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Exam?)null);

            var act = async () => await _sut.UnpublishAsync(999, 10, false);
            await act.Should().ThrowAsync<ExamNotFoundException>();
        }
    }
}
