// AttemptServiceTests.cs — covers StartOrResume, GetById, SaveAnswer, Submit, GetResult.

using ExamPlatform.BLL.Exceptions;
using ExamPlatform.BLL.Services;
using ExamPlatform.BLL.Tests.Helpers;
using ExamPlatform.DAL.Interfaces;
using ExamPlatform.Domain.Entities;
using ExamPlatform.Domain.Enums;
using ExamPlatform.BLL.Interfaces;
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
        private Mock<IBackgroundTaskQueue>      _taskQueueMock   = null!;
        private AttemptService _sut = null!;

        [SetUp]
        public void SetUp()
        {
            _attemptRepoMock = new Mock<IAttemptRepository>();
            _answerRepoMock  = new Mock<IAttemptAnswerRepository>();
            _examRepoMock    = new Mock<IExamRepository>();
            _notifRepoMock   = new Mock<INotificationRepository>();
            _loggerMock      = new Mock<ILogger<AttemptService>>();
            _taskQueueMock   = new Mock<IBackgroundTaskQueue>();

            _sut = new AttemptService(
                _attemptRepoMock.Object,
                _answerRepoMock.Object,
                _examRepoMock.Object,
                _notifRepoMock.Object,
                _loggerMock.Object,
                _taskQueueMock.Object);
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
        public async Task GetResultAsync_SubmittedButNotYetPublished_ReturnsAttempt()
        {
            // Business rule REVISED (product decision, 2026-07): the score is visible
            // immediately after submission — MCQ scoring is automatic, so there is nothing
            // to wait for. The API layer withholds the per-question breakdown (correct
            // answers) and the topic chart until results are published; the service no
            // longer blocks the whole result.
            var attempt = TestDataBuilder.BuildSubmittedAttempt(1, 1);
            attempt.Exam = TestDataBuilder.BuildPublishedExam(); // still Published, not ResultsPublished
            _attemptRepoMock.Setup(r => r.GetByIdWithAnswersAndExamAsync(1)).ReturnsAsync(attempt);

            var result = await _sut.GetResultAsync(1, 1);

            result.AttemptId.Should().Be(1);
            result.Score.Should().NotBeNull("the submitted score is available right away");
        }

        [Test]
        public async Task GetResultAsync_AttemptStillInProgress_Throws()
        {
            // No score exists yet, and serving questions through the result endpoint
            // would bypass the attempt flow.
            var attempt = TestDataBuilder.BuildInProgressAttempt(1, 1);
            attempt.Exam = TestDataBuilder.BuildPublishedExam();
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

        [Test]
        public async Task GetResultAsync_AttemptNotFoundOrNotOwned_ThrowsAttemptNotFoundException()
        {
            _attemptRepoMock.Setup(r => r.GetByIdWithAnswersAndExamAsync(999))
                            .ReturnsAsync((Attempt?)null);

            var act = async () => await _sut.GetResultAsync(999, userId: 1);
            await act.Should().ThrowAsync<AttemptNotFoundException>();
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

        // ════════════════════════════════════════════════════════════════
        //   Helpers for evaluation tests
        // ════════════════════════════════════════════════════════════════

        // Builds an InProgress attempt whose exam has 3 questions worth 10 marks each:
        //   Q1 answered CORRECTLY, Q2 answered WRONG, Q3 skipped (no answer row).
        // Correct option ids follow TestDataBuilder.BuildFourOptions: qId*10+1 is correct.
        private static Attempt BuildLoadedAttempt(
            int minutesElapsed = 10, decimal passingMarks = 30m)
        {
            var exam = TestDataBuilder.BuildPublishedExam(1, examinerId: 10,
                totalMarks: 30m, passingMarks: passingMarks);
            exam.ExamQuestions = new List<ExamQuestion>();
            for (int qid = 1; qid <= 3; qid++)
            {
                exam.ExamQuestions.Add(new ExamQuestion
                {
                    ExamId = 1,
                    QuestionId = qid,
                    QuestionOrder = qid,
                    Question = TestDataBuilder.BuildQuestion(qid, marks: 10m)
                });
            }

            var attempt = TestDataBuilder.BuildInProgressAttempt(1, userId: 1, examId: 1);
            attempt.StartedAt = DateTime.UtcNow.AddMinutes(-minutesElapsed);
            attempt.Exam = exam;
            attempt.User = TestDataBuilder.BuildCandidate(1);
            attempt.AttemptAnswers = new List<AttemptAnswer>
            {
                new() { AnswerId = 1, AttemptId = 1, QuestionId = 1, SelectedOptionId = 11 }, // correct
                new() { AnswerId = 2, AttemptId = 1, QuestionId = 2, SelectedOptionId = 22 }, // wrong
                // Q3 skipped — no row.
            };
            return attempt;
        }

        // ════════════════════════════════════════════════════════════════
        //   SubmitAsync — EVALUATION (happy path)
        // ════════════════════════════════════════════════════════════════

        [Test]
        public async Task SubmitAsync_ValidAttempt_ScoresCorrectWrongAndSkippedQuestions()
        {
            var attempt = BuildLoadedAttempt(passingMarks: 30m);
            _attemptRepoMock.Setup(r => r.GetByIdWithAnswersAndExamAsync(1)).ReturnsAsync(attempt);
            _attemptRepoMock.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);

            var result = await _sut.SubmitAsync(1, userId: 1);

            // Only Q1 was correct: 10 of 30 marks; passing needs 30 → failed.
            result.Score.Should().Be(10m);
            result.IsPassed.Should().BeFalse();
            result.AttemptStatus.Should().Be(AttemptStatus.Submitted);
            result.SubmittedAt.Should().NotBeNull();

            // Both answered rows were evaluated and updated.
            var q1 = result.AttemptAnswers.First(a => a.QuestionId == 1);
            var q2 = result.AttemptAnswers.First(a => a.QuestionId == 2);
            q1.IsCorrect.Should().BeTrue();  q1.MarksAwarded.Should().Be(10m);
            q2.IsCorrect.Should().BeFalse(); q2.MarksAwarded.Should().Be(0m);
        }

        [Test]
        public async Task SubmitAsync_ScoreMeetsPassingMarks_MarksAttemptPassed()
        {
            var attempt = BuildLoadedAttempt(passingMarks: 10m); // Q1 alone is enough
            _attemptRepoMock.Setup(r => r.GetByIdWithAnswersAndExamAsync(1)).ReturnsAsync(attempt);
            _attemptRepoMock.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);

            var result = await _sut.SubmitAsync(1, userId: 1);

            result.IsPassed.Should().BeTrue();
        }

        // ════════════════════════════════════════════════════════════════
        //   GetByIdAsync — remaining branches
        // ════════════════════════════════════════════════════════════════

        [Test]
        public async Task GetByIdAsync_AttemptNotFound_ThrowsAttemptNotFoundException()
        {
            _attemptRepoMock.Setup(r => r.GetByIdWithAnswersAndExamAsync(999))
                            .ReturnsAsync((Attempt?)null);

            var act = async () => await _sut.GetByIdAsync(999, userId: 1);
            await act.Should().ThrowAsync<AttemptNotFoundException>();
        }

        [Test]
        public async Task GetByIdAsync_SubmittedAttempt_ReturnsWithoutTimerCheck()
        {
            var attempt = TestDataBuilder.BuildSubmittedAttempt(1, userId: 1);
            attempt.Exam = TestDataBuilder.BuildPublishedExam();
            _attemptRepoMock.Setup(r => r.GetByIdWithAnswersAndExamAsync(1)).ReturnsAsync(attempt);

            var result = await _sut.GetByIdAsync(1, userId: 1);

            result.AttemptStatus.Should().Be(AttemptStatus.Submitted);
        }

        [Test]
        public async Task GetByIdAsync_TimerExpired_AutoSubmitsAndNotifiesExaminer()
        {
            // Exam duration is 60 minutes; 120 minutes have elapsed → time is up.
            var attempt = BuildLoadedAttempt(minutesElapsed: 120);
            _attemptRepoMock.Setup(r => r.GetByIdWithAnswersAndExamAsync(1)).ReturnsAsync(attempt);
            _attemptRepoMock.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);
            _notifRepoMock.Setup(r => r.AddAsync(It.IsAny<Notification>()))
                          .ReturnsAsync((Notification n) => n);
            _notifRepoMock.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);

            var result = await _sut.GetByIdAsync(1, userId: 1);

            result.AttemptStatus.Should().Be(AttemptStatus.AutoSubmitted);
            // Examiner gets an AttemptAutoSubmitted alert.
            _notifRepoMock.Verify(r => r.AddAsync(
                It.Is<Notification>(n => n.UserId == 10 &&
                                         n.NotifType == NotificationType.AttemptAutoSubmitted)),
                Times.Once);
        }

        [Test]
        public async Task GetByIdAsync_TimerExpired_NotificationFailureDoesNotBreakAutoSubmit()
        {
            var attempt = BuildLoadedAttempt(minutesElapsed: 120);
            _attemptRepoMock.Setup(r => r.GetByIdWithAnswersAndExamAsync(1)).ReturnsAsync(attempt);
            _attemptRepoMock.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);
            _notifRepoMock.Setup(r => r.AddAsync(It.IsAny<Notification>()))
                          .ThrowsAsync(new InvalidOperationException("notif table down"));

            var result = await _sut.GetByIdAsync(1, userId: 1);

            // NotifyExaminerAsync swallows the failure — auto-submit still completed.
            result.AttemptStatus.Should().Be(AttemptStatus.AutoSubmitted);
        }

        // ════════════════════════════════════════════════════════════════
        //   SaveAnswerAsync — remaining branches
        // ════════════════════════════════════════════════════════════════

        [Test]
        public async Task SaveAnswerAsync_ValidAnswer_UpsertsAnswerRow()
        {
            var attempt = BuildLoadedAttempt();
            _attemptRepoMock.Setup(r => r.GetByIdWithAnswersAndExamAsync(1)).ReturnsAsync(attempt);
            _answerRepoMock.Setup(r => r.UpsertAsync(1, 3, 31)).Returns(Task.CompletedTask);

            await _sut.SaveAnswerAsync(1, questionId: 3, selectedOptionId: 31, userId: 1);

            _answerRepoMock.Verify(r => r.UpsertAsync(1, 3, 31), Times.Once);
        }

        [Test]
        public async Task SaveAnswerAsync_AttemptNotFound_ThrowsAttemptNotFoundException()
        {
            _attemptRepoMock.Setup(r => r.GetByIdWithAnswersAndExamAsync(999))
                            .ReturnsAsync((Attempt?)null);

            var act = async () => await _sut.SaveAnswerAsync(999, 1, 11, userId: 1);
            await act.Should().ThrowAsync<AttemptNotFoundException>();
        }

        [Test]
        public async Task SaveAnswerAsync_QuestionNotInExam_ThrowsValidationException()
        {
            var attempt = BuildLoadedAttempt();
            _attemptRepoMock.Setup(r => r.GetByIdWithAnswersAndExamAsync(1)).ReturnsAsync(attempt);

            // Question 77 is not part of exam 1.
            var act = async () => await _sut.SaveAnswerAsync(1, questionId: 77, selectedOptionId: 11, userId: 1);
            await act.Should().ThrowAsync<ValidationException>()
                     .WithMessage("*not part of exam*");
        }

        [Test]
        public async Task SaveAnswerAsync_OptionFromAnotherQuestion_ThrowsValidationException()
        {
            var attempt = BuildLoadedAttempt();
            _attemptRepoMock.Setup(r => r.GetByIdWithAnswersAndExamAsync(1)).ReturnsAsync(attempt);

            // Option 21 belongs to question 2, not question 1 (the score=0 bug guard).
            var act = async () => await _sut.SaveAnswerAsync(1, questionId: 1, selectedOptionId: 21, userId: 1);
            await act.Should().ThrowAsync<ValidationException>()
                     .WithMessage("*does not belong to question*");
        }

        [Test]
        public async Task SaveAnswerAsync_TimerExpired_AutoSubmitsWithoutSaving()
        {
            var attempt = BuildLoadedAttempt(minutesElapsed: 120);
            _attemptRepoMock.Setup(r => r.GetByIdWithAnswersAndExamAsync(1)).ReturnsAsync(attempt);
            _attemptRepoMock.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);
            _notifRepoMock.Setup(r => r.AddAsync(It.IsAny<Notification>()))
                          .ReturnsAsync((Notification n) => n);
            _notifRepoMock.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);

            await _sut.SaveAnswerAsync(1, questionId: 1, selectedOptionId: 11, userId: 1);

            attempt.AttemptStatus.Should().Be(AttemptStatus.AutoSubmitted);
            _answerRepoMock.Verify(
                r => r.UpsertAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int?>()), Times.Never);
        }

        // ════════════════════════════════════════════════════════════════
        //   StartOrResumeAsync — one attempt per candidate
        // ════════════════════════════════════════════════════════════════

        [Test]
        public async Task StartOrResumeAsync_AlreadyCompletedExam_ThrowsExamNotOpenForAttemptException()
        {
            var exam = TestDataBuilder.BuildPublishedExam(1, examinerId: 10);
            _examRepoMock.Setup(r => r.GetByIdWithQuestionsAndOptionsAsync(1)).ReturnsAsync(exam);
            _attemptRepoMock.Setup(r => r.GetInProgressAsync(1, 1)).ReturnsAsync((Attempt?)null);
            // The candidate submitted this exam before — no second attempt.
            _attemptRepoMock.Setup(r => r.HasCompletedAttemptAsync(1, 1)).ReturnsAsync(true);

            var act = async () => await _sut.StartOrResumeAsync(userId: 1, examId: 1);

            await act.Should().ThrowAsync<ExamNotOpenForAttemptException>()
                     .WithMessage("*already completed*");
            _attemptRepoMock.Verify(r => r.AddAsync(It.IsAny<Attempt>()), Times.Never);
        }

        [Test]
        public async Task StartOrResumeAsync_StartJustBeforeWindowCloses_GetsFullDuration()
        {
            // Window closes in 1 minute — a "late" start is still allowed and the
            // candidate receives the exam's full duration (server timer handles it).
            var exam = TestDataBuilder.BuildPublishedExam(1, examinerId: 10);
            exam.EndTime = DateTime.UtcNow.AddMinutes(1);
            _examRepoMock.Setup(r => r.GetByIdWithQuestionsAndOptionsAsync(1)).ReturnsAsync(exam);
            _attemptRepoMock.Setup(r => r.GetInProgressAsync(1, 1)).ReturnsAsync((Attempt?)null);
            _attemptRepoMock.Setup(r => r.HasCompletedAttemptAsync(1, 1)).ReturnsAsync(false);
            _attemptRepoMock.Setup(r => r.AddAsync(It.IsAny<Attempt>()))
                            .ReturnsAsync((Attempt a) => a);
            _attemptRepoMock.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);

            var result = await _sut.StartOrResumeAsync(userId: 1, examId: 1);

            result.AttemptStatus.Should().Be(AttemptStatus.InProgress);
        }

        // ════════════════════════════════════════════════════════════════
        //   StartOrResumeAsync — resume branches
        // ════════════════════════════════════════════════════════════════

        [Test]
        public async Task StartOrResumeAsync_ResumeWithExpiredTimer_AutoSubmits()
        {
            var attempt = BuildLoadedAttempt(minutesElapsed: 120);
            _examRepoMock.Setup(r => r.GetByIdWithQuestionsAndOptionsAsync(1))
                         .ReturnsAsync(attempt.Exam);
            _attemptRepoMock.Setup(r => r.GetInProgressAsync(1, 1)).ReturnsAsync(attempt);
            _attemptRepoMock.Setup(r => r.GetByIdWithAnswersAndExamAsync(1)).ReturnsAsync(attempt);
            _attemptRepoMock.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);
            _notifRepoMock.Setup(r => r.AddAsync(It.IsAny<Notification>()))
                          .ReturnsAsync((Notification n) => n);
            _notifRepoMock.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);

            var result = await _sut.StartOrResumeAsync(userId: 1, examId: 1);

            result.AttemptStatus.Should().Be(AttemptStatus.AutoSubmitted);
        }

        [Test]
        public async Task StartOrResumeAsync_ResumeActiveAttempt_NotifiesExaminerOfReconnect()
        {
            var attempt = BuildLoadedAttempt(minutesElapsed: 10); // 50 minutes remain
            _examRepoMock.Setup(r => r.GetByIdWithQuestionsAndOptionsAsync(1))
                         .ReturnsAsync(attempt.Exam);
            _attemptRepoMock.Setup(r => r.GetInProgressAsync(1, 1)).ReturnsAsync(attempt);
            _attemptRepoMock.Setup(r => r.GetByIdWithAnswersAndExamAsync(1)).ReturnsAsync(attempt);
            _notifRepoMock.Setup(r => r.AddAsync(It.IsAny<Notification>()))
                          .ReturnsAsync((Notification n) => n);
            _notifRepoMock.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);

            var result = await _sut.StartOrResumeAsync(userId: 1, examId: 1);

            result.AttemptStatus.Should().Be(AttemptStatus.InProgress);
            _notifRepoMock.Verify(r => r.AddAsync(
                It.Is<Notification>(n => n.NotifType == NotificationType.AttemptResumed)),
                Times.Once);
        }

        // ════════════════════════════════════════════════════════════════
        //   GetByUserPagedAsync
        // ════════════════════════════════════════════════════════════════

        [Test]
        public async Task GetByUserPagedAsync_ClampsPageAndPageSize()
        {
            _attemptRepoMock.Setup(r => r.GetByUserPagedAsync(1, 1, 100))
                            .ReturnsAsync((new List<Attempt>(), 0));

            var result = await _sut.GetByUserPagedAsync(1, page: -5, pageSize: 9999);

            result.Page.Should().Be(1);
            result.PageSize.Should().Be(100);
            _attemptRepoMock.Verify(r => r.GetByUserPagedAsync(1, 1, 100), Times.Once);
        }

        // ════════════════════════════════════════════════════════════════
        //   GetAllByExamForExaminerAsync — ownership rules
        // ════════════════════════════════════════════════════════════════

        [Test]
        public async Task GetAllByExamForExaminerAsync_ExamNotFound_ThrowsExamNotFoundException()
        {
            _examRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Exam?)null);

            var act = async () => await _sut.GetAllByExamForExaminerAsync(999, 10, isAdmin: false);
            await act.Should().ThrowAsync<ExamNotFoundException>();
        }

        [Test]
        public async Task GetAllByExamForExaminerAsync_NotOwner_ThrowsExamNotFoundException()
        {
            var exam = TestDataBuilder.BuildPublishedExam(1, examinerId: 10);
            _examRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(exam);

            // Examiner 20 does not own exam 1 — same 404, no info leak.
            var act = async () => await _sut.GetAllByExamForExaminerAsync(1, 20, isAdmin: false);
            await act.Should().ThrowAsync<ExamNotFoundException>();
        }

        [Test]
        public async Task GetAllByExamForExaminerAsync_Owner_ReturnsAllAttempts()
        {
            var exam = TestDataBuilder.BuildPublishedExam(1, examinerId: 10);
            _examRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(exam);
            _attemptRepoMock.Setup(r => r.GetAllByExamIdAsync(1))
                            .ReturnsAsync(new List<Attempt>
                            {
                                TestDataBuilder.BuildSubmittedAttempt(1, 1, 1),
                                TestDataBuilder.BuildInProgressAttempt(2, 2, 1)
                            });

            var result = await _sut.GetAllByExamForExaminerAsync(1, 10, isAdmin: false);

            result.Should().HaveCount(2);
        }

        [Test]
        public async Task GetAllByExamForExaminerAsync_AdminBypassesOwnership()
        {
            var exam = TestDataBuilder.BuildPublishedExam(1, examinerId: 10);
            _examRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(exam);
            _attemptRepoMock.Setup(r => r.GetAllByExamIdAsync(1))
                            .ReturnsAsync(new List<Attempt>());

            // Requester 99 is not the owner but IS admin.
            var act = async () => await _sut.GetAllByExamForExaminerAsync(1, 99, isAdmin: true);
            await act.Should().NotThrowAsync();
        }

        // ════════════════════════════════════════════════════════════════
        //   RecordTabSwitchAsync — proctoring / integrity
        // ════════════════════════════════════════════════════════════════

        // Builds an InProgress attempt with Exam + User wired up so the service
        // can read the exam owner (for notifications) and the candidate name.
        private static Attempt BuildAttemptForProctoring(int startCount)
        {
            var attempt = TestDataBuilder.BuildInProgressAttempt(attemptId: 1, userId: 1, examId: 1);
            attempt.TabSwitchCount = startCount;
            attempt.Exam = TestDataBuilder.BuildPublishedExam(1, examinerId: 10);
            attempt.User = TestDataBuilder.BuildCandidate(1);
            return attempt;
        }

        [Test]
        public async Task RecordTabSwitchAsync_AttemptNotOwned_ThrowsAttemptNotFoundException()
        {
            var attempt = BuildAttemptForProctoring(0);
            _attemptRepoMock.Setup(r => r.GetByIdWithAnswersAndExamAsync(1)).ReturnsAsync(attempt);

            // userId 999 does not own attempt (UserId == 1).
            var act = async () => await _sut.RecordTabSwitchAsync(1, userId: 999);
            await act.Should().ThrowAsync<AttemptNotFoundException>();
        }

        [Test]
        public async Task RecordTabSwitchAsync_AttemptAlreadySubmitted_ReturnsCountWithoutSaving()
        {
            var attempt = TestDataBuilder.BuildSubmittedAttempt(attemptId: 1, userId: 1);
            attempt.TabSwitchCount = 2;
            _attemptRepoMock.Setup(r => r.GetByIdWithAnswersAndExamAsync(1)).ReturnsAsync(attempt);

            var result = await _sut.RecordTabSwitchAsync(1, userId: 1);

            result.Should().Be(2); // unchanged
            _attemptRepoMock.Verify(r => r.SaveChangesAsync(), Times.Never);
        }

        [Test]
        public async Task RecordTabSwitchAsync_InProgress_IncrementsAndSaves()
        {
            var attempt = BuildAttemptForProctoring(0);
            _attemptRepoMock.Setup(r => r.GetByIdWithAnswersAndExamAsync(1)).ReturnsAsync(attempt);

            var result = await _sut.RecordTabSwitchAsync(1, userId: 1);

            result.Should().Be(1);
            attempt.TabSwitchCount.Should().Be(1);
            _attemptRepoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Test]
        public async Task RecordTabSwitchAsync_BelowThreshold_DoesNotNotifyExaminer()
        {
            var attempt = BuildAttemptForProctoring(0); // 0 → 1, threshold is 3
            _attemptRepoMock.Setup(r => r.GetByIdWithAnswersAndExamAsync(1)).ReturnsAsync(attempt);

            await _sut.RecordTabSwitchAsync(1, userId: 1);

            _notifRepoMock.Verify(r => r.AddAsync(It.IsAny<Notification>()), Times.Never);
        }

        [Test]
        public async Task RecordTabSwitchAsync_CrossingThreshold_NotifiesExaminerOnce()
        {
            var attempt = BuildAttemptForProctoring(2); // 2 → 3 == threshold
            _attemptRepoMock.Setup(r => r.GetByIdWithAnswersAndExamAsync(1)).ReturnsAsync(attempt);

            var result = await _sut.RecordTabSwitchAsync(1, userId: 1);

            result.Should().Be(3);
            // Examiner (exam owner, id 10) gets exactly one IntegrityFlag notification.
            _notifRepoMock.Verify(r => r.AddAsync(It.Is<Notification>(n =>
                n.UserId == 10 && n.NotifType == NotificationType.IntegrityFlag)), Times.Once);
        }

        [Test]
        public async Task RecordTabSwitchAsync_AlreadyPastThreshold_DoesNotNotifyAgain()
        {
            var attempt = BuildAttemptForProctoring(5); // 5 → 6, past threshold already
            _attemptRepoMock.Setup(r => r.GetByIdWithAnswersAndExamAsync(1)).ReturnsAsync(attempt);

            await _sut.RecordTabSwitchAsync(1, userId: 1);

            // '==' threshold check means no second notification after the first crossing.
            _notifRepoMock.Verify(r => r.AddAsync(It.IsAny<Notification>()), Times.Never);
        }
    }
}
