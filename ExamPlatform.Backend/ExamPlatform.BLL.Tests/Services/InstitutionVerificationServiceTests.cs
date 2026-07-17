// InstitutionVerificationServiceTests — submit eligibility (name required,
// Professional-tier only, no duplicate pending/verified, resubmission after
// rejection updates the same row), admin approve/reject (happy path,
// already-processed guard, not-found guard), and notifications.

using ExamPlatform.BLL.Exceptions;
using ExamPlatform.BLL.Interfaces;
using ExamPlatform.BLL.Models;
using ExamPlatform.BLL.Services;
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
    public class InstitutionVerificationServiceTests
    {
        private Mock<IInstitutionRepository> _institutionRepoMock = null!;
        private Mock<IExaminerSubscriptionService> _subscriptionServiceMock = null!;
        private Mock<INotificationRepository> _notifRepoMock = null!;
        private Mock<ILogger<InstitutionVerificationService>> _loggerMock = null!;
        private InstitutionVerificationService _sut = null!;

        [SetUp]
        public void SetUp()
        {
            _institutionRepoMock = new Mock<IInstitutionRepository>();
            _subscriptionServiceMock = new Mock<IExaminerSubscriptionService>();
            _notifRepoMock = new Mock<INotificationRepository>();
            _loggerMock = new Mock<ILogger<InstitutionVerificationService>>();

            _subscriptionServiceMock
                .Setup(s => s.CanCreateExamAsync(It.IsAny<int>(), It.IsAny<DateTime?>()))
                .ReturnsAsync(new ExamCreationEligibility(
                    true, ExaminerSubscriptionStatus.ProfessionalActive, 0, null, 0, null, true));

            _institutionRepoMock.Setup(r => r.AddAsync(It.IsAny<Institution>())).ReturnsAsync((Institution i) => i);
            _institutionRepoMock.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);
            _notifRepoMock.Setup(r => r.AddAsync(It.IsAny<Notification>())).ReturnsAsync((Notification n) => n);
            _notifRepoMock.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);

            _sut = new InstitutionVerificationService(
                _institutionRepoMock.Object, _subscriptionServiceMock.Object,
                _notifRepoMock.Object, _loggerMock.Object);
        }

        // ════════════════════════════════════════════════════════════════
        //   SubmitAsync
        // ════════════════════════════════════════════════════════════════

        [Test]
        public async Task SubmitAsync_EmptyName_ThrowsValidationException()
        {
            var act = async () => await _sut.SubmitAsync(10, "  ", null);
            await act.Should().ThrowAsync<ValidationException>();
        }

        [Test]
        public async Task SubmitAsync_NotProfessionalTier_ThrowsValidationException()
        {
            _subscriptionServiceMock
                .Setup(s => s.CanCreateExamAsync(10, It.IsAny<DateTime?>()))
                .ReturnsAsync(new ExamCreationEligibility(
                    true, ExaminerSubscriptionStatus.FreeForever, 0, null, 2, null, false));

            var act = async () => await _sut.SubmitAsync(10, "Acme Institute", null);
            await act.Should().ThrowAsync<ValidationException>();
        }

        [Test]
        public async Task SubmitAsync_NoExistingRequest_CreatesPendingRequest()
        {
            _institutionRepoMock.Setup(r => r.GetByOwnerUserIdAsync(10)).ReturnsAsync((Institution?)null);

            var result = await _sut.SubmitAsync(10, "Acme Institute", "https://cdn/logo.png");

            result.VerificationStatus.Should().Be(InstitutionVerificationStatus.Pending);
            result.OwnerUserId.Should().Be(10);
            _institutionRepoMock.Verify(r => r.AddAsync(It.IsAny<Institution>()), Times.Once);
        }

        [Test]
        public async Task SubmitAsync_AlreadyPending_ThrowsValidationException()
        {
            _institutionRepoMock.Setup(r => r.GetByOwnerUserIdAsync(10)).ReturnsAsync(
                new Institution { VerificationStatus = InstitutionVerificationStatus.Pending });

            var act = async () => await _sut.SubmitAsync(10, "Acme Institute", null);
            await act.Should().ThrowAsync<ValidationException>();
        }

        [Test]
        public async Task SubmitAsync_AlreadyVerified_ThrowsValidationException()
        {
            _institutionRepoMock.Setup(r => r.GetByOwnerUserIdAsync(10)).ReturnsAsync(
                new Institution { VerificationStatus = InstitutionVerificationStatus.Verified });

            var act = async () => await _sut.SubmitAsync(10, "Acme Institute", null);
            await act.Should().ThrowAsync<ValidationException>();
        }

        [Test]
        public async Task SubmitAsync_PreviouslyRejected_UpdatesSameRowAndResetsToPending()
        {
            var existing = new Institution
            {
                InstitutionId = 5, OwnerUserId = 10, Name = "Old Name",
                VerificationStatus = InstitutionVerificationStatus.Rejected,
                VerifiedAt = DateTime.UtcNow.AddDays(-1), VerifiedByAdminUserId = 1
            };
            _institutionRepoMock.Setup(r => r.GetByOwnerUserIdAsync(10)).ReturnsAsync(existing);

            var result = await _sut.SubmitAsync(10, "New Name", null);

            result.InstitutionId.Should().Be(5); // same row
            result.Name.Should().Be("New Name");
            result.VerificationStatus.Should().Be(InstitutionVerificationStatus.Pending);
            result.VerifiedAt.Should().BeNull();
            result.VerifiedByAdminUserId.Should().BeNull();
            _institutionRepoMock.Verify(r => r.AddAsync(It.IsAny<Institution>()), Times.Never);
        }

        // ════════════════════════════════════════════════════════════════
        //   ApproveAsync / RejectAsync
        // ════════════════════════════════════════════════════════════════

        [Test]
        public async Task ApproveAsync_NotFound_ThrowsInstitutionNotFoundException()
        {
            _institutionRepoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Institution?)null);

            var act = async () => await _sut.ApproveAsync(99, 1);
            await act.Should().ThrowAsync<InstitutionNotFoundException>();
        }

        [Test]
        public async Task ApproveAsync_AlreadyProcessed_ThrowsValidationException()
        {
            _institutionRepoMock.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(
                new Institution { InstitutionId = 5, VerificationStatus = InstitutionVerificationStatus.Verified });

            var act = async () => await _sut.ApproveAsync(5, 1);
            await act.Should().ThrowAsync<ValidationException>();
        }

        [Test]
        public async Task ApproveAsync_Pending_VerifiesAndNotifies()
        {
            var institution = new Institution
            {
                InstitutionId = 5, OwnerUserId = 10, Name = "Acme",
                VerificationStatus = InstitutionVerificationStatus.Pending
            };
            _institutionRepoMock.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(institution);

            var result = await _sut.ApproveAsync(5, adminUserId: 1);

            result.VerificationStatus.Should().Be(InstitutionVerificationStatus.Verified);
            result.VerifiedByAdminUserId.Should().Be(1);
            _notifRepoMock.Verify(r => r.AddAsync(It.Is<Notification>(
                n => n.UserId == 10 && n.NotifType == NotificationType.InstitutionVerified)), Times.Once);
        }

        [Test]
        public async Task RejectAsync_Pending_RejectsAndNotifies()
        {
            var institution = new Institution
            {
                InstitutionId = 5, OwnerUserId = 10, Name = "Acme",
                VerificationStatus = InstitutionVerificationStatus.Pending
            };
            _institutionRepoMock.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(institution);

            var result = await _sut.RejectAsync(5, adminUserId: 1);

            result.VerificationStatus.Should().Be(InstitutionVerificationStatus.Rejected);
            _notifRepoMock.Verify(r => r.AddAsync(It.Is<Notification>(
                n => n.UserId == 10 && n.NotifType == NotificationType.InstitutionRejected)), Times.Once);
        }

        [Test]
        public async Task RejectAsync_NotFound_ThrowsInstitutionNotFoundException()
        {
            _institutionRepoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Institution?)null);

            var act = async () => await _sut.RejectAsync(99, 1);
            await act.Should().ThrowAsync<InstitutionNotFoundException>();
        }

        // ════════════════════════════════════════════════════════════════
        //   GetMineAsync / GetPendingAsync
        // ════════════════════════════════════════════════════════════════

        [Test]
        public async Task GetMineAsync_DelegatesToRepository()
        {
            var institution = new Institution { InstitutionId = 1, OwnerUserId = 10 };
            _institutionRepoMock.Setup(r => r.GetByOwnerUserIdAsync(10)).ReturnsAsync(institution);

            (await _sut.GetMineAsync(10)).Should().BeSameAs(institution);
        }

        [Test]
        public async Task GetPendingAsync_DelegatesToRepositoryWithPendingStatus()
        {
            var pending = new List<Institution> { new() { InstitutionId = 1, VerificationStatus = InstitutionVerificationStatus.Pending } };
            _institutionRepoMock.Setup(r => r.GetByStatusAsync(InstitutionVerificationStatus.Pending)).ReturnsAsync(pending);

            (await _sut.GetPendingAsync()).Should().BeEquivalentTo(pending);
        }
    }
}
