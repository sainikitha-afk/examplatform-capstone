// CandidatePurchaseServiceTests — HasActivePurchaseAsync, the full purchase
// flow (idempotency, duplicate-purchase guard, coupon path, success/failure
// outcomes), and the simulated refund.

using ExamPlatform.BLL.Exceptions;
using ExamPlatform.BLL.Interfaces;
using ExamPlatform.BLL.Models;
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
    public class CandidatePurchaseServiceTests
    {
        private Mock<ICandidatePurchaseRepository> _purchaseRepoMock = null!;
        private Mock<IExamRepository> _examRepoMock = null!;
        private Mock<IUserRepository> _userRepoMock = null!;
        private Mock<IPaymentRepository> _paymentRepoMock = null!;
        private Mock<IInvoiceService> _invoiceServiceMock = null!;
        private Mock<ICouponService> _couponServiceMock = null!;
        private Mock<IPaymentGatewayService> _gatewayMock = null!;
        private Mock<ILogger<CandidatePurchaseService>> _loggerMock = null!;
        private CandidatePurchaseService _sut = null!;

        private static readonly DateTime Now = new(2026, 7, 16, 12, 0, 0, DateTimeKind.Utc);

        private static Exam PremiumExam(int examId = 1, decimal price = 999m) => new()
        {
            ExamId = examId, ExaminerId = 10, CatId = 1, Title = "Premium Exam",
            DurationMinutes = 60, TotalMarks = 100m, PassingMarks = 50m,
            StartTime = Now.AddDays(-1), EndTime = Now.AddDays(30),
            ExamStatus = ExamStatus.Published, CreatedAt = Now.AddDays(-1),
            IsPremium = true, Price = price
        };

        [SetUp]
        public void SetUp()
        {
            _purchaseRepoMock = new Mock<ICandidatePurchaseRepository>();
            _examRepoMock = new Mock<IExamRepository>();
            _userRepoMock = new Mock<IUserRepository>();
            _paymentRepoMock = new Mock<IPaymentRepository>();
            _invoiceServiceMock = new Mock<IInvoiceService>();
            _couponServiceMock = new Mock<ICouponService>();
            _gatewayMock = new Mock<IPaymentGatewayService>();
            _loggerMock = new Mock<ILogger<CandidatePurchaseService>>();

            _paymentRepoMock.Setup(r => r.AddAsync(It.IsAny<Payment>())).ReturnsAsync((Payment p) => { p.PaymentId = 1; return p; });
            _paymentRepoMock.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);
            _purchaseRepoMock.Setup(r => r.AddAsync(It.IsAny<CandidatePurchase>())).ReturnsAsync((CandidatePurchase p) => p);
            _purchaseRepoMock.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);
            _userRepoMock.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(TestDataBuilder.BuildCandidate(5));

            _sut = new CandidatePurchaseService(
                _purchaseRepoMock.Object, _examRepoMock.Object, _userRepoMock.Object,
                _paymentRepoMock.Object, _invoiceServiceMock.Object, _couponServiceMock.Object,
                _gatewayMock.Object, _loggerMock.Object);
        }

        // ════════════════════════════════════════════════════════════════
        //   HasActivePurchaseAsync
        // ════════════════════════════════════════════════════════════════

        [Test]
        public async Task HasActivePurchaseAsync_NoPurchase_ReturnsFalse()
        {
            _purchaseRepoMock.Setup(r => r.GetByUserAndExamAsync(5, 1)).ReturnsAsync((CandidatePurchase?)null);

            (await _sut.HasActivePurchaseAsync(5, 1)).Should().BeFalse();
        }

        [Test]
        public async Task HasActivePurchaseAsync_RefundedPurchase_ReturnsFalse()
        {
            _purchaseRepoMock.Setup(r => r.GetByUserAndExamAsync(5, 1))
                .ReturnsAsync(new CandidatePurchase { Status = CandidatePurchaseStatus.Refunded });

            (await _sut.HasActivePurchaseAsync(5, 1)).Should().BeFalse();
        }

        [Test]
        public async Task HasActivePurchaseAsync_ActivePurchase_ReturnsTrue()
        {
            _purchaseRepoMock.Setup(r => r.GetByUserAndExamAsync(5, 1))
                .ReturnsAsync(new CandidatePurchase { Status = CandidatePurchaseStatus.Active });

            (await _sut.HasActivePurchaseAsync(5, 1)).Should().BeTrue();
        }

        // ════════════════════════════════════════════════════════════════
        //   PurchaseAsync
        // ════════════════════════════════════════════════════════════════

        [Test]
        public async Task PurchaseAsync_RepeatedIdempotencyKey_ReturnsOriginalWithoutCharging()
        {
            var existing = new Payment { PaymentId = 9, Status = PaymentStatus.Success, IdempotencyKey = "k1" };
            _paymentRepoMock.Setup(r => r.GetByIdempotencyKeyAsync("k1")).ReturnsAsync(existing);

            var result = await _sut.PurchaseAsync(5, 1, PaymentMethod.CreditCard, "4242424242424242",
                null, "k1", false, false, Now);

            result.Should().BeSameAs(existing);
            _gatewayMock.Verify(g => g.Charge(It.IsAny<PaymentChargeRequest>(), It.IsAny<Random>()), Times.Never);
        }

        [Test]
        public async Task PurchaseAsync_ExamNotPremium_ThrowsValidationException()
        {
            var freeExam = PremiumExam();
            freeExam.IsPremium = false;
            freeExam.Price = null;
            _paymentRepoMock.Setup(r => r.GetByIdempotencyKeyAsync(It.IsAny<string>())).ReturnsAsync((Payment?)null);
            _examRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(freeExam);

            var act = async () => await _sut.PurchaseAsync(5, 1, PaymentMethod.CreditCard, "4242424242424242",
                null, "k2", false, false, Now);

            await act.Should().ThrowAsync<ValidationException>();
        }

        [Test]
        public async Task PurchaseAsync_AlreadyActivePurchase_ThrowsDuplicatePurchaseException()
        {
            _paymentRepoMock.Setup(r => r.GetByIdempotencyKeyAsync(It.IsAny<string>())).ReturnsAsync((Payment?)null);
            _examRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(PremiumExam());
            _purchaseRepoMock.Setup(r => r.GetByUserAndExamAsync(5, 1))
                .ReturnsAsync(new CandidatePurchase { Status = CandidatePurchaseStatus.Active });

            var act = async () => await _sut.PurchaseAsync(5, 1, PaymentMethod.CreditCard, "4242424242424242",
                null, "k3", false, false, Now);

            await act.Should().ThrowAsync<DuplicatePurchaseException>();
        }

        [Test]
        public async Task PurchaseAsync_PreviouslyRefunded_ReusesExistingRowInsteadOfInserting()
        {
            // Regression test: CandidatePurchase has a DB-unique index on
            // (UserId, ExamId) with no status qualifier (it must fire for
            // BOTH Active and Refunded rows to block concurrent duplicate
            // purchases). A naive AddAsync of a second row for the same
            // (user, exam) after a refund would violate that constraint and
            // crash with an unhandled 500 in production — even though this
            // mock-based test wouldn't itself catch a DB constraint, we can
            // still assert the service takes the reuse path (Update, not
            // AddAsync) that avoids ever attempting the illegal insert.
            var refundedRow = new CandidatePurchase
            {
                PurchaseId = 77, UserId = 5, ExamId = 1, PaymentId = 41,
                Status = CandidatePurchaseStatus.Refunded, PurchasedAt = Now.AddDays(-10)
            };
            _paymentRepoMock.Setup(r => r.GetByIdempotencyKeyAsync(It.IsAny<string>())).ReturnsAsync((Payment?)null);
            _examRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(PremiumExam());
            _purchaseRepoMock.Setup(r => r.GetByUserAndExamAsync(5, 1)).ReturnsAsync(refundedRow);
            _gatewayMock.Setup(g => g.Charge(It.IsAny<PaymentChargeRequest>(), null))
                .Returns(new PaymentChargeOutcome(PaymentStatus.Success, null));

            var payment = await _sut.PurchaseAsync(5, 1, PaymentMethod.CreditCard, "4242424242424242",
                null, "k4", false, false, Now);

            payment.Status.Should().Be(PaymentStatus.Success);
            _purchaseRepoMock.Verify(r => r.AddAsync(It.IsAny<CandidatePurchase>()), Times.Never);
            _purchaseRepoMock.Verify(r => r.Update(It.Is<CandidatePurchase>(
                p => p.PurchaseId == 77 && p.Status == CandidatePurchaseStatus.Active)), Times.Once);
        }

        [Test]
        public async Task PurchaseAsync_SuccessfulCharge_GrantsAccessAndGeneratesInvoice()
        {
            _paymentRepoMock.Setup(r => r.GetByIdempotencyKeyAsync(It.IsAny<string>())).ReturnsAsync((Payment?)null);
            _examRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(PremiumExam(price: 999m));
            _purchaseRepoMock.Setup(r => r.GetByUserAndExamAsync(5, 1)).ReturnsAsync((CandidatePurchase?)null);
            _gatewayMock.Setup(g => g.Charge(It.IsAny<PaymentChargeRequest>(), null))
                .Returns(new PaymentChargeOutcome(PaymentStatus.Success, null));

            var payment = await _sut.PurchaseAsync(5, 1, PaymentMethod.CreditCard, "4242424242424242",
                null, "k5", false, false, Now);

            payment.Status.Should().Be(PaymentStatus.Success);
            payment.Amount.Should().Be(999m);
            _purchaseRepoMock.Verify(r => r.AddAsync(It.Is<CandidatePurchase>(
                p => p.UserId == 5 && p.ExamId == 1 && p.Status == CandidatePurchaseStatus.Active)), Times.Once);
            _invoiceServiceMock.Verify(s => s.GenerateAsync(It.IsAny<InvoiceRequest>()), Times.Once);
        }

        [Test]
        public async Task PurchaseAsync_FailedCharge_DoesNotGrantAccess()
        {
            _paymentRepoMock.Setup(r => r.GetByIdempotencyKeyAsync(It.IsAny<string>())).ReturnsAsync((Payment?)null);
            _examRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(PremiumExam());
            _purchaseRepoMock.Setup(r => r.GetByUserAndExamAsync(5, 1)).ReturnsAsync((CandidatePurchase?)null);
            _gatewayMock.Setup(g => g.Charge(It.IsAny<PaymentChargeRequest>(), null))
                .Returns(new PaymentChargeOutcome(PaymentStatus.Failed, "Card declined."));

            var payment = await _sut.PurchaseAsync(5, 1, PaymentMethod.CreditCard, "4000000000000002",
                null, "k6", false, false, Now);

            payment.Status.Should().Be(PaymentStatus.Failed);
            _purchaseRepoMock.Verify(r => r.AddAsync(It.IsAny<CandidatePurchase>()), Times.Never);
            _invoiceServiceMock.Verify(s => s.GenerateAsync(It.IsAny<InvoiceRequest>()), Times.Never);
        }

        [Test]
        public async Task PurchaseAsync_ValidCoupon_AppliesDiscount()
        {
            _paymentRepoMock.Setup(r => r.GetByIdempotencyKeyAsync(It.IsAny<string>())).ReturnsAsync((Payment?)null);
            _examRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(PremiumExam(price: 1000m));
            _purchaseRepoMock.Setup(r => r.GetByUserAndExamAsync(5, 1)).ReturnsAsync((CandidatePurchase?)null);
            _couponServiceMock.Setup(c => c.ValidateAsync("SAVE10", 5, CouponScope.ExamPurchaseOnly, Now))
                .ReturnsAsync(new CouponValidationResult(true, 7, 10, null));
            _gatewayMock.Setup(g => g.Charge(It.IsAny<PaymentChargeRequest>(), null))
                .Returns(new PaymentChargeOutcome(PaymentStatus.Success, null));

            var payment = await _sut.PurchaseAsync(5, 1, PaymentMethod.CreditCard, "4242424242424242",
                "SAVE10", "k7", false, false, Now);

            payment.Amount.Should().Be(900m); // 1000 - 10%
            _couponServiceMock.Verify(c => c.RedeemAsync(7, 5, payment.PaymentId, Now), Times.Once);
        }

        [Test]
        public async Task PurchaseAsync_InvalidCoupon_Throws()
        {
            _paymentRepoMock.Setup(r => r.GetByIdempotencyKeyAsync(It.IsAny<string>())).ReturnsAsync((Payment?)null);
            _examRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(PremiumExam());
            _purchaseRepoMock.Setup(r => r.GetByUserAndExamAsync(5, 1)).ReturnsAsync((CandidatePurchase?)null);
            _couponServiceMock.Setup(c => c.ValidateAsync("BAD", 5, CouponScope.ExamPurchaseOnly, Now))
                .ReturnsAsync(new CouponValidationResult(false, null, 0, "Coupon code not found."));

            var act = async () => await _sut.PurchaseAsync(5, 1, PaymentMethod.CreditCard, "4242424242424242",
                "BAD", "k8", false, false, Now);

            await act.Should().ThrowAsync<InvalidCouponException>();
        }

        // ════════════════════════════════════════════════════════════════
        //   GetMyPurchasesAsync / RefundAsync
        // ════════════════════════════════════════════════════════════════

        [Test]
        public async Task GetMyPurchasesAsync_DelegatesToRepository()
        {
            var purchases = new List<CandidatePurchase> { new() { PurchaseId = 1, UserId = 5 } };
            _purchaseRepoMock.Setup(r => r.GetByUserIdAsync(5)).ReturnsAsync(purchases);

            var result = await _sut.GetMyPurchasesAsync(5);

            result.Should().BeEquivalentTo(purchases);
        }

        [Test]
        public async Task RefundAsync_ActivePurchase_FlipsPurchaseAndPaymentToRefunded()
        {
            var purchase = new CandidatePurchase { PurchaseId = 1, PaymentId = 2, Status = CandidatePurchaseStatus.Active };
            var payment = new Payment { PaymentId = 2, Status = PaymentStatus.Success };
            _purchaseRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(purchase);
            _paymentRepoMock.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(payment);

            await _sut.RefundAsync(1, Now);

            purchase.Status.Should().Be(CandidatePurchaseStatus.Refunded);
            payment.Status.Should().Be(PaymentStatus.Refunded);
        }

        [Test]
        public async Task RefundAsync_AlreadyRefunded_IsIdempotent()
        {
            var purchase = new CandidatePurchase { PurchaseId = 1, PaymentId = 2, Status = CandidatePurchaseStatus.Refunded };
            _purchaseRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(purchase);

            await _sut.RefundAsync(1, Now);

            _purchaseRepoMock.Verify(r => r.Update(It.IsAny<CandidatePurchase>()), Times.Never);
        }

        [Test]
        public async Task RefundAsync_PurchaseNotFound_ThrowsValidationException()
        {
            _purchaseRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((CandidatePurchase?)null);

            var act = async () => await _sut.RefundAsync(999, Now);

            await act.Should().ThrowAsync<ValidationException>();
        }
    }
}
