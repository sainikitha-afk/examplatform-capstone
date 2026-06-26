// AuthServiceTests.cs — covers RegisterAsync, CreateExaminerAsync, LoginAsync.
// Key learning for the viva: IRepository.AddAsync returns Task<T> (not Task),
// and SaveChangesAsync returns Task<int>. Moq requires ReturnsAsync for both.

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
    public class AuthServiceTests
    {
        private Mock<IUserRepository> _userRepoMock = null!;
        private Mock<ILogger<AuthService>> _loggerMock = null!;
        private AuthService _sut = null!;

        [SetUp]
        public void SetUp()
        {
            _userRepoMock = new Mock<IUserRepository>();
            _loggerMock   = new Mock<ILogger<AuthService>>();

            // Happy-path defaults — override per test when needed.
            _userRepoMock.Setup(r => r.IsEmailTakenAsync(It.IsAny<string>()))
                         .ReturnsAsync(false);
            _userRepoMock.Setup(r => r.IsUsernameTakenAsync(It.IsAny<string>()))
                         .ReturnsAsync(false);
            // AddAsync returns Task<User> — return the entity that was passed in.
            _userRepoMock.Setup(r => r.AddAsync(It.IsAny<User>()))
                         .ReturnsAsync((User u) => u);
            // SaveChangesAsync returns Task<int> (rows affected).
            _userRepoMock.Setup(r => r.SaveChangesAsync())
                         .ReturnsAsync(1);

            _sut = new AuthService(_userRepoMock.Object, _loggerMock.Object);
        }

        // ════════════════════════════════════════════════════════════════
        //   RegisterAsync — HAPPY PATH
        // ════════════════════════════════════════════════════════════════

        [Test]
        public async Task RegisterAsync_WithValidInput_CreatesAndReturnsCandidateUser()
        {
            // ─── ACT ──────────────────────────────────────────────────
            var result = await _sut.RegisterAsync("Alice Smith", "alice", "alice@test.com", "secret123");

            // ─── ASSERT ───────────────────────────────────────────────
            result.Should().NotBeNull();
            result.Role.Should().Be(UserRole.Candidate); // self-register always = Candidate
            result.IsActive.Should().BeTrue();
            result.PasswordHash.Should().NotBe("secret123"); // must be hashed

            _userRepoMock.Verify(r => r.AddAsync(It.IsAny<User>()), Times.Once);
            _userRepoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        // ════════════════════════════════════════════════════════════════
        //   RegisterAsync — VALIDATION FAILURES
        // ════════════════════════════════════════════════════════════════

        [TestCase("", "alice", "alice@test.com", "secret123", TestName = "EmptyFullName")]
        [TestCase("Alice", "ab", "alice@test.com", "secret123", TestName = "ShortUsername")]
        [TestCase("Alice", "alice", "notanemail", "secret123", TestName = "InvalidEmail")]
        [TestCase("Alice", "alice", "alice@test.com", "12345", TestName = "ShortPassword")]
        public async Task RegisterAsync_WithInvalidInput_ThrowsValidationException(
            string fullName, string username, string email, string password)
        {
            var act = async () => await _sut.RegisterAsync(fullName, username, email, password);
            await act.Should().ThrowAsync<ValidationException>();
            _userRepoMock.Verify(r => r.AddAsync(It.IsAny<User>()), Times.Never);
        }

        [Test]
        public async Task RegisterAsync_WithDuplicateEmail_ThrowsDuplicateEmailException()
        {
            _userRepoMock.Setup(r => r.IsEmailTakenAsync(It.IsAny<string>())).ReturnsAsync(true);

            var act = async () => await _sut.RegisterAsync("Alice", "alice", "alice@test.com", "secret123");
            await act.Should().ThrowAsync<DuplicateEmailException>();
            _userRepoMock.Verify(r => r.AddAsync(It.IsAny<User>()), Times.Never);
        }

        [Test]
        public async Task RegisterAsync_WithDuplicateUsername_ThrowsDuplicateUsernameException()
        {
            _userRepoMock.Setup(r => r.IsUsernameTakenAsync(It.IsAny<string>())).ReturnsAsync(true);

            var act = async () => await _sut.RegisterAsync("Alice", "alice", "alice@test.com", "secret123");
            await act.Should().ThrowAsync<DuplicateUsernameException>();
        }

        // ════════════════════════════════════════════════════════════════
        //   CreateExaminerAsync
        // ════════════════════════════════════════════════════════════════

        [Test]
        public async Task CreateExaminerAsync_WithValidInput_CreatesExaminerRole()
        {
            var result = await _sut.CreateExaminerAsync("Bob Exam", "bob", "bob@test.com", "pass123");

            result.Role.Should().Be(UserRole.Examiner);
            _userRepoMock.Verify(r => r.AddAsync(It.Is<User>(u => u.Role == UserRole.Examiner)), Times.Once);
        }

        [Test]
        public async Task CreateExaminerAsync_WithDuplicateEmail_ThrowsDuplicateEmailException()
        {
            _userRepoMock.Setup(r => r.IsEmailTakenAsync(It.IsAny<string>())).ReturnsAsync(true);

            var act = async () => await _sut.CreateExaminerAsync("Bob", "bob", "bob@test.com", "pass123");
            await act.Should().ThrowAsync<DuplicateEmailException>();
        }

        // ════════════════════════════════════════════════════════════════
        //   LoginAsync — HAPPY PATH
        // ════════════════════════════════════════════════════════════════

        [Test]
        public async Task LoginAsync_WithValidEmailAndPassword_ReturnsUser()
        {
            // ─── ARRANGE ──────────────────────────────────────────────
            // Hash a real password so BCrypt.Verify in the service will succeed.
            var passwordHash = BCrypt.Net.BCrypt.HashPassword("password123");
            var user = TestDataBuilder.BuildCandidate();
            user.PasswordHash = passwordHash;

            _userRepoMock.Setup(r => r.GetByEmailAsync("candidate1@test.com"))
                         .ReturnsAsync(user);

            // ─── ACT ──────────────────────────────────────────────────
            var result = await _sut.LoginAsync("candidate1@test.com", "password123");

            // ─── ASSERT ───────────────────────────────────────────────
            result.UserId.Should().Be(user.UserId);
        }

        [Test]
        public async Task LoginAsync_WithValidUsernameAndPassword_ReturnsUser()
        {
            var passwordHash = BCrypt.Net.BCrypt.HashPassword("password123");
            var user = TestDataBuilder.BuildCandidate();
            user.PasswordHash = passwordHash;

            // Email lookup misses; username lookup hits.
            _userRepoMock.Setup(r => r.GetByEmailAsync(It.IsAny<string>()))
                         .ReturnsAsync((User?)null);
            _userRepoMock.Setup(r => r.GetByUsernameAsync(user.Username))
                         .ReturnsAsync(user);

            var result = await _sut.LoginAsync(user.Username, "password123");
            result.UserId.Should().Be(user.UserId);
        }

        // ════════════════════════════════════════════════════════════════
        //   LoginAsync — FAILURE PATHS
        // ════════════════════════════════════════════════════════════════

        [Test]
        public async Task LoginAsync_WithUnknownUser_ThrowsInvalidCredentialsException()
        {
            _userRepoMock.Setup(r => r.GetByEmailAsync(It.IsAny<string>()))
                         .ReturnsAsync((User?)null);
            _userRepoMock.Setup(r => r.GetByUsernameAsync(It.IsAny<string>()))
                         .ReturnsAsync((User?)null);

            var act = async () => await _sut.LoginAsync("nobody@test.com", "password123");
            await act.Should().ThrowAsync<InvalidCredentialsException>();

            // Unknown identifier is a security signal — must log a warning.
            _loggerMock.VerifyWarning(Times.AtLeastOnce());
        }

        [Test]
        public async Task LoginAsync_WithDeactivatedUser_ThrowsUserDeactivatedException()
        {
            var passwordHash = BCrypt.Net.BCrypt.HashPassword("password123");
            var deactivatedUser = TestDataBuilder.BuildCandidate(isActive: false);
            deactivatedUser.PasswordHash = passwordHash;

            _userRepoMock.Setup(r => r.GetByEmailAsync(It.IsAny<string>()))
                         .ReturnsAsync(deactivatedUser);

            var act = async () => await _sut.LoginAsync(deactivatedUser.Email, "password123");
            await act.Should().ThrowAsync<UserDeactivatedException>();
            _loggerMock.VerifyWarning(Times.AtLeastOnce());
        }

        [Test]
        public async Task LoginAsync_WithWrongPassword_ThrowsInvalidCredentialsException()
        {
            var passwordHash = BCrypt.Net.BCrypt.HashPassword("correctPassword");
            var user = TestDataBuilder.BuildCandidate();
            user.PasswordHash = passwordHash;

            _userRepoMock.Setup(r => r.GetByEmailAsync(user.Email)).ReturnsAsync(user);

            // BCrypt.Verify("wrongPassword", correctHash) → false
            var act = async () => await _sut.LoginAsync(user.Email, "wrongPassword");
            await act.Should().ThrowAsync<InvalidCredentialsException>();
            _loggerMock.VerifyWarning(Times.AtLeastOnce());
        }

        [TestCase("", "password", TestName = "EmptyIdentifier")]
        [TestCase("user@test.com", "", TestName = "EmptyPassword")]
        public async Task LoginAsync_WithEmptyInputs_ThrowsInvalidCredentialsException(
            string identifier, string password)
        {
            var act = async () => await _sut.LoginAsync(identifier, password);
            await act.Should().ThrowAsync<InvalidCredentialsException>();
        }
    }
}
