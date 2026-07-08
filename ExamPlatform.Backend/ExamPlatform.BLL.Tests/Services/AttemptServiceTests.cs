// AttemptServiceTests.cs — covers StartOrResume, GetById, SaveAnswer, Submit, GetResult.

using ExamPlatform.BLL.Exceptions;
using ExamPlatform.BLL.Services;
using ExamPlatform.BLL.Tests.Helpers;
using ExamPlatform.DAL.Interfaces;
using ExamPlatform.Domain.Entities;
using ExamPlatform.Domain.Enums;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace ExamPlatform.BLL.Tests.Services
{
    [TestFixture]
    public class AttemptServiceTests
    {
        private Mock<IAttemptRepository>        _attemptRepoMock = null!;
        private Mock<IAttemptAnswerRepository>  _answerRepoMock  = null!;
        private Mock<IExamRepository>           _examRepoMock    = null!;
        private Mock<INotificationRepository>   _notifRepoMock   = null!;
        private Mock<ILogger<AttemptService>>   _loggerMock      = null!;
        private AttemptService _sut = null!;

        [SetUp]
        public void SetUp()
        {
            _attemptRepoMock = new Mock<IAttemptRepository>();
            _answerRepoMock  = new Mock<IAttemptAnswerRepository>();
            _examRepoMock    = new Mock<IExamRepository>();
            _notifRepoMock   = new Mock<INotificationRepository>();
            _loggerMock      = new Mock<ILogger<AttemptService>>();

            _sut = new AttemptService(
                _attemptRepoMock.Object,
                _answerRepoMock.Object,
                _examRepoMock.Object,
                _notifRepoMock.Object,
                _loggerMock.Object);
        }

        // ════════════════════════════════════════════════════════════════
        //   StartOrResumeAsync
        // ════════════════════════════════════════════════════════════════

        [Test]
        public async Task StartOrResumeAsync_ExamNotFound_ThrowsExamNotFoundException()
        {
            _examRepoMock.Setup(r => r.GetByIdWithQuestionsAndOptionsAsync(It.IsAny<int>()))
                         .ReturnsAsync((Exam?)null);

            var act = async () => await _sut.StartOrResumeAsync(1, 99);
            await act.Should().ThrowAsync<ExamNotFoundException>();
        }

        [Test]
        public async Task StartOrResumeAsync_ExamNotPublished_ThrowsExamNotOpenForAttemptException()
        {
            var exam = TestDataBuilder.BuildDraftExam(); // Draft status
            _examRepoMock.Setup(r => r.GetByIdWithQuestionsAndOptionsAsync(1)).ReturnsAsync(exam);

            var act = async () => await _sut.StartOrResumeAsync(1, 1);
            await act.Should().ThrowAsync<ExamNotOpenForAttemptException>();
        }

        [Test]
        public async Task StartOrResumeAsync_ExamWindowNotYetOpen_ThrowsExamNotOpenForAttemptException()
        {
            var exam = TestDataBuilder.BuildPublishedExam();
            exam.StartTime = DateTime.UtcNow.AddHours(1);
            exam.EndTime   = DateTime.UtcNow.AddHours(3);
            _examRepoMock.Setup(r => r.GetByIdWithQuestionsAndOptionsAsync(1)).ReturnsAsync(exam);

            var act = async () => await _sut.StartOrResumeAsync(1, 1);
            await act.Should().ThrowAsync<ExamNotOpenForAttemptException>();
        }

        [Test]
        public async Task StartOrResumeAsync_ExamWindowClosed_ThrowsExamNotOpenForAttemptException()
        {
            var exam = TestDataBuilder.BuildPublishedExam();
            exam.StartTime = DateTime.UtcNow.AddHours(-3);
            exam.EndTime   = DateTime.UtcNow.AddHours(-1);
            _examRepoMock.Setup(r => r.GetByIdWithQuestionsAndOptionsAsync(1)).ReturnsAsync(exam);

            var act = async () => await _sut.StartOrResumeAsync(1, 1);
            await act.Should().ThrowAsync<ExamNotOpenForAttemptException>();
        }

        [Test]
        public async Task StartOrResumeAsync_ValidWindowNoExistingAttempt_CreatesNewAttempt()
        {
            // ─── ARRANGE ──────────────────────────────────────────────
            var exam = TestDataBuilder.BuildPublishedExam(1);
            _examRepoMock.Setup(r => r.GetByIdWithQuestionsAndOptionsAsync(1)).ReturnsAsync(exam);
            _attemptRepoMock.Setup(r => r.GetInProgressAsync(1, 1)).ReturnsAsync((Attempt?)null);
            // AddAsync returns Task<Attempt>
            _attemptRepoMock.Setup(r => r.AddAsync(It.IsAny<Attempt>()))
                            .ReturnsAsync((Attempt a) => a);
            _attemptRepoMock.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);

            // ─── ACT ──────────────────────────────────────────────────
            var result = await _sut.StartOrResumeAsync(1, 1);

            // ─── ASSERT ───────────────────────────────────────────────
            result.AttemptStatus.Should().Be(AttemptStatus.InProgress);
            _attemptRepoMock.Verify(r => r.AddAsync(It.IsAny<Attempt>()), Times.Once);
        }

        [Test]
        public async Task StartOrResumeAsync_ExistingInProgressAttempt_ReturnsSameAttempt()
        {
            // ─── ARRANGE ──────────────────────────────────────────────
            var exam     = TestDataBuilder.BuildPublishedExam(1);
            var existing = TestDataBuilder.BuildInProgressAttempt(42, 1, 1);

            // Bug fix: resume now calls GetByIdWithAnswersAndExamAsync to load
            // the full attempt (with AttemptAnswers) instead of returning the
            // bare `existing` with an empty answers collection.
            // We simulate the full load by building an attempt with Exam attached.
            var fullAttempt = TestDataBuilder.BuildInProgressAttempt(42, 1, 1);
            fullAttempt.Exam = exam; // needed so ComputeRemainingSeconds can read DurationMinutes

            _examRepoMock.Setup(r => r.GetByIdWithQuestionsAndOptionsAsync(1)).ReturnsAsync(exam);
            _attemptRepoMock.Setup(r => r.GetInProgressAsync(1, 1)).ReturnsAsync(existing);
            _attemptRepoMock.Setup(r => r.GetByIdWithAnswersAndExamAsync(42))
                            .ReturnsAsync(fullAttempt);

            // ─── ACT ──────────────────────────────────────────────────
            var result = await _sut.StartOrResumeAsync(1, 1);

            // ─── ASSERT ───────────────────────────────────────────────
            result.AttemptId.Should().Be(42);
            // Resume must NOT create a new attempt row.
            _attemptRepoMock.Verify(r => r.AddAsync(It.IsAny<Attempt>()), Times.Never);
            // Resume must call the full-load method so answers are populated.
            _attemptRepoMock.Verify(r => r.GetByIdWithAnswersAndExamAsync(42), Times.Once);
        }

        // ════════════════════════════════════════════════════════════════
        //   GetByIdAsync — ownership
        // ════════════════════════════════════════════════════════════════

        [Test]
        public async Task GetByIdAsync_AttemptBelongsToAnotherUser_ThrowsAttemptNotFoundException()
        {
            var attempt = TestDataBuilder.BuildInProgressAttempt(1, userId: 2);
            attempt.Exam = TestDataBuilder.BuildPublishedExam();
            _attemptRepoMock.Setup(r => r.GetByIdWithAnswersAndExamAsync(1)).ReturnsAsync(attempt);

            var act = async () => await _sut.GetByIdAsync(1, userId: 1);
            await act.Should().ThrowAsync<AttemptNotFoundException>();
        }

        // ════════════════════════════════════════════════════════════════
        //   SubmitAsync
        // ════════════════════════════════════════════════════════════════

        [Test]
        public async Task SubmitAsync_AlreadySubmitted_ThrowsAttemptAlreadySubmittedException()
        {
            var attempt = TestDataBuilder.BuildSubmittedAttempt(1, 1);
            attempt.Exam = TestDataBuilder.BuildPublishedExam();
            _attemptRepoMock.Setup(r => r.GetByIdWithAnswersAndExamAsync(1)).ReturnsAsync(attempt);

            var act = async () => await _sut.SubmitAsync(1, 1);
            await act.Should().ThrowAsync<AttemptAlreadySubmittedException>();
        }

        [Test]
        public async Task SubmitAsync_AttemptBelongsToAnotherUser_ThrowsAttemptNotFoundException()
        {
            var attempt = TestDataBuilder.BuildInProgressAttempt(1, userId: 2);
            attempt.Exam = TestDataBuilder.BuildPublishedExam();
            _attemptRepoMock.Setup(r => r.GetByIdWithAnswersAndExamAsync(1)).ReturnsAsync(attempt);

            var act = async () => await _sut.SubmitAsync(1, userId: 1);
            await act.Should().ThrowAsync<AttemptNotFoundException>();
        }

        // ════════════════════════════════════════════════════════════════
        //   GetResultAsync
        // ════════════════════════════════════════════════════════════════

        [Test]
        public async Task GetResultAsync_ResultsNotYetPublished_ThrowsResultsNotPublishedException()
        {
            var attempt = TestDataBuilder.BuildSubmittedAttempt(1, 1);
            attempt.Exam = TestDataBuilder.BuildPublishedExam(); // still Published, not ResultsPublished
            _attemptRepoMock.Setup(r => r.GetByIdWithAnswersAndExamAsync(1)).ReturnsAsync(attempt);

            var act = async () => await _sut.GetResultAsync(1, 1);
            await act.Should().ThrowAsync<ResultsNotPublishedException>();
        }

        [Test]
        public async Task GetResultAsync_ResultsPublished_ReturnsAttempt()
        {
            var attempt = TestDataBuilder.BuildSubmittedAttempt(1, 1);
            var exam = TestDataBuilder.BuildPublishedExam();
            exam.ExamStatus = ExamStatus.ResultsPublished;
            attempt.Exam = exam;
            _attemptRepoMock.Setup(r => r.GetByIdWithAnswersAndExamAsync(1)).ReturnsAsync(attempt);

            var result = await _sut.GetResultAsync(1, 1);

            result.AttemptId.Should().Be(1);
        }

        // ════════════════════════════════════════════════════════════════
        //   GetByUserAsync
        // ════════════════════════════════════════════════════════════════

        [Test]
        public async Task GetByUserAsync_ReturnsAttemptsForUser()
        {
            var attempts = new List<Attempt>
            {
                TestDataBuilder.BuildSubmittedAttempt(1, 1),
                TestDataBuilder.BuildSubmittedAttempt(2, 1),
            };
            _attemptRepoMock.Setup(r => r.GetByUserAsync(1)).ReturnsAsync(attempts);

            var result = await _sut.GetByUserAsync(1);

            result.Should().HaveCount(2);
        }

        // ════════════════════════════════════════════════════════════════
        //   SaveAnswerAsync
        // ════════════════════════════════════════════════════════════════

        [Test]
        public async Task SaveAnswerAsync_AlreadySubmitted_ThrowsAttemptAlreadySubmittedException()
        {
            var attempt = TestDataBuilder.BuildSubmittedAttempt(1, 1);
            attempt.Exam = TestDataBuilder.BuildPublishedExam();
            _attemptRepoMock.Setup(r => r.GetByIdWithAnswersAndExamAsync(1)).ReturnsAsync(attempt);

            var act = async () => await _sut.SaveAnswerAsync(1, 1, 2, userId: 1);
            await act.Should().ThrowAsync<AttemptAlreadySubmittedException>();
        }
    }
}
