// ExaminerRequestServiceTests — covers the full examiner self-registration lifecycle:
//   SubmitAsync (happy path, duplicate email, short purpose)
//   ApproveAsync (happy path, already-processed guard)
//   RejectAsync (happy path, already-processed guard)
//   GetByIdAsync (not-found guard)

#pragma warning disable CA1848
using ExamPlatform.BLL.Exceptions;
using ExamPlatform.BLL.Interfaces;
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
    public class ExaminerRequestServiceTests
    {
        private Mock<IExaminerRequestRepository> _requestRepoMock = null!;
        private Mock<IUserRepository>            _userRepoMock    = null!;
        private Mock<INotificationRepository>    _notifRepoMock   = null!;
        private Mock<INotificationEmailService>  _emailSvcMock    = null!;
        private Mock<ILogger<ExaminerRequestService>> _loggerMock  = null!;
        private ExaminerRequestService _sut = null!;

        [SetUp]
        public void SetUp()
        {
            _requestRepoMock = new Mock<IExaminerRequestRepository>();
            _userRepoMock    = new Mock<IUserRepository>();
            _notifRepoMock   = new Mock<INotificationRepository>();
            _emailSvcMock    = new Mock<INotificationEmailService>();
            _loggerMock      = new Mock<ILogger<ExaminerRequestService>>();

            _sut = new ExaminerRequestService(
                _requestRepoMock.Object,
                _userRepoMock.Object,
                _notifRepoMock.Object,
                _emailSvcMock.Object,
                _loggerMock.Object);

            // Default: AddAsync returns the entity unchanged (sets it up for
            // the notification fan-out which calls _notifRepository.AddAsync too).
            _requestRepoMock
                .Setup(r => r.AddAsync(It.IsAny<ExaminerRequest>()))
                .ReturnsAsync((ExaminerRequest req) => req);
            _requestRepoMock
                .Setup(r => r.SaveChangesAsync())
                .ReturnsAsync(1);
            _notifRepoMock
                .Setup(r => r.AddAsync(It.IsAny<Notification>()))
                .ReturnsAsync((Notification n) => n);
            _userRepoMock
                .Setup(r => r.AddAsync(It.IsAny<User>()))
                .ReturnsAsync((User u) => u);
            _userRepoMock
                .Setup(r => r.SaveChangesAsync())
                .ReturnsAsync(1);
            _emailSvcMock
                .Setup(s => s.SendAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);
        }

        // ════════════════════════════════════════════════════════════════
        //   SubmitAsync
        // ════════════════════════════════════════════════════════════════

        [Test]
        public async Task SubmitAsync_ValidRequest_CreatesRequestAndNotifiesAdmins()
        {
            // ─── ARRANGE ──────────────────────────────────────────────
            // No active request for this email already exists.
            _requestRepoMock
                .Setup(r => r.HasActiveRequestAsync("riya@example.com"))
                .ReturnsAsync(false);

            // Two active admins will each receive a notification.
            var admin1 = TestDataBuilder.BuildAdmin(userId: 99);
            var admin2 = TestDataBuilder.BuildAdmin(userId: 100);
            admin2.Email = "admin2@test.com";
            _userRepoMock
                .Setup(r => r.GetAllByRoleAsync(UserRole.Admin))
                .ReturnsAsync(new List<User> { admin1, admin2 });

            // ─── ACT ──────────────────────────────────────────────────
            var result = await _sut.SubmitAsync(
                "Riya Sharma",
                "riya@example.com",
                "I teach Java at XYZ University and want to create assessments.");

            // ─── ASSERT ───────────────────────────────────────────────
            result.Status.Should().Be(ExaminerRequestStatus.Pending);
            result.Email.Should().Be("riya@example.com");

            // One Notification per admin.
            _notifRepoMock.Verify(r =>
                r.AddAsync(It.Is<Notification>(n =>
                    n.NotifType == NotificationType.ExaminerRequestReceived)),
                Times.Exactly(2));

            // Single SaveChanges flushes request + notifications.
            _requestRepoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Test]
        public async Task SubmitAsync_DuplicateActiveEmail_ThrowsDuplicateEmailException()
        {
            // Email already has a Pending/Approved request in the DB.
            _requestRepoMock
                .Setup(r => r.HasActiveRequestAsync("riya@example.com"))
                .ReturnsAsync(true);

            var act = async () =>
                await _sut.SubmitAsync(
                    "Riya Sharma", "riya@example.com",
                    "I teach Java at XYZ and want to create assessments.");

            await act.Should().ThrowAsync<DuplicateEmailException>();
        }

        [Test]
        public async Task SubmitAsync_ShortPurpose_ThrowsValidationException()
        {
            // Purpose must be at least 20 characters; "short" is only 5.
            _requestRepoMock
                .Setup(r => r.HasActiveRequestAsync(It.IsAny<string>()))
                .ReturnsAsync(false);

            var act = async () =>
                await _sut.SubmitAsync("Riya", "riya@example.com", "short");

            await act.Should().ThrowAsync<ValidationException>();
        }

        // ════════════════════════════════════════════════════════════════
        //   GetByIdAsync
        // ════════════════════════════════════════════════════════════════

        [Test]
        public async Task GetByIdAsync_NonExistent_ThrowsExaminerRequestNotFoundException()
        {
            _requestRepoMock
                .Setup(r => r.GetByIdAsync(999))
                .ReturnsAsync((ExaminerRequest?)null);

            var act = async () => await _sut.GetByIdAsync(999);
            await act.Should().ThrowAsync<ExaminerRequestNotFoundException>();
        }

        // ════════════════════════════════════════════════════════════════
        //   ApproveAsync
        // ════════════════════════════════════════════════════════════════

        [Test]
        public async Task ApproveAsync_PendingRequest_CreatesUserAndSendsEmail()
        {
            // ─── ARRANGE ──────────────────────────────────────────────
            var request = TestDataBuilder.BuildExaminerRequest(
                requestId: 1, email: "riya@example.com",
                status: ExaminerRequestStatus.Pending);

            _requestRepoMock
                .Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync(request);

            // Username "riya" is available on the first try.
            _userRepoMock
                .Setup(r => r.IsUsernameTakenAsync("riya"))
                .ReturnsAsync(false);

            // ─── ACT ──────────────────────────────────────────────────
            var result = await _sut.ApproveAsync(1);

            // ─── ASSERT ───────────────────────────────────────────────
            result.Request.Status.Should().Be(ExaminerRequestStatus.Approved);
            result.Request.ProcessedAt.Should().NotBeNull();
            result.CreatedUser.Role.Should().Be(UserRole.Examiner);
            result.CreatedUser.Username.Should().Be("riya");
            result.TemporaryPassword.Should().HaveLength(12);

            // Examiner User was persisted.
            _userRepoMock.Verify(r =>
                r.AddAsync(It.Is<User>(u => u.Role == UserRole.Examiner)), Times.Once);

            // Welcome notification was created for the new examiner.
            _notifRepoMock.Verify(r =>
                r.AddAsync(It.Is<Notification>(n =>
                    n.NotifType == NotificationType.ExaminerAccountApproved)), Times.Once);

            // Approval email was sent.
            _emailSvcMock.Verify(s =>
                s.SendAsync("riya@example.com", It.IsAny<string>(), It.IsAny<string>()),
                Times.Once);
        }

        [Test]
        public async Task ApproveAsync_AlreadyApproved_ThrowsExaminerRequestAlreadyProcessedException()
        {
            var request = TestDataBuilder.BuildExaminerRequest(
                requestId: 1, status: ExaminerRequestStatus.Approved);

            _requestRepoMock
                .Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync(request);

            var act = async () => await _sut.ApproveAsync(1);
            await act.Should().ThrowAsync<ExaminerRequestAlreadyProcessedException>();
        }

        [Test]
        public async Task ApproveAsync_UsernameCollision_AppendsSuffix()
        {
            // Base username "riya" is taken, "riya1" is available.
            var request = TestDataBuilder.BuildExaminerRequest(
                requestId: 1, email: "riya@example.com",
                status: ExaminerRequestStatus.Pending);

            _requestRepoMock
                .Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync(request);

            // "riya" is taken, "riya1" is free.
            _userRepoMock.Setup(r => r.IsUsernameTakenAsync("riya")).ReturnsAsync(true);
            _userRepoMock.Setup(r => r.IsUsernameTakenAsync("riya1")).ReturnsAsync(false);

            var result = await _sut.ApproveAsync(1);

            result.CreatedUser.Username.Should().Be("riya1");
        }

        // ════════════════════════════════════════════════════════════════
        //   RejectAsync
        // ════════════════════════════════════════════════════════════════

        [Test]
        public async Task RejectAsync_PendingRequest_SetsStatusAndSendsEmail()
        {
            // ─── ARRANGE ──────────────────────────────────────────────
            var request = TestDataBuilder.BuildExaminerRequest(
                requestId: 1, status: ExaminerRequestStatus.Pending);

            _requestRepoMock
                .Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync(request);

            // ─── ACT ──────────────────────────────────────────────────
            var result = await _sut.RejectAsync(1, adminNote: "Insufficient credentials.");

            // ─── ASSERT ───────────────────────────────────────────────
            result.Status.Should().Be(ExaminerRequestStatus.Rejected);
            result.AdminNote.Should().Be("Insufficient credentials.");
            result.ProcessedAt.Should().NotBeNull();

            _requestRepoMock.Verify(r =>
                r.Update(It.Is<ExaminerRequest>(req =>
                    req.Status == ExaminerRequestStatus.Rejected)), Times.Once);
            _requestRepoMock.Verify(r => r.SaveChangesAsync(), Times.Once);

            // Rejection email was sent.
            _emailSvcMock.Verify(s =>
                s.SendAsync(request.Email, It.IsAny<string>(), It.IsAny<string>()),
                Times.Once);
        }

        [Test]
        public async Task RejectAsync_AlreadyRejected_ThrowsExaminerRequestAlreadyProcessedException()
        {
            var request = TestDataBuilder.BuildExaminerRequest(
                requestId: 1, status: ExaminerRequestStatus.Rejected);

            _requestRepoMock
                .Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync(request);

            var act = async () => await _sut.RejectAsync(1, adminNote: null);
            await act.Should().ThrowAsync<ExaminerRequestAlreadyProcessedException>();
        }

        // ════════════════════════════════════════════════════════════════
        //   READ METHODS
        // ════════════════════════════════════════════════════════════════

        [Test]
        public async Task GetAllAsync_ReturnsAllRequests()
        {
            _requestRepoMock.Setup(r => r.GetAllAsync())
                            .ReturnsAsync(new List<ExaminerRequest>
                            {
                                TestDataBuilder.BuildExaminerRequest(1),
                                TestDataBuilder.BuildExaminerRequest(2, email: "b@x.com"),
                            });

            var result = await _sut.GetAllAsync();

            result.Should().HaveCount(2);
        }

        [Test]
        public async Task GetPendingAsync_ReturnsOnlyPendingRequests()
        {
            _requestRepoMock.Setup(r => r.GetPendingAsync())
                            .ReturnsAsync(new List<ExaminerRequest>
                            {
                                TestDataBuilder.BuildExaminerRequest(1)
                            });

            var result = await _sut.GetPendingAsync();

            result.Should().HaveCount(1);
        }

        // ════════════════════════════════════════════════════════════════
        //   SubmitAsync — remaining validation branches
        // ════════════════════════════════════════════════════════════════

        [Test]
        public async Task SubmitAsync_EmptyFullName_ThrowsValidationException()
        {
            var act = async () => await _sut.SubmitAsync(
                "", "riya@example.com", "A long enough purpose describing teaching experience.");
            await act.Should().ThrowAsync<ValidationException>()
                     .WithMessage("*Full name*");
        }

        [Test]
        public async Task SubmitAsync_InvalidEmail_ThrowsValidationException()
        {
            var act = async () => await _sut.SubmitAsync(
                "Riya Sharma", "not-an-email", "A long enough purpose describing teaching experience.");
            await act.Should().ThrowAsync<ValidationException>()
                     .WithMessage("*valid email*");
        }

        // ════════════════════════════════════════════════════════════════
        //   ApproveAsync — remaining branches
        // ════════════════════════════════════════════════════════════════

        [Test]
        public async Task ApproveAsync_EmailAlreadyHasUserAccount_ThrowsDuplicateEmailException()
        {
            var request = TestDataBuilder.BuildExaminerRequest(
                requestId: 1, email: "riya@example.com", status: ExaminerRequestStatus.Pending);
            _requestRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(request);

            // The applicant already registered as a Candidate with this email.
            _userRepoMock.Setup(r => r.IsEmailTakenAsync("riya@example.com")).ReturnsAsync(true);

            var act = async () => await _sut.ApproveAsync(1);
            await act.Should().ThrowAsync<DuplicateEmailException>();
        }

        [Test]
        public async Task ApproveAsync_EmailFailure_StillApprovesAndReturnsCredentials()
        {
            var request = TestDataBuilder.BuildExaminerRequest(
                requestId: 1, email: "riya@example.com", status: ExaminerRequestStatus.Pending);
            _requestRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(request);
            _userRepoMock.Setup(r => r.IsUsernameTakenAsync("riya")).ReturnsAsync(false);
            _emailSvcMock.Setup(s => s.SendAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()))
                         .ThrowsAsync(new InvalidOperationException("SMTP down"));

            var result = await _sut.ApproveAsync(1);

            // The account was created; credentials are still in the response.
            result.Request.Status.Should().Be(ExaminerRequestStatus.Approved);
            result.TemporaryPassword.Should().NotBeNullOrEmpty();
        }

        // ════════════════════════════════════════════════════════════════
        //   RejectAsync — remaining branches
        // ════════════════════════════════════════════════════════════════

        [Test]
        public async Task RejectAsync_EmailFailure_StillRejectsTheRequest()
        {
            var request = TestDataBuilder.BuildExaminerRequest(
                requestId: 1, status: ExaminerRequestStatus.Pending);
            _requestRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(request);
            _emailSvcMock.Setup(s => s.SendAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()))
                         .ThrowsAsync(new InvalidOperationException("SMTP down"));

            var result = await _sut.RejectAsync(1, adminNote: null);

            result.Status.Should().Be(ExaminerRequestStatus.Rejected);
        }
    }
}
