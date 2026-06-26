// NotificationServiceTests.cs — covers GetByUserAsync, GetUnreadCountAsync,
// MarkAsReadAsync (happy, idempotent, not-found, wrong-owner), MarkAllReadAsync.

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
    public class NotificationServiceTests
    {
        private Mock<INotificationRepository> _notifRepoMock = null!;
        private Mock<ILogger<NotificationService>> _loggerMock = null!;
        private NotificationService _sut = null!;

        [SetUp]
        public void SetUp()
        {
            _notifRepoMock = new Mock<INotificationRepository>();
            _loggerMock    = new Mock<ILogger<NotificationService>>();
            _sut = new NotificationService(_notifRepoMock.Object, _loggerMock.Object);
        }

        [Test]
        public async Task GetByUserAsync_ReturnsNotificationsForUser()
        {
            var notifs = new List<Notification>
            {
                TestDataBuilder.BuildNotification(1, userId: 5),
                TestDataBuilder.BuildNotification(2, userId: 5),
            };
            _notifRepoMock.Setup(r => r.GetByUserAsync(5)).ReturnsAsync(notifs);

            var result = await _sut.GetByUserAsync(5);

            result.Should().HaveCount(2);
        }

        [Test]
        public async Task GetUnreadCountAsync_ReturnsCount()
        {
            _notifRepoMock.Setup(r => r.UnreadCountAsync(5)).ReturnsAsync(3);

            var count = await _sut.GetUnreadCountAsync(5);

            count.Should().Be(3);
        }

        [Test]
        public async Task MarkAsReadAsync_UnreadNotification_MarksReadAndSaves()
        {
            var notif = TestDataBuilder.BuildNotification(1, userId: 5, isRead: false);
            _notifRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(notif);
            // SaveChangesAsync returns Task<int>
            _notifRepoMock.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);

            var result = await _sut.MarkAsReadAsync(1, userId: 5);

            result.IsRead.Should().BeTrue();
            _notifRepoMock.Verify(r => r.Update(It.IsAny<Notification>()), Times.Once);
            _notifRepoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Test]
        public async Task MarkAsReadAsync_NotificationNotFound_ThrowsUserNotFoundException()
        {
            _notifRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<int>()))
                         .ReturnsAsync((Notification?)null);

            var act = async () => await _sut.MarkAsReadAsync(99, userId: 5);
            await act.Should().ThrowAsync<UserNotFoundException>();
        }

        [Test]
        public async Task MarkAsReadAsync_BelongsToAnotherUser_ThrowsUserNotFoundException()
        {
            // Notification belongs to userId=7; caller is userId=5.
            var notif = TestDataBuilder.BuildNotification(1, userId: 7);
            _notifRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(notif);

            var act = async () => await _sut.MarkAsReadAsync(1, userId: 5);
            await act.Should().ThrowAsync<UserNotFoundException>();
        }

        [Test]
        public async Task MarkAsReadAsync_AlreadyReadNotification_IsNoOp()
        {
            // Already read — no DB write.
            var notif = TestDataBuilder.BuildNotification(1, userId: 5, isRead: true);
            _notifRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(notif);

            var result = await _sut.MarkAsReadAsync(1, userId: 5);

            result.IsRead.Should().BeTrue();
            _notifRepoMock.Verify(r => r.SaveChangesAsync(), Times.Never);
        }

        [Test]
        public async Task MarkAllReadAsync_CallsRepository()
        {
            // MarkAllReadAsync uses ExecuteUpdateAsync — returns Task (not Task<int>).
            _notifRepoMock.Setup(r => r.MarkAllReadAsync(5)).Returns(Task.CompletedTask);

            await _sut.MarkAllReadAsync(5);

            _notifRepoMock.Verify(r => r.MarkAllReadAsync(5), Times.Once);
        }
    }
}
