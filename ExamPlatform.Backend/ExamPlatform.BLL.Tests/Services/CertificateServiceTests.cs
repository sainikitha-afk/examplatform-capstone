// CertificateServiceTests.cs — covers GetAllAsync, GetByIdAsync, GetByUserAsync,
// GetByIdWithDetailsAsync, and RevokeAsync (including notification + idempotency).

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
    public class CertificateServiceTests
    {
        private Mock<ICertificateRepository>        _certRepoMock  = null!;
        private Mock<INotificationRepository>       _notifRepoMock = null!;
        // CertificateService now also injects INotificationEmailService so it can
        // send revocation emails. Mocked here so tests don't need SMTP.
        private Mock<INotificationEmailService>     _emailSvcMock  = null!;
        private Mock<ILogger<CertificateService>>   _loggerMock    = null!;
        private CertificateService _sut = null!;

        [SetUp]
        public void SetUp()
        {
            _certRepoMock  = new Mock<ICertificateRepository>();
            _notifRepoMock = new Mock<INotificationRepository>();
            _emailSvcMock  = new Mock<INotificationEmailService>();
            _loggerMock    = new Mock<ILogger<CertificateService>>();

            _sut = new CertificateService(
                _certRepoMock.Object,
                _notifRepoMock.Object,
                _emailSvcMock.Object,
                _loggerMock.Object);
        }

        // ════════════════════════════════════════════════════════════════
        //   GetAllAsync
        // ════════════════════════════════════════════════════════════════

        [Test]
        public async Task GetAllAsync_ReturnsAllCertificates()
        {
            var certs = new List<Certificate>
            {
                TestDataBuilder.BuildCertificate(1),
                TestDataBuilder.BuildCertificate(2),
            };
            _certRepoMock.Setup(r => r.GetAllWithUserAndExamAsync()).ReturnsAsync(certs);

            var result = await _sut.GetAllAsync();

            result.Should().HaveCount(2);
        }

        // ════════════════════════════════════════════════════════════════
        //   GetByIdAsync  (Admin — now uses GetByIdWithUserAndExamAsync)
        // ════════════════════════════════════════════════════════════════

        [Test]
        public async Task GetByIdAsync_ExistingCert_ReturnsCertificate()
        {
            // GetByIdAsync internally calls GetByIdWithUserAndExamAsync (not the
            // base GetByIdAsync) so that User + Exam navs are always populated.
            var cert = TestDataBuilder.BuildCertificate(1);
            _certRepoMock
                .Setup(r => r.GetByIdWithUserAndExamAsync(1))
                .ReturnsAsync(cert);

            var result = await _sut.GetByIdAsync(1);

            result.CertificateId.Should().Be(1);
        }

        [Test]
        public async Task GetByIdAsync_NonExistentCert_ThrowsCertificateNotFoundException()
        {
            _certRepoMock
                .Setup(r => r.GetByIdWithUserAndExamAsync(It.IsAny<int>()))
                .ReturnsAsync((Certificate?)null);

            var act = async () => await _sut.GetByIdAsync(999);
            await act.Should().ThrowAsync<CertificateNotFoundException>();
        }

        // ════════════════════════════════════════════════════════════════
        //   GetByUserAsync  (Candidate)
        // ════════════════════════════════════════════════════════════════

        [Test]
        public async Task GetByUserAsync_ReturnsCandidateOwnCertificates()
        {
            // ─── ARRANGE ──────────────────────────────────────────────
            // Candidate userId=5 has two certificates.
            var certs = new List<Certificate>
            {
                TestDataBuilder.BuildCertificate(1, userId: 5, examId: 7),
                TestDataBuilder.BuildCertificate(2, userId: 5, examId: 9),
            };
            _certRepoMock
                .Setup(r => r.GetByUserAsync(5))
                .ReturnsAsync(certs);

            // ─── ACT ──────────────────────────────────────────────────
            var result = await _sut.GetByUserAsync(5);

            // ─── ASSERT ───────────────────────────────────────────────
            result.Should().HaveCount(2);
            // Repository is called with the candidate's own userId — scoping
            // is enforced at the query level, not by a post-filter.
            _certRepoMock.Verify(r => r.GetByUserAsync(5), Times.Once);
        }

        // ════════════════════════════════════════════════════════════════
        //   GetByIdWithDetailsAsync  (Candidate or Admin)
        // ════════════════════════════════════════════════════════════════

        [Test]
        public async Task GetByIdWithDetailsAsync_AdminCanAccessAnyCert()
        {
            // ─── ARRANGE ──────────────────────────────────────────────
            // Certificate belongs to user 5; caller is admin (userId=99).
            var cert = TestDataBuilder.BuildCertificate(1, userId: 5);
            _certRepoMock
                .Setup(r => r.GetByIdWithUserAndExamAsync(1))
                .ReturnsAsync(cert);

            // ─── ACT ──────────────────────────────────────────────────
            var result = await _sut.GetByIdWithDetailsAsync(1, callerId: 99, isAdmin: true);

            // ─── ASSERT ───────────────────────────────────────────────
            result.CertificateId.Should().Be(1);
        }

        [Test]
        public async Task GetByIdWithDetailsAsync_CandidateCanAccessOwnCert()
        {
            var cert = TestDataBuilder.BuildCertificate(1, userId: 5);
            _certRepoMock
                .Setup(r => r.GetByIdWithUserAndExamAsync(1))
                .ReturnsAsync(cert);

            var result = await _sut.GetByIdWithDetailsAsync(1, callerId: 5, isAdmin: false);

            result.CertificateId.Should().Be(1);
        }

        [Test]
        public async Task GetByIdWithDetailsAsync_CandidateAccessesOthersCert_ThrowsCertificateNotFoundException()
        {
            // ─── ARRANGE ──────────────────────────────────────────────
            // Cert belongs to user 5; candidate 7 tries to access it.
            // Must get 404 (not 403) to avoid leaking existence.
            var cert = TestDataBuilder.BuildCertificate(1, userId: 5);
            _certRepoMock
                .Setup(r => r.GetByIdWithUserAndExamAsync(1))
                .ReturnsAsync(cert);

            // ─── ACT & ASSERT ──────────────────────────────────────────
            var act = async () =>
                await _sut.GetByIdWithDetailsAsync(1, callerId: 7, isAdmin: false);

            await act.Should().ThrowAsync<CertificateNotFoundException>();
        }

        [Test]
        public async Task GetByIdWithDetailsAsync_NonExistentCert_ThrowsCertificateNotFoundException()
        {
            _certRepoMock
                .Setup(r => r.GetByIdWithUserAndExamAsync(It.IsAny<int>()))
                .ReturnsAsync((Certificate?)null);

            var act = async () =>
                await _sut.GetByIdWithDetailsAsync(999, callerId: 1, isAdmin: false);

            await act.Should().ThrowAsync<CertificateNotFoundException>();
        }

        // ════════════════════════════════════════════════════════════════
        //   RevokeAsync
        // ════════════════════════════════════════════════════════════════

        [Test]
        public async Task RevokeAsync_IssuedCertificate_SetsStatusRevokedAndNotifiesCandidate()
        {
            // ─── ARRANGE ──────────────────────────────────────────────
            var cert = TestDataBuilder.BuildCertificate(1, userId: 5, status: CertificateStatus.Issued);

            // GetByIdAsync internally calls GetByIdWithUserAndExamAsync.
            _certRepoMock
                .Setup(r => r.GetByIdWithUserAndExamAsync(1))
                .ReturnsAsync(cert);
            _certRepoMock
                .Setup(r => r.SaveChangesAsync())
                .ReturnsAsync(1);

            // RevokeAsync calls _notifRepository.AddAsync to create a
            // CertificateRevoked notification for the candidate.
            _notifRepoMock
                .Setup(r => r.AddAsync(It.IsAny<Notification>()))
                .ReturnsAsync((Notification n) => n);

            // ─── ACT ──────────────────────────────────────────────────
            var result = await _sut.RevokeAsync(1);

            // ─── ASSERT ───────────────────────────────────────────────
            result.CertStatus.Should().Be(CertificateStatus.Revoked);
            result.RevokedAt.Should().NotBeNull();
            _certRepoMock.Verify(r => r.Update(It.IsAny<Certificate>()), Times.Once);
            _certRepoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
            // A CertificateRevoked notification must be created for the candidate.
            _notifRepoMock.Verify(r => r.AddAsync(It.Is<Notification>(
                n => n.UserId == cert.UserId &&
                     n.NotifType == NotificationType.CertificateRevoked)), Times.Once);
        }

        [Test]
        public async Task RevokeAsync_AlreadyRevokedCertificate_IsNoOp()
        {
            // Revoking an already-revoked cert must not write to the DB and
            // must NOT create a duplicate notification.
            var cert = TestDataBuilder.BuildCertificate(1, status: CertificateStatus.Revoked);
            _certRepoMock
                .Setup(r => r.GetByIdWithUserAndExamAsync(1))
                .ReturnsAsync(cert);

            var result = await _sut.RevokeAsync(1);

            result.CertStatus.Should().Be(CertificateStatus.Revoked);
            _certRepoMock.Verify(r => r.Update(It.IsAny<Certificate>()), Times.Never);
            _certRepoMock.Verify(r => r.SaveChangesAsync(), Times.Never);
            // No notification should be created for an idempotent no-op revoke.
            _notifRepoMock.Verify(r => r.AddAsync(It.IsAny<Notification>()), Times.Never);
            _loggerMock.VerifyWarning(Times.AtLeastOnce());
        }

        [Test]
        public async Task RevokeAsync_NonExistentCert_ThrowsCertificateNotFoundException()
        {
            _certRepoMock
                .Setup(r => r.GetByIdWithUserAndExamAsync(It.IsAny<int>()))
                .ReturnsAsync((Certificate?)null);

            var act = async () => await _sut.RevokeAsync(999);
            await act.Should().ThrowAsync<CertificateNotFoundException>();
        }
    }
}
