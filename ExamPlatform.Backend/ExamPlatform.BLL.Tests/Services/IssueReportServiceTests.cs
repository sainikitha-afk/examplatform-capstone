// IssueReportServiceTests.cs — covers the candidate "flag / report a problem"
// workflow: raising reports (validation, examiner notification, resilience) and
// examiner triage (ownership, status transitions, candidate notification).

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
    public class IssueReportServiceTests
    {
        private Mock<IIssueReportRepository>  _reportRepoMock = null!;
        private Mock<IExamRepository>         _examRepoMock   = null!;
        private Mock<IUserRepository>         _userRepoMock   = null!;
        private Mock<INotificationRepository> _notifRepoMock  = null!;
        private Mock<ILogger<IssueReportService>> _loggerMock = null!;
        private IssueReportService _sut = null!;

        [SetUp]
        public void SetUp()
        {
            _reportRepoMock = new Mock<IIssueReportRepository>();
            _examRepoMock   = new Mock<IExamRepository>();
            _userRepoMock   = new Mock<IUserRepository>();
            _notifRepoMock  = new Mock<INotificationRepository>();
            _loggerMock     = new Mock<ILogger<IssueReportService>>();

            _sut = new IssueReportService(
                _reportRepoMock.Object,
                _examRepoMock.Object,
                _userRepoMock.Object,
                _notifRepoMock.Object,
                _loggerMock.Object);
        }

        private static IssueReport BuildReport(int id = 1, int examinerId = 10,
            IssueStatus status = IssueStatus.Open) => new()
        {
            IssueReportId = id,
            CandidateId = 1,
            ExamId = 1,
            Message = "Something is wrong.",
            Status = status,
            CreatedAt = DateTime.UtcNow,
            Exam = TestDataBuilder.BuildPublishedExam(1, examinerId: examinerId)
        };

        // ════════════════════════════════════════════════════════════════
        //   RaiseAsync
        // ════════════════════════════════════════════════════════════════

        [Test]
        public async Task RaiseAsync_BlankMessage_ThrowsValidationException()
        {
            var act = async () => await _sut.RaiseAsync(1, 1, null, null, "   ");
            await act.Should().ThrowAsync<ValidationException>();
        }

        [Test]
        public async Task RaiseAsync_ExamNotFound_ThrowsExamNotFoundException()
        {
            _examRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((Exam?)null);

            var act = async () => await _sut.RaiseAsync(1, 1, null, null, "Valid message");
            await act.Should().ThrowAsync<ExamNotFoundException>();
        }

        [Test]
        public async Task RaiseAsync_Valid_SavesReportAndNotifiesExaminer()
        {
            _examRepoMock.Setup(r => r.GetByIdAsync(1))
                         .ReturnsAsync(TestDataBuilder.BuildPublishedExam(1, examinerId: 10));
            _userRepoMock.Setup(r => r.GetByIdAsync(1))
                         .ReturnsAsync(TestDataBuilder.BuildCandidate(1));

            var report = await _sut.RaiseAsync(1, 1, attemptId: 5, questionId: 7, "Two options are correct");

            report.Should().NotBeNull();
            report.Status.Should().Be(IssueStatus.Open);
            report.QuestionId.Should().Be(7);
            _reportRepoMock.Verify(r => r.AddAsync(It.IsAny<IssueReport>()), Times.Once);
            // Examiner (exam owner id 10) receives an IssueReported notification.
            _notifRepoMock.Verify(r => r.AddAsync(It.Is<Notification>(n =>
                n.UserId == 10 && n.NotifType == NotificationType.IssueReported)), Times.Once);
        }

        [Test]
        public async Task RaiseAsync_NotificationFails_StillReturnsSavedReport()
        {
            _examRepoMock.Setup(r => r.GetByIdAsync(1))
                         .ReturnsAsync(TestDataBuilder.BuildPublishedExam(1, examinerId: 10));
            _userRepoMock.Setup(r => r.GetByIdAsync(1))
                         .ReturnsAsync(TestDataBuilder.BuildCandidate(1));
            // Simulate the examiner notification blowing up — must be swallowed.
            _notifRepoMock.Setup(r => r.AddAsync(It.IsAny<Notification>()))
                          .ThrowsAsync(new Exception("db down"));

            var act = async () => await _sut.RaiseAsync(1, 1, null, null, "Valid message");

            await act.Should().NotThrowAsync();
            _reportRepoMock.Verify(r => r.AddAsync(It.IsAny<IssueReport>()), Times.Once);
        }

        // ════════════════════════════════════════════════════════════════
        //   UpdateStatusAsync
        // ════════════════════════════════════════════════════════════════

        [Test]
        public async Task UpdateStatusAsync_ReportNotFound_ThrowsIssueReportNotFoundException()
        {
            _reportRepoMock.Setup(r => r.GetByIdWithContextAsync(1)).ReturnsAsync((IssueReport?)null);

            var act = async () => await _sut.UpdateStatusAsync(1, 10, IssueStatus.Resolved, null);
            await act.Should().ThrowAsync<IssueReportNotFoundException>();
        }

        [Test]
        public async Task UpdateStatusAsync_ExaminerDoesNotOwnExam_ThrowsIssueReportNotFoundException()
        {
            // Report belongs to an exam owned by examiner 10; caller is examiner 20.
            _reportRepoMock.Setup(r => r.GetByIdWithContextAsync(1))
                           .ReturnsAsync(BuildReport(1, examinerId: 10));

            var act = async () => await _sut.UpdateStatusAsync(1, examinerId: 20, IssueStatus.Resolved, null);
            await act.Should().ThrowAsync<IssueReportNotFoundException>();
        }

        [Test]
        public async Task UpdateStatusAsync_Resolved_SetsResolvedAtAndNotifiesCandidate()
        {
            _reportRepoMock.Setup(r => r.GetByIdWithContextAsync(1))
                           .ReturnsAsync(BuildReport(1, examinerId: 10));

            var result = await _sut.UpdateStatusAsync(1, examinerId: 10, IssueStatus.Resolved, "Fixed the typo");

            result.Status.Should().Be(IssueStatus.Resolved);
            result.ResolvedAt.Should().NotBeNull();
            result.ResolutionNote.Should().Be("Fixed the typo");
            // Candidate (id 1) is told their report was resolved.
            _notifRepoMock.Verify(r => r.AddAsync(It.Is<Notification>(n =>
                n.UserId == 1 && n.NotifType == NotificationType.IssueResolved)), Times.Once);
        }

        [Test]
        public async Task UpdateStatusAsync_Reviewed_DoesNotNotifyCandidateAndLeavesResolvedAtNull()
        {
            _reportRepoMock.Setup(r => r.GetByIdWithContextAsync(1))
                           .ReturnsAsync(BuildReport(1, examinerId: 10));

            var result = await _sut.UpdateStatusAsync(1, examinerId: 10, IssueStatus.Reviewed, null);

            result.Status.Should().Be(IssueStatus.Reviewed);
            result.ResolvedAt.Should().BeNull();
            _notifRepoMock.Verify(r => r.AddAsync(It.IsAny<Notification>()), Times.Never);
        }

        // ════════════════════════════════════════════════════════════════
        //   GetForExaminerPagedAsync
        // ════════════════════════════════════════════════════════════════

        [Test]
        public async Task GetForExaminerPagedAsync_ClampsPageSizeAndPage()
        {
            _reportRepoMock.Setup(r => r.GetForExaminerPagedAsync(10, 1, 100, false))
                           .ReturnsAsync((new List<IssueReport>(), 0));

            var result = await _sut.GetForExaminerPagedAsync(10, page: 0, pageSize: 5000, openOnly: false);

            result.Page.Should().Be(1);        // page 0 → 1
            result.PageSize.Should().Be(100);  // 5000 → clamped to 100
        }
    }
}
