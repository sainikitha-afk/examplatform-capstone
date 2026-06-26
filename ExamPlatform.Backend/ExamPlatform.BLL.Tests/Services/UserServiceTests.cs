// UserServiceTests.cs — covers GetByIdAsync, GetAllAsync, GetAllPagedAsync, SetActiveStatusAsync.

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
    public class UserServiceTests
    {
        private Mock<IUserRepository> _userRepoMock = null!;
        private Mock<ILogger<UserService>> _loggerMock = null!;
        private UserService _sut = null!;

        [SetUp]
        public void SetUp()
        {
            _userRepoMock = new Mock<IUserRepository>();
            _loggerMock   = new Mock<ILogger<UserService>>();
            _sut = new UserService(_userRepoMock.Object, _loggerMock.Object);
        }

        // ════════════════════════════════════════════════════════════════
        //   GetByIdAsync
        // ════════════════════════════════════════════════════════════════

        [Test]
        public async Task GetByIdAsync_ExistingUser_ReturnsUser()
        {
            var user = TestDataBuilder.BuildCandidate(userId: 5);
            _userRepoMock.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(user);

            var result = await _sut.GetByIdAsync(5);

            result.UserId.Should().Be(5);
        }

        [Test]
        public async Task GetByIdAsync_NonExistentUser_ThrowsUserNotFoundException()
        {
            _userRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<int>()))
                         .ReturnsAsync((User?)null);

            var act = async () => await _sut.GetByIdAsync(999);
            await act.Should().ThrowAsync<UserNotFoundException>();
        }

        // ════════════════════════════════════════════════════════════════
        //   GetAllAsync
        // ════════════════════════════════════════════════════════════════

        [Test]
        public async Task GetAllAsync_ReturnsAllUsers()
        {
            var users = new List<User>
            {
                TestDataBuilder.BuildCandidate(1),
                TestDataBuilder.BuildExaminer(2),
            };
            _userRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(users);

            var result = await _sut.GetAllAsync();

            result.Should().HaveCount(2);
        }

        // ════════════════════════════════════════════════════════════════
        //   SetActiveStatusAsync
        // ════════════════════════════════════════════════════════════════

        [Test]
        public async Task SetActiveStatusAsync_DeactivatesActiveUser()
        {
            var user = TestDataBuilder.BuildCandidate(userId: 1, isActive: true);
            _userRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(user);
            // SaveChangesAsync returns Task<int>
            _userRepoMock.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);

            var result = await _sut.SetActiveStatusAsync(1, false);

            result.IsActive.Should().BeFalse();
            _userRepoMock.Verify(r => r.Update(It.IsAny<User>()), Times.Once);
            _userRepoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Test]
        public async Task SetActiveStatusAsync_SameStatusAsExisting_IsNoOp()
        {
            // User is already active; setting active = true should skip the save.
            var user = TestDataBuilder.BuildCandidate(userId: 1, isActive: true);
            _userRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(user);

            var result = await _sut.SetActiveStatusAsync(1, true);

            result.IsActive.Should().BeTrue();
            _userRepoMock.Verify(r => r.SaveChangesAsync(), Times.Never);
        }

        [Test]
        public async Task SetActiveStatusAsync_NonExistentUser_ThrowsUserNotFoundException()
        {
            _userRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<int>()))
                         .ReturnsAsync((User?)null);

            var act = async () => await _sut.SetActiveStatusAsync(999, false);
            await act.Should().ThrowAsync<UserNotFoundException>();
        }

        // ════════════════════════════════════════════════════════════════
        //   GetAllPagedAsync — pagination
        // ════════════════════════════════════════════════════════════════

        [Test]
        public async Task GetAllPagedAsync_FirstPage_ReturnsItemsAndMetadata()
        {
            // ─── ARRANGE ──────────────────────────────────────────────
            // Simulates 25 total users with page 1 returning the first 20.
            var firstPageItems = Enumerable.Range(1, 20)
                .Select(i => TestDataBuilder.BuildCandidate(i))
                .ToList();

            // GetPagedAsync returns a value-tuple (Items, TotalCount) — Moq
            // handles tuple returns via ReturnsAsync with a matching tuple literal.
            _userRepoMock
                .Setup(r => r.GetPagedAsync(1, 20))
                .ReturnsAsync(((IEnumerable<User>)firstPageItems, 25));

            // ─── ACT ──────────────────────────────────────────────────
            var result = await _sut.GetAllPagedAsync(page: 1, pageSize: 20);

            // ─── ASSERT ───────────────────────────────────────────────
            result.Items.Should().HaveCount(20);
            result.Page.Should().Be(1);
            result.PageSize.Should().Be(20);
            result.TotalCount.Should().Be(25);
            result.TotalPages.Should().Be(2);        // ceil(25 / 20) = 2
            result.HasNextPage.Should().BeTrue();
            result.HasPreviousPage.Should().BeFalse();
        }

        [Test]
        public async Task GetAllPagedAsync_SecondPage_SkipsAndTakesCorrectly()
        {
            // ─── ARRANGE ──────────────────────────────────────────────
            // Page 2 with pageSize=10 — repo returns items 11-15 (5 remaining out of 15).
            var secondPageItems = Enumerable.Range(11, 5)
                .Select(i => TestDataBuilder.BuildCandidate(i))
                .ToList();

            _userRepoMock
                .Setup(r => r.GetPagedAsync(2, 10))
                .ReturnsAsync(((IEnumerable<User>)secondPageItems, 15));

            // ─── ACT ──────────────────────────────────────────────────
            var result = await _sut.GetAllPagedAsync(page: 2, pageSize: 10);

            // ─── ASSERT ───────────────────────────────────────────────
            result.Items.Should().HaveCount(5);
            result.Page.Should().Be(2);
            result.TotalCount.Should().Be(15);
            result.TotalPages.Should().Be(2);        // ceil(15 / 10) = 2
            result.HasPreviousPage.Should().BeTrue();
            result.HasNextPage.Should().BeFalse();   // page 2 is the last page
        }

        [Test]
        public async Task GetAllPagedAsync_PageSizeOver100_ClampsTo100()
        {
            // ─── ARRANGE ──────────────────────────────────────────────
            // pageSize = 200 must be clamped to 100 inside the service
            // BEFORE the repository call — so the repo must be set up with 100.
            var items = new List<User> { TestDataBuilder.BuildCandidate(1) };

            _userRepoMock
                .Setup(r => r.GetPagedAsync(1, 100))   // clamped value, not 200
                .ReturnsAsync(((IEnumerable<User>)items, 1));

            // ─── ACT ──────────────────────────────────────────────────
            var result = await _sut.GetAllPagedAsync(page: 1, pageSize: 200);

            // ─── ASSERT ───────────────────────────────────────────────
            result.PageSize.Should().Be(100);   // clamped
            _userRepoMock.Verify(r => r.GetPagedAsync(1, 100), Times.Once);
            // The repository must NOT be called with 200 (unclamped value).
            _userRepoMock.Verify(r => r.GetPagedAsync(1, 200), Times.Never);
        }
    }
}
