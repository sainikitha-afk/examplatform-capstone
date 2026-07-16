// AnalyticsServiceTests.cs
//
// WHY THESE TESTS EXIST:
//   AnalyticsService has complex in-memory LINQ aggregations (average, pass rate,
//   best-score-per-exam grouping). Tests verify:
//     - Empty datasets return safe zero values (no divide-by-zero).
//     - Best-score-per-exam picks MaxBy correctly when a user has multiple attempts.
//     - Pass rate computation matches expectation.
//     - PlatformStats counts by role correctly.

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
    public class AnalyticsServiceTests
    {
        private Mock<IAttemptRepository>     _attemptRepoMock = null!;
        private Mock<ICertificateRepository> _certRepoMock    = null!;
        private Mock<IUserRepository>        _userRepoMock    = null!;
        private Mock<IExamRepository>        _examRepoMock    = null!;
        private Mock<ILogger<AnalyticsService>> _loggerMock   = null!;
        private AnalyticsService _sut = null!;

        [SetUp]
        public void SetUp()
        {
            _attemptRepoMock = new Mock<IAttemptRepository>();
            _certRepoMock    = new Mock<ICertificateRepository>();
            _userRepoMock    = new Mock<IUserRepository>();
            _examRepoMock    = new Mock<IExamRepository>();
            _loggerMock      = new Mock<ILogger<AnalyticsService>>();

            _sut = new AnalyticsService(
                _attemptRepoMock.Object,
                _certRepoMock.Object,
                _userRepoMock.Object,
                _examRepoMock.Object,
                _loggerMock.Object);
        }

        // ════════════════════════════════════════════════════════════════
        //   GetCandidateDashboardAsync — EMPTY DATA
        // ════════════════════════════════════════════════════════════════

        [Test]
        public async Task GetCandidateDashboardAsync_NoAttempts_ReturnsAllZeros()
        {
            // ─── ARRANGE ──────────────────────────────────────────────
            _attemptRepoMock.Setup(r => r.GetByUserAsync(1))
                             .ReturnsAsync(new List<Attempt>());
            _certRepoMock.Setup(r => r.GetByUserAsync(1))
                         .ReturnsAsync(new List<Certificate>());

            // ─── ACT ──────────────────────────────────────────────────
            var result = await _sut.GetCandidateDashboardAsync(userId: 1);

            // ─── ASSERT ───────────────────────────────────────────────
            result.TotalAttempts.Should().Be(0);
            result.AverageScore.Should().Be(0.0);
            result.PassRate.Should().Be(0.0);
            result.CertificatesEarned.Should().Be(0);
            result.BestScores.Should().BeEmpty();
        }

        [Test]
        public async Task GetCandidateDashboardAsync_OnlyInProgressAttempts_ReturnsZeroSubmitted()
        {
            // InProgress attempts are excluded from stats (no score yet).
            var inProgress = TestDataBuilder.BuildInProgressAttempt(1, 1, 1);
            _attemptRepoMock.Setup(r => r.GetByUserAsync(1))
                             .ReturnsAsync(new List<Attempt> { inProgress });
            _certRepoMock.Setup(r => r.GetByUserAsync(1))
                         .ReturnsAsync(new List<Certificate>());

            var result = await _sut.GetCandidateDashboardAsync(userId: 1);

            result.TotalAttempts.Should().Be(0); // InProgress excluded
        }

        // ════════════════════════════════════════════════════════════════
        //   GetCandidateDashboardAsync — PASS RATE + AVERAGE SCORE
        // ════════════════════════════════════════════════════════════════

        [Test]
        public async Task GetCandidateDashboardAsync_MixedResults_ComputesCorrectPassRate()
        {
            // ─── ARRANGE ──────────────────────────────────────────────
            // 2 passed (score ≥ passing), 1 failed. Expected pass rate = 66.67%.
            // Exams must be ResultsPublished for analytics to include the attempts.
            var exam = TestDataBuilder.BuildResultsPublishedExam(examId: 1);

            var a1 = TestDataBuilder.BuildSubmittedAttempt(1, 1, 1, score: 40m, isPassed: true);
            var a2 = TestDataBuilder.BuildSubmittedAttempt(2, 1, 2, score: 35m, isPassed: true);
            var a3 = TestDataBuilder.BuildSubmittedAttempt(3, 1, 3, score: 20m, isPassed: false);

            // Attach Exam navigation so ExamTitle is available.
            a1.Exam = exam;
            a2.Exam = TestDataBuilder.BuildResultsPublishedExam(examId: 2);
            a3.Exam = TestDataBuilder.BuildResultsPublishedExam(examId: 3);

            _attemptRepoMock.Setup(r => r.GetByUserAsync(1))
                             .ReturnsAsync(new List<Attempt> { a1, a2, a3 });
            _certRepoMock.Setup(r => r.GetByUserAsync(1))
                         .ReturnsAsync(new List<Certificate> { TestDataBuilder.BuildCertificate() });

            // ─── ACT ──────────────────────────────────────────────────
            var result = await _sut.GetCandidateDashboardAsync(userId: 1);

            // ─── ASSERT ───────────────────────────────────────────────
            result.TotalAttempts.Should().Be(3);
            result.PassRate.Should().BeApproximately(66.67, 0.1);
            result.AverageScore.Should().BeApproximately(31.67, 0.1); // (40+35+20)/3
            result.CertificatesEarned.Should().Be(1);
            result.BestScores.Should().HaveCount(3); // one entry per exam
        }

        [Test]
        public async Task GetCandidateDashboardAsync_MultipleAttemptsOnSameExam_PicksBestScore()
        {
            // ─── ARRANGE ──────────────────────────────────────────────
            // Two attempts on the same exam — BestScores should pick the higher one.
            var exam = TestDataBuilder.BuildResultsPublishedExam(examId: 1);

            var a1 = TestDataBuilder.BuildSubmittedAttempt(1, 1, examId: 1, score: 30m, isPassed: false);
            var a2 = TestDataBuilder.BuildSubmittedAttempt(2, 1, examId: 1, score: 45m, isPassed: true);
            a1.Exam = exam;
            a2.Exam = exam;

            _attemptRepoMock.Setup(r => r.GetByUserAsync(1))
                             .ReturnsAsync(new List<Attempt> { a1, a2 });
            _certRepoMock.Setup(r => r.GetByUserAsync(1))
                         .ReturnsAsync(new List<Certificate>());

            // ─── ACT ──────────────────────────────────────────────────
            var result = await _sut.GetCandidateDashboardAsync(userId: 1);

            // ─── ASSERT ───────────────────────────────────────────────
            result.BestScores.Should().HaveCount(1); // one entry per exam
            result.BestScores.First().BestScore.Should().Be(45m); // the higher score
            result.BestScores.First().IsPassed.Should().BeTrue();
        }

        // ════════════════════════════════════════════════════════════════
        //   GetCandidateDashboardAsync — PUBLICATION RULE (no result leak)
        // ════════════════════════════════════════════════════════════════

        [Test]
        public async Task GetCandidateDashboardAsync_SubmittedOnUnpublishedExam_IsExcluded()
        {
            // A submitted attempt whose exam is only Published (results NOT released)
            // must not appear in analytics — no score, pass/fail, or best-score leak.
            var a1 = TestDataBuilder.BuildSubmittedAttempt(1, 1, examId: 1, score: 45m, isPassed: true);
            a1.Exam = TestDataBuilder.BuildPublishedExam(examId: 1); // ExamStatus.Published

            _attemptRepoMock.Setup(r => r.GetByUserAsync(1))
                             .ReturnsAsync(new List<Attempt> { a1 });
            _certRepoMock.Setup(r => r.GetByUserAsync(1))
                         .ReturnsAsync(new List<Certificate>());

            var result = await _sut.GetCandidateDashboardAsync(userId: 1);

            result.TotalAttempts.Should().Be(0);        // excluded
            result.AverageScore.Should().Be(0.0);       // no score leaked
            result.PassRate.Should().Be(0.0);           // no pass/fail leaked
            result.BestScores.Should().BeEmpty();       // no best-score entry leaked
        }

        [Test]
        public async Task GetCandidateDashboardAsync_AutoSubmittedOnUnpublishedExam_IsExcluded()
        {
            var a1 = TestDataBuilder.BuildSubmittedAttempt(1, 1, examId: 1, score: 50m, isPassed: true);
            a1.AttemptStatus = AttemptStatus.AutoSubmitted;
            a1.Exam = TestDataBuilder.BuildPublishedExam(examId: 1); // not ResultsPublished

            _attemptRepoMock.Setup(r => r.GetByUserAsync(1))
                             .ReturnsAsync(new List<Attempt> { a1 });
            _certRepoMock.Setup(r => r.GetByUserAsync(1))
                         .ReturnsAsync(new List<Certificate>());

            var result = await _sut.GetCandidateDashboardAsync(userId: 1);

            result.TotalAttempts.Should().Be(0);
            result.BestScores.Should().BeEmpty();
        }

        [Test]
        public async Task GetCandidateDashboardAsync_AutoSubmittedOnResultsPublishedExam_IsIncluded()
        {
            // AutoSubmitted on a ResultsPublished exam IS a legitimate, published result.
            var a1 = TestDataBuilder.BuildSubmittedAttempt(1, 1, examId: 1, score: 40m, isPassed: true);
            a1.AttemptStatus = AttemptStatus.AutoSubmitted;
            a1.Exam = TestDataBuilder.BuildResultsPublishedExam(examId: 1);

            _attemptRepoMock.Setup(r => r.GetByUserAsync(1))
                             .ReturnsAsync(new List<Attempt> { a1 });
            _certRepoMock.Setup(r => r.GetByUserAsync(1))
                         .ReturnsAsync(new List<Certificate>());

            var result = await _sut.GetCandidateDashboardAsync(userId: 1);

            result.TotalAttempts.Should().Be(1);
            result.PassRate.Should().Be(100.0);
            result.BestScores.Should().HaveCount(1);
            result.BestScores.First().BestScore.Should().Be(40m);
        }

        [Test]
        public async Task GetCandidateDashboardAsync_MixedPublishedAndUnpublished_CountsOnlyPublished()
        {
            var published = TestDataBuilder.BuildSubmittedAttempt(1, 1, examId: 1, score: 40m, isPassed: true);
            published.Exam = TestDataBuilder.BuildResultsPublishedExam(examId: 1);

            var unpublished = TestDataBuilder.BuildSubmittedAttempt(2, 1, examId: 2, score: 10m, isPassed: false);
            unpublished.Exam = TestDataBuilder.BuildPublishedExam(examId: 2);

            _attemptRepoMock.Setup(r => r.GetByUserAsync(1))
                             .ReturnsAsync(new List<Attempt> { published, unpublished });
            _certRepoMock.Setup(r => r.GetByUserAsync(1))
                         .ReturnsAsync(new List<Certificate>());

            var result = await _sut.GetCandidateDashboardAsync(userId: 1);

            result.TotalAttempts.Should().Be(1);          // only the published one
            result.AverageScore.Should().Be(40.0);        // unpublished 10 excluded
            result.PassRate.Should().Be(100.0);           // unpublished fail excluded
            result.BestScores.Should().HaveCount(1);
        }

        // ════════════════════════════════════════════════════════════════
        //   GetCandidateDashboardAsync — CERTIFICATE COUNT (exclude revoked)
        // ════════════════════════════════════════════════════════════════

        [Test]
        public async Task GetCandidateDashboardAsync_RevokedCertificate_IsExcludedFromCount()
        {
            _attemptRepoMock.Setup(r => r.GetByUserAsync(1))
                             .ReturnsAsync(new List<Attempt>());
            _certRepoMock.Setup(r => r.GetByUserAsync(1))
                         .ReturnsAsync(new List<Certificate>
                         {
                             TestDataBuilder.BuildCertificate(1, status: CertificateStatus.Issued),
                             TestDataBuilder.BuildCertificate(2, status: CertificateStatus.Revoked),
                             TestDataBuilder.BuildCertificate(3, status: CertificateStatus.Issued),
                         });

            var result = await _sut.GetCandidateDashboardAsync(userId: 1);

            result.CertificatesEarned.Should().Be(2); // 2 issued, revoked one excluded
        }

        // ════════════════════════════════════════════════════════════════
        //   GetPlatformStatsAsync
        // ════════════════════════════════════════════════════════════════

        [Test]
        public async Task GetPlatformStatsAsync_CountsByRole()
        {
            // ─── ARRANGE ──────────────────────────────────────────────
            var users = new List<User>
            {
                TestDataBuilder.BuildCandidate(1),
                TestDataBuilder.BuildCandidate(2),
                TestDataBuilder.BuildExaminer(10),
                TestDataBuilder.BuildAdmin(99),
            };
            var exams = new List<Exam>
            {
                TestDataBuilder.BuildPublishedExam(1),
                TestDataBuilder.BuildDraftExam(2),
            };
            var attempts = new List<Attempt>
            {
                TestDataBuilder.BuildSubmittedAttempt(1),
            };
            var certs = new List<Certificate>
            {
                TestDataBuilder.BuildCertificate(1, status: CertificateStatus.Issued),
            };

            _userRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(users);
            _examRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(exams);
            _attemptRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(attempts);
            _certRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(certs);

            // ─── ACT ──────────────────────────────────────────────────
            var result = await _sut.GetPlatformStatsAsync();

            // ─── ASSERT ───────────────────────────────────────────────
            result.TotalUsers.Should().Be(2);       // only Candidates
            result.TotalExaminers.Should().Be(1);
            result.TotalExams.Should().Be(2);
            result.TotalAttempts.Should().Be(1);
            result.TotalCertificatesIssued.Should().Be(1);
        }

        [Test]
        public async Task GetPlatformStatsAsync_ExamsCompletedLast30Days_CountsCorrectly()
        {
            // ─── ARRANGE ──────────────────────────────────────────────
            var users    = new List<User>();
            var attempts = new List<Attempt>();
            var certs    = new List<Certificate>();

            // One exam ended 10 days ago with ResultsPublished (should count).
            var recentExam = TestDataBuilder.BuildPublishedExam(1);
            recentExam.ExamStatus = ExamStatus.ResultsPublished;
            recentExam.EndTime    = DateTime.UtcNow.AddDays(-10);

            // One exam ended 45 days ago (outside the 30-day window — should NOT count).
            var oldExam = TestDataBuilder.BuildPublishedExam(2);
            oldExam.ExamStatus = ExamStatus.ResultsPublished;
            oldExam.EndTime    = DateTime.UtcNow.AddDays(-45);

            _userRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(users);
            _examRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Exam> { recentExam, oldExam });
            _attemptRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(attempts);
            _certRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(certs);

            // ─── ACT ──────────────────────────────────────────────────
            var result = await _sut.GetPlatformStatsAsync();

            // ─── ASSERT ───────────────────────────────────────────────
            result.ExamsCompletedLast30Days.Should().Be(1); // only the recent one
        }
    }
}
