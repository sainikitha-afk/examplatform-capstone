// RevenueAnalyticsServiceTests — verifies every aggregation in the Admin
// Revenue Dashboard summary: revenue split, failure rate, monthly grouping,
// plan distribution, top purchased exams, recent transactions, conversion
// rate, and coupon redemption count.

using ExamPlatform.BLL.Services;
using ExamPlatform.DAL.Interfaces;
using ExamPlatform.Domain.Entities;
using ExamPlatform.Domain.Enums;
using FluentAssertions;
using Moq;
using NUnit.Framework;

namespace ExamPlatform.BLL.Tests.Services
{
    [TestFixture]
    public class RevenueAnalyticsServiceTests
    {
        private Mock<IPaymentRepository> _paymentRepoMock = null!;
        private Mock<IExaminerSubscriptionRepository> _subRepoMock = null!;
        private RevenueAnalyticsService _sut = null!;

        [SetUp]
        public void SetUp()
        {
            _paymentRepoMock = new Mock<IPaymentRepository>();
            _subRepoMock = new Mock<IExaminerSubscriptionRepository>();
            _sut = new RevenueAnalyticsService(_paymentRepoMock.Object, _subRepoMock.Object);
        }

        private static Payment SuccessPayment(
            decimal amount, PaymentPurpose purpose, DateTime completedAt,
            User? user = null, CandidatePurchase? purchase = null, int? couponId = null) => new()
        {
            PaymentId = new Random().Next(1, 100000),
            UserId = user?.UserId ?? 1,
            User = user!,
            Amount = amount,
            Purpose = purpose,
            Status = PaymentStatus.Success,
            Method = PaymentMethod.CreditCard,
            FakeTransactionId = Guid.NewGuid().ToString(),
            FakeGatewayReference = Guid.NewGuid().ToString(),
            IdempotencyKey = Guid.NewGuid().ToString(),
            CouponId = couponId,
            InitiatedAt = completedAt,
            CompletedAt = completedAt,
            CandidatePurchase = purchase
        };

        [Test]
        public async Task GetSummaryAsync_SplitsSubscriptionAndExamRevenueCorrectly()
        {
            var payments = new List<Payment>
            {
                SuccessPayment(1000m, PaymentPurpose.SubscriptionPurchase, new DateTime(2026,6,1)),
                SuccessPayment(500m, PaymentPurpose.SubscriptionRenewal, new DateTime(2026,6,2)),
                SuccessPayment(300m, PaymentPurpose.ExamCertificationPurchase, new DateTime(2026,6,3))
            };
            _paymentRepoMock.Setup(r => r.GetAllForAnalyticsAsync()).ReturnsAsync(payments);
            _subRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<ExaminerSubscription>());

            var summary = await _sut.GetSummaryAsync();

            summary.TotalRevenue.Should().Be(1800m);
            summary.SubscriptionRevenue.Should().Be(1500m);
            summary.ExamCertificationRevenue.Should().Be(300m);
        }

        [Test]
        public async Task GetSummaryAsync_ComputesFailureRateExcludingTransientAndRefunded()
        {
            var payments = new List<Payment>
            {
                SuccessPayment(100m, PaymentPurpose.ExamCertificationPurchase, DateTime.UtcNow),
                new() { PaymentId = 2, Status = PaymentStatus.Failed, Purpose = PaymentPurpose.ExamCertificationPurchase,
                    Method = PaymentMethod.CreditCard, FakeTransactionId="t2", FakeGatewayReference="g2", IdempotencyKey="k2", InitiatedAt = DateTime.UtcNow, Amount = 100m },
                new() { PaymentId = 3, Status = PaymentStatus.Pending, Purpose = PaymentPurpose.ExamCertificationPurchase,
                    Method = PaymentMethod.CreditCard, FakeTransactionId="t3", FakeGatewayReference="g3", IdempotencyKey="k3", InitiatedAt = DateTime.UtcNow, Amount = 100m },
                new() { PaymentId = 4, Status = PaymentStatus.Refunded, Purpose = PaymentPurpose.ExamCertificationPurchase,
                    Method = PaymentMethod.CreditCard, FakeTransactionId="t4", FakeGatewayReference="g4", IdempotencyKey="k4", InitiatedAt = DateTime.UtcNow, Amount = 100m },
            };
            _paymentRepoMock.Setup(r => r.GetAllForAnalyticsAsync()).ReturnsAsync(payments);
            _subRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<ExaminerSubscription>());

            var summary = await _sut.GetSummaryAsync();

            // 1 success + 1 failed counted; pending and refunded excluded from both buckets.
            summary.TotalSuccessfulPayments.Should().Be(1);
            summary.TotalFailedPayments.Should().Be(1);
            summary.PaymentFailureRate.Should().Be(0.5d);
        }

        [Test]
        public async Task GetSummaryAsync_GroupsMonthlyRevenueByCompletionMonth()
        {
            var payments = new List<Payment>
            {
                SuccessPayment(100m, PaymentPurpose.ExamCertificationPurchase, new DateTime(2026, 6, 15)),
                SuccessPayment(200m, PaymentPurpose.ExamCertificationPurchase, new DateTime(2026, 6, 20)),
                SuccessPayment(300m, PaymentPurpose.ExamCertificationPurchase, new DateTime(2026, 7, 1)),
            };
            _paymentRepoMock.Setup(r => r.GetAllForAnalyticsAsync()).ReturnsAsync(payments);
            _subRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<ExaminerSubscription>());

            var summary = await _sut.GetSummaryAsync();

            summary.MonthlyRevenue.Should().HaveCount(2);
            summary.MonthlyRevenue[0].Month.Should().Be("2026-06");
            summary.MonthlyRevenue[0].Revenue.Should().Be(300m);
            summary.MonthlyRevenue[1].Month.Should().Be("2026-07");
            summary.MonthlyRevenue[1].Revenue.Should().Be(300m);
        }

        [Test]
        public async Task GetSummaryAsync_PlanDistributionGroupsByStatus()
        {
            _paymentRepoMock.Setup(r => r.GetAllForAnalyticsAsync()).ReturnsAsync(new List<Payment>());
            _subRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<ExaminerSubscription>
            {
                new() { Status = ExaminerSubscriptionStatus.TrialActive },
                new() { Status = ExaminerSubscriptionStatus.TrialActive },
                new() { Status = ExaminerSubscriptionStatus.ProfessionalActive },
                new() { Status = ExaminerSubscriptionStatus.FreeForever },
            });

            var summary = await _sut.GetSummaryAsync();

            summary.ActiveTrialCount.Should().Be(2);
            summary.ActiveProfessionalCount.Should().Be(1);
            summary.ActiveFreeForeverCount.Should().Be(1);
            summary.PlanDistribution.Should().Contain(s => s.Status == "TrialActive" && s.Count == 2);
        }

        [Test]
        public async Task GetSummaryAsync_ConversionRate_ExcludesUndecidedTrialAccounts()
        {
            _paymentRepoMock.Setup(r => r.GetAllForAnalyticsAsync()).ReturnsAsync(new List<Payment>());
            _subRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<ExaminerSubscription>
            {
                new() { Status = ExaminerSubscriptionStatus.TrialActive },   // undecided, excluded
                new() { Status = ExaminerSubscriptionStatus.TrialExhausted }, // undecided, excluded
                new() { Status = ExaminerSubscriptionStatus.ProfessionalActive }, // converted
                new() { Status = ExaminerSubscriptionStatus.FreeForever },   // declined
                new() { Status = ExaminerSubscriptionStatus.FreeForever },   // declined
            });

            var summary = await _sut.GetSummaryAsync();

            // 1 professional out of 3 decided (1 professional + 2 free-forever).
            summary.TrialToPaidConversionRate.Should().BeApproximately(1d / 3d, 0.0001);
        }

        [Test]
        public async Task GetSummaryAsync_NoSubscriptions_ConversionRateIsZeroNotNaN()
        {
            _paymentRepoMock.Setup(r => r.GetAllForAnalyticsAsync()).ReturnsAsync(new List<Payment>());
            _subRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<ExaminerSubscription>());

            var summary = await _sut.GetSummaryAsync();

            summary.TrialToPaidConversionRate.Should().Be(0d);
            summary.PaymentFailureRate.Should().Be(0d);
        }

        [Test]
        public async Task GetSummaryAsync_TopPurchasedExams_RanksByPurchaseCount()
        {
            var examA = new Exam { ExamId = 1, Title = "Java Certification", ExaminerId = 10, CatId = 1,
                DurationMinutes = 60, TotalMarks = 100, PassingMarks = 50, StartTime = DateTime.UtcNow, EndTime = DateTime.UtcNow.AddDays(1), CreatedAt = DateTime.UtcNow };
            var examB = new Exam { ExamId = 2, Title = "Python Certification", ExaminerId = 10, CatId = 1,
                DurationMinutes = 60, TotalMarks = 100, PassingMarks = 50, StartTime = DateTime.UtcNow, EndTime = DateTime.UtcNow.AddDays(1), CreatedAt = DateTime.UtcNow };

            var payments = new List<Payment>
            {
                SuccessPayment(500m, PaymentPurpose.ExamCertificationPurchase, DateTime.UtcNow,
                    purchase: new CandidatePurchase { Exam = examA }),
                SuccessPayment(500m, PaymentPurpose.ExamCertificationPurchase, DateTime.UtcNow,
                    purchase: new CandidatePurchase { Exam = examA }),
                SuccessPayment(300m, PaymentPurpose.ExamCertificationPurchase, DateTime.UtcNow,
                    purchase: new CandidatePurchase { Exam = examB }),
            };
            _paymentRepoMock.Setup(r => r.GetAllForAnalyticsAsync()).ReturnsAsync(payments);
            _subRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<ExaminerSubscription>());

            var summary = await _sut.GetSummaryAsync();

            summary.TopPurchasedExams.Should().HaveCount(2);
            summary.TopPurchasedExams[0].ExamTitle.Should().Be("Java Certification");
            summary.TopPurchasedExams[0].PurchaseCount.Should().Be(2);
            summary.TopPurchasedExams[0].Revenue.Should().Be(1000m);
        }

        [Test]
        public async Task GetSummaryAsync_CouponRedemptions_CountsOnlySuccessfulPaymentsWithCoupon()
        {
            var payments = new List<Payment>
            {
                SuccessPayment(100m, PaymentPurpose.ExamCertificationPurchase, DateTime.UtcNow, couponId: 1),
                SuccessPayment(100m, PaymentPurpose.ExamCertificationPurchase, DateTime.UtcNow, couponId: null),
            };
            _paymentRepoMock.Setup(r => r.GetAllForAnalyticsAsync()).ReturnsAsync(payments);
            _subRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<ExaminerSubscription>());

            var summary = await _sut.GetSummaryAsync();

            summary.CouponRedemptions.Should().Be(1);
        }

        [Test]
        public async Task GetSummaryAsync_RecentTransactions_OrderedNewestFirstAndCappedAtTen()
        {
            var payments = Enumerable.Range(0, 15)
                .Select(i => SuccessPayment(10m * i, PaymentPurpose.ExamCertificationPurchase,
                    DateTime.UtcNow.AddMinutes(-i)))
                .ToList();
            // Reverse InitiatedAt so payment[0] (i=0) has the LATEST timestamp already;
            // set InitiatedAt explicitly to make ordering unambiguous under test.
            for (int i = 0; i < payments.Count; i++)
                payments[i].InitiatedAt = DateTime.UtcNow.AddMinutes(-i);

            _paymentRepoMock.Setup(r => r.GetAllForAnalyticsAsync()).ReturnsAsync(payments);
            _subRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<ExaminerSubscription>());

            var summary = await _sut.GetSummaryAsync();

            summary.RecentTransactions.Should().HaveCount(10);
            summary.RecentTransactions.Should().BeInDescendingOrder(t => t.Date);
        }
    }
}
