// ExaminerSubscriptionServiceTests — the full Part 2 state machine: trial
// creation/grandfathering, the 8-exam boundary, Free Forever's 3-day cooldown
// boundary, the decline-twice-to-Free-Forever chain, the upgrade/payment
// path (including idempotency and the automatic 25%-off offer), and the
// expiry sweep.

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
    public class ExaminerSubscriptionServiceTests
    {
        private Mock<IExaminerSubscriptionRepository> _subRepoMock = null!;
        private Mock<ISubscriptionPlanRepository> _planRepoMock = null!;
        private Mock<IExamRepository> _examRepoMock = null!;
        private Mock<IUserRepository> _userRepoMock = null!;
        private Mock<IPaymentRepository> _paymentRepoMock = null!;
        private Mock<IInvoiceService> _invoiceServiceMock = null!;
        private Mock<ICouponService> _couponServiceMock = null!;
        private Mock<IPaymentGatewayService> _gatewayMock = null!;
        private Mock<ILogger<ExaminerSubscriptionService>> _loggerMock = null!;
        private ExaminerSubscriptionService _sut = null!;

        private static readonly DateTime Now = new(2026, 7, 16, 12, 0, 0, DateTimeKind.Utc);

        private static readonly SubscriptionPlan TrialPlan = new()
        {
            PlanId = 1, Name = "Free Trial", Tier = SubscriptionTier.Trial,
            PriceMonthly = 0m, PriceYearly = 0m, MaxExamsLifetime = 8, ExamCooldownDays = null, IsActive = true
        };
        private static readonly SubscriptionPlan ProfessionalPlan = new()
        {
            PlanId = 2, Name = "Professional", Tier = SubscriptionTier.Professional,
            PriceMonthly = 1499m, PriceYearly = 14990m, MaxExamsLifetime = null, ExamCooldownDays = null, IsActive = true
        };
        private static readonly SubscriptionPlan FreeForeverPlan = new()
        {
            PlanId = 3, Name = "Free Forever", Tier = SubscriptionTier.FreeForever,
            PriceMonthly = 0m, PriceYearly = 0m, MaxExamsLifetime = null, ExamCooldownDays = 3, IsActive = true
        };

        [SetUp]
        public void SetUp()
        {
            _subRepoMock = new Mock<IExaminerSubscriptionRepository>();
            _planRepoMock = new Mock<ISubscriptionPlanRepository>();
            _examRepoMock = new Mock<IExamRepository>();
            _userRepoMock = new Mock<IUserRepository>();
            _paymentRepoMock = new Mock<IPaymentRepository>();
            _invoiceServiceMock = new Mock<IInvoiceService>();
            _couponServiceMock = new Mock<ICouponService>();
            _gatewayMock = new Mock<IPaymentGatewayService>();
            _loggerMock = new Mock<ILogger<ExaminerSubscriptionService>>();

            // Plans already seeded by default — most tests aren't exercising seeding itself.
            _planRepoMock.Setup(r => r.GetByTierAsync(SubscriptionTier.Trial)).ReturnsAsync(TrialPlan);
            _planRepoMock.Setup(r => r.GetByTierAsync(SubscriptionTier.Professional)).ReturnsAsync(ProfessionalPlan);
            _planRepoMock.Setup(r => r.GetByTierAsync(SubscriptionTier.FreeForever)).ReturnsAsync(FreeForeverPlan);
            _planRepoMock.Setup(r => r.AddAsync(It.IsAny<SubscriptionPlan>())).ReturnsAsync((SubscriptionPlan p) => p);
            _planRepoMock.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);

            _subRepoMock.Setup(r => r.AddAsync(It.IsAny<ExaminerSubscription>())).ReturnsAsync((ExaminerSubscription s) => s);
            _subRepoMock.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);

            _examRepoMock.Setup(r => r.GetByExaminerAsync(It.IsAny<int>())).ReturnsAsync(new List<Exam>());

            _sut = new ExaminerSubscriptionService(
                _subRepoMock.Object, _planRepoMock.Object, _examRepoMock.Object, _userRepoMock.Object,
                _paymentRepoMock.Object, _invoiceServiceMock.Object, _couponServiceMock.Object,
                _gatewayMock.Object, _loggerMock.Object);
        }

        private ExaminerSubscription Sub(
            ExaminerSubscriptionStatus status, int trialUsed = 0, int declineCount = 0,
            DateTime? lastFreeExam = null, DateTime? subEnd = null, bool autoRenew = false,
            SubscriptionPlan? plan = null) => new()
        {
            SubscriptionId = 1,
            UserId = 10,
            PlanId = (plan ?? TrialPlan).PlanId,
            Plan = plan ?? TrialPlan,
            Status = status,
            TrialExamsUsed = trialUsed,
            UpgradeDeclineCount = declineCount,
            LastFreeExamCreatedAt = lastFreeExam,
            SubscriptionStartDate = Now.AddDays(-10),
            SubscriptionEndDate = subEnd,
            AutoRenew = autoRenew,
            CreatedAt = Now.AddDays(-10),
            UpdatedAt = Now.AddDays(-10)
        };

        // ════════════════════════════════════════════════════════════════
        //   GetOrCreateAsync
        // ════════════════════════════════════════════════════════════════

        [Test]
        public async Task GetOrCreateAsync_ExistingSubscription_ReturnsItUnchanged()
        {
            var existing = Sub(ExaminerSubscriptionStatus.TrialActive, trialUsed: 3);
            _subRepoMock.Setup(r => r.GetByUserIdAsync(10)).ReturnsAsync(existing);

            var result = await _sut.GetOrCreateAsync(10, Now);

            result.Should().BeSameAs(existing);
            _subRepoMock.Verify(r => r.AddAsync(It.IsAny<ExaminerSubscription>()), Times.Never);
        }

        [Test]
        public async Task GetOrCreateAsync_NewExaminerNoPriorExams_CreatesTrialActive()
        {
            _subRepoMock.Setup(r => r.GetByUserIdAsync(10)).ReturnsAsync((ExaminerSubscription?)null);
            _examRepoMock.Setup(r => r.GetByExaminerAsync(10)).ReturnsAsync(new List<Exam>());

            var result = await _sut.GetOrCreateAsync(10, Now);

            result.Status.Should().Be(ExaminerSubscriptionStatus.TrialActive);
            result.PlanId.Should().Be(TrialPlan.PlanId);
        }

        [Test]
        public async Task GetOrCreateAsync_ExaminerWithPriorExams_GrandfatheredOntoProfessional()
        {
            _subRepoMock.Setup(r => r.GetByUserIdAsync(10)).ReturnsAsync((ExaminerSubscription?)null);
            _examRepoMock.Setup(r => r.GetByExaminerAsync(10))
                .ReturnsAsync(new List<Exam> { TestDataBuilder.BuildDraftExam(1, examinerId: 10) });

            var result = await _sut.GetOrCreateAsync(10, Now);

            result.Status.Should().Be(ExaminerSubscriptionStatus.ProfessionalActive);
            result.PlanId.Should().Be(ProfessionalPlan.PlanId);
        }

        [Test]
        public async Task GetOrCreateAsync_PlansNotSeeded_SeedsAllThree()
        {
            _planRepoMock.Setup(r => r.GetByTierAsync(It.IsAny<SubscriptionTier>())).ReturnsAsync((SubscriptionPlan?)null);
            _subRepoMock.Setup(r => r.GetByUserIdAsync(It.IsAny<int>())).ReturnsAsync((ExaminerSubscription?)null);

            var act = async () => await _sut.GetOrCreateAsync(10, Now);

            // Plans stay null throughout (mock always returns null), so GetOrCreateAsync
            // itself will fail past seeding — this test only verifies seeding was attempted.
            await act.Should().ThrowAsync<InvalidOperationException>();
            _planRepoMock.Verify(r => r.AddAsync(It.Is<SubscriptionPlan>(p => p.Tier == SubscriptionTier.Trial)), Times.Once);
            _planRepoMock.Verify(r => r.AddAsync(It.Is<SubscriptionPlan>(p => p.Tier == SubscriptionTier.Professional)), Times.Once);
            _planRepoMock.Verify(r => r.AddAsync(It.Is<SubscriptionPlan>(p => p.Tier == SubscriptionTier.FreeForever)), Times.Once);
        }

        // ════════════════════════════════════════════════════════════════
        //   CanCreateExamAsync
        // ════════════════════════════════════════════════════════════════

        [Test]
        public async Task CanCreateExamAsync_TrialActiveUnderLimit_Allowed()
        {
            _subRepoMock.Setup(r => r.GetByUserIdAsync(10)).ReturnsAsync(Sub(ExaminerSubscriptionStatus.TrialActive, trialUsed: 7));

            var result = await _sut.CanCreateExamAsync(10, Now);

            result.Allowed.Should().BeTrue();
            result.CanCreatePremiumExams.Should().BeFalse();
        }

        [Test]
        public async Task CanCreateExamAsync_TrialExhausted_NotAllowed()
        {
            _subRepoMock.Setup(r => r.GetByUserIdAsync(10)).ReturnsAsync(Sub(ExaminerSubscriptionStatus.TrialExhausted, trialUsed: 8));

            var result = await _sut.CanCreateExamAsync(10, Now);

            result.Allowed.Should().BeFalse();
            result.Status.Should().Be(ExaminerSubscriptionStatus.TrialExhausted);
        }

        [Test]
        public async Task CanCreateExamAsync_ProfessionalActive_AllowedAndPremiumAllowed()
        {
            _subRepoMock.Setup(r => r.GetByUserIdAsync(10))
                .ReturnsAsync(Sub(ExaminerSubscriptionStatus.ProfessionalActive, plan: ProfessionalPlan));

            var result = await _sut.CanCreateExamAsync(10, Now);

            result.Allowed.Should().BeTrue();
            result.CanCreatePremiumExams.Should().BeTrue();
        }

        [Test]
        public async Task CanCreateExamAsync_GracePeriod_AllowedAndPremiumAllowed()
        {
            _subRepoMock.Setup(r => r.GetByUserIdAsync(10))
                .ReturnsAsync(Sub(ExaminerSubscriptionStatus.GracePeriod, plan: ProfessionalPlan));

            var result = await _sut.CanCreateExamAsync(10, Now);

            result.Allowed.Should().BeTrue();
            result.CanCreatePremiumExams.Should().BeTrue();
        }

        [Test]
        public async Task CanCreateExamAsync_FreeForeverNeverUsed_Allowed()
        {
            _subRepoMock.Setup(r => r.GetByUserIdAsync(10))
                .ReturnsAsync(Sub(ExaminerSubscriptionStatus.FreeForever, plan: FreeForeverPlan, lastFreeExam: null));

            var result = await _sut.CanCreateExamAsync(10, Now);

            result.Allowed.Should().BeTrue();
        }

        [Test]
        public async Task CanCreateExamAsync_FreeForeverInsideCooldown_NotAllowed()
        {
            _subRepoMock.Setup(r => r.GetByUserIdAsync(10))
                .ReturnsAsync(Sub(ExaminerSubscriptionStatus.FreeForever, plan: FreeForeverPlan,
                    lastFreeExam: Now.AddDays(-2).AddSeconds(-1))); // 1 second short of 3 days

            var result = await _sut.CanCreateExamAsync(10, Now);

            result.Allowed.Should().BeFalse();
            result.NextEligibleAt.Should().NotBeNull();
        }

        [Test]
        public async Task CanCreateExamAsync_FreeForeverExactlyAtCooldownBoundary_Allowed()
        {
            _subRepoMock.Setup(r => r.GetByUserIdAsync(10))
                .ReturnsAsync(Sub(ExaminerSubscriptionStatus.FreeForever, plan: FreeForeverPlan,
                    lastFreeExam: Now.AddDays(-3))); // exactly 3 days ago

            var result = await _sut.CanCreateExamAsync(10, Now);

            result.Allowed.Should().BeTrue();
        }

        // ════════════════════════════════════════════════════════════════
        //   RecordExamCreatedAsync
        // ════════════════════════════════════════════════════════════════

        [Test]
        public async Task RecordExamCreatedAsync_TrialActive_IncrementsUsage()
        {
            var sub = Sub(ExaminerSubscriptionStatus.TrialActive, trialUsed: 3);
            _subRepoMock.Setup(r => r.GetByUserIdAsync(10)).ReturnsAsync(sub);

            await _sut.RecordExamCreatedAsync(10, Now);

            sub.TrialExamsUsed.Should().Be(4);
            sub.Status.Should().Be(ExaminerSubscriptionStatus.TrialActive);
        }

        [Test]
        public async Task RecordExamCreatedAsync_EighthExam_FlipsToTrialExhausted()
        {
            var sub = Sub(ExaminerSubscriptionStatus.TrialActive, trialUsed: 7);
            _subRepoMock.Setup(r => r.GetByUserIdAsync(10)).ReturnsAsync(sub);

            await _sut.RecordExamCreatedAsync(10, Now);

            sub.TrialExamsUsed.Should().Be(8);
            sub.Status.Should().Be(ExaminerSubscriptionStatus.TrialExhausted);
        }

        [Test]
        public async Task RecordExamCreatedAsync_FreeForever_SetsLastFreeExamCreatedAt()
        {
            var sub = Sub(ExaminerSubscriptionStatus.FreeForever, plan: FreeForeverPlan, lastFreeExam: null);
            _subRepoMock.Setup(r => r.GetByUserIdAsync(10)).ReturnsAsync(sub);

            await _sut.RecordExamCreatedAsync(10, Now);

            sub.LastFreeExamCreatedAt.Should().Be(Now);
        }

        [Test]
        public async Task RecordExamCreatedAsync_ProfessionalActive_DoesNotTouchCounters()
        {
            var sub = Sub(ExaminerSubscriptionStatus.ProfessionalActive, plan: ProfessionalPlan, trialUsed: 0);
            _subRepoMock.Setup(r => r.GetByUserIdAsync(10)).ReturnsAsync(sub);

            await _sut.RecordExamCreatedAsync(10, Now);

            sub.TrialExamsUsed.Should().Be(0);
            sub.LastFreeExamCreatedAt.Should().BeNull();
        }

        // ════════════════════════════════════════════════════════════════
        //   DeclineUpgradeAsync
        // ════════════════════════════════════════════════════════════════

        [Test]
        public async Task DeclineUpgradeAsync_NotTrialExhausted_Throws()
        {
            _subRepoMock.Setup(r => r.GetByUserIdAsync(10)).ReturnsAsync(Sub(ExaminerSubscriptionStatus.TrialActive));

            var act = async () => await _sut.DeclineUpgradeAsync(10, Now);

            await act.Should().ThrowAsync<ValidationException>();
        }

        [Test]
        public async Task DeclineUpgradeAsync_FirstDecline_StaysExhaustedWithCountOne()
        {
            var sub = Sub(ExaminerSubscriptionStatus.TrialExhausted, trialUsed: 8, declineCount: 0);
            _subRepoMock.Setup(r => r.GetByUserIdAsync(10)).ReturnsAsync(sub);

            var count = await _sut.DeclineUpgradeAsync(10, Now);

            count.Should().Be(1);
            sub.Status.Should().Be(ExaminerSubscriptionStatus.TrialExhausted);
        }

        [Test]
        public async Task DeclineUpgradeAsync_SecondDecline_MovesToFreeForever()
        {
            var sub = Sub(ExaminerSubscriptionStatus.TrialExhausted, trialUsed: 8, declineCount: 1);
            _subRepoMock.Setup(r => r.GetByUserIdAsync(10)).ReturnsAsync(sub);

            var count = await _sut.DeclineUpgradeAsync(10, Now);

            count.Should().Be(2);
            sub.Status.Should().Be(ExaminerSubscriptionStatus.FreeForever);
            sub.PlanId.Should().Be(FreeForeverPlan.PlanId);
            sub.LastFreeExamCreatedAt.Should().BeNull();
        }

        // ════════════════════════════════════════════════════════════════
        //   UpgradeAsync
        // ════════════════════════════════════════════════════════════════

        private void SetUpUser(int userId = 10) =>
            _userRepoMock.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(TestDataBuilder.BuildExaminer(userId));

        [Test]
        public async Task UpgradeAsync_RepeatedIdempotencyKey_ReturnsOriginalPaymentWithoutCharging()
        {
            var existingPayment = new Payment { PaymentId = 5, Status = PaymentStatus.Success, IdempotencyKey = "key-1" };
            _paymentRepoMock.Setup(r => r.GetByIdempotencyKeyAsync("key-1")).ReturnsAsync(existingPayment);

            var result = await _sut.UpgradeAsync(10, PaymentMethod.CreditCard, "4242424242424242",
                annual: false, couponCode: null, idempotencyKey: "key-1",
                simulateNetworkFailure: false, simulateTimeout: false, Now);

            result.Should().BeSameAs(existingPayment);
            _gatewayMock.Verify(g => g.Charge(It.IsAny<PaymentChargeRequest>(), It.IsAny<Random>()), Times.Never);
        }

        [Test]
        public async Task UpgradeAsync_SuccessfulCharge_ActivatesProfessionalAndGeneratesInvoice()
        {
            var sub = Sub(ExaminerSubscriptionStatus.TrialExhausted, trialUsed: 8, declineCount: 0);
            _subRepoMock.Setup(r => r.GetByUserIdAsync(10)).ReturnsAsync(sub);
            _paymentRepoMock.Setup(r => r.GetByIdempotencyKeyAsync(It.IsAny<string>())).ReturnsAsync((Payment?)null);
            _paymentRepoMock.Setup(r => r.AddAsync(It.IsAny<Payment>())).ReturnsAsync((Payment p) => { p.PaymentId = 1; return p; });
            _paymentRepoMock.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);
            _gatewayMock.Setup(g => g.Charge(It.IsAny<PaymentChargeRequest>(), null))
                .Returns(new PaymentChargeOutcome(PaymentStatus.Success, null));
            SetUpUser();

            var payment = await _sut.UpgradeAsync(10, PaymentMethod.CreditCard, "4242424242424242",
                annual: false, couponCode: null, idempotencyKey: "key-2",
                simulateNetworkFailure: false, simulateTimeout: false, Now);

            payment.Status.Should().Be(PaymentStatus.Success);
            payment.Amount.Should().Be(ProfessionalPlan.PriceMonthly); // no discount, decline count 0
            sub.Status.Should().Be(ExaminerSubscriptionStatus.ProfessionalActive);
            sub.PlanId.Should().Be(ProfessionalPlan.PlanId);
            _invoiceServiceMock.Verify(s => s.GenerateAsync(It.IsAny<InvoiceRequest>()), Times.Once);
        }

        [Test]
        public async Task UpgradeAsync_AtDeclineCountOne_AppliesAutomatic25PercentDiscount()
        {
            var sub = Sub(ExaminerSubscriptionStatus.TrialExhausted, trialUsed: 8, declineCount: 1);
            _subRepoMock.Setup(r => r.GetByUserIdAsync(10)).ReturnsAsync(sub);
            _paymentRepoMock.Setup(r => r.GetByIdempotencyKeyAsync(It.IsAny<string>())).ReturnsAsync((Payment?)null);
            _paymentRepoMock.Setup(r => r.AddAsync(It.IsAny<Payment>())).ReturnsAsync((Payment p) => { p.PaymentId = 1; return p; });
            _paymentRepoMock.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);
            _gatewayMock.Setup(g => g.Charge(It.IsAny<PaymentChargeRequest>(), null))
                .Returns(new PaymentChargeOutcome(PaymentStatus.Success, null));
            SetUpUser();

            var payment = await _sut.UpgradeAsync(10, PaymentMethod.CreditCard, "4242424242424242",
                annual: false, couponCode: null, idempotencyKey: "key-3",
                simulateNetworkFailure: false, simulateTimeout: false, Now);

            payment.Amount.Should().Be(ProfessionalPlan.PriceMonthly * 0.75m);
            sub.DiscountAppliedPercent.Should().Be(25);
            _couponServiceMock.Verify(c => c.ValidateAsync(
                It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CouponScope>(), It.IsAny<DateTime>()), Times.Never);
        }

        [Test]
        public async Task UpgradeAsync_FailedCharge_DoesNotActivateSubscription()
        {
            var sub = Sub(ExaminerSubscriptionStatus.TrialExhausted, trialUsed: 8, declineCount: 0);
            _subRepoMock.Setup(r => r.GetByUserIdAsync(10)).ReturnsAsync(sub);
            _paymentRepoMock.Setup(r => r.GetByIdempotencyKeyAsync(It.IsAny<string>())).ReturnsAsync((Payment?)null);
            _paymentRepoMock.Setup(r => r.AddAsync(It.IsAny<Payment>())).ReturnsAsync((Payment p) => { p.PaymentId = 1; return p; });
            _paymentRepoMock.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);
            _gatewayMock.Setup(g => g.Charge(It.IsAny<PaymentChargeRequest>(), null))
                .Returns(new PaymentChargeOutcome(PaymentStatus.Failed, "Card declined."));
            SetUpUser();

            var payment = await _sut.UpgradeAsync(10, PaymentMethod.CreditCard, "4000000000000002",
                annual: false, couponCode: null, idempotencyKey: "key-4",
                simulateNetworkFailure: false, simulateTimeout: false, Now);

            payment.Status.Should().Be(PaymentStatus.Failed);
            sub.Status.Should().Be(ExaminerSubscriptionStatus.TrialExhausted); // unchanged
            _invoiceServiceMock.Verify(s => s.GenerateAsync(It.IsAny<InvoiceRequest>()), Times.Never);
        }

        [Test]
        public async Task UpgradeAsync_InvalidCoupon_Throws()
        {
            var sub = Sub(ExaminerSubscriptionStatus.TrialExhausted, trialUsed: 8, declineCount: 0);
            _subRepoMock.Setup(r => r.GetByUserIdAsync(10)).ReturnsAsync(sub);
            _paymentRepoMock.Setup(r => r.GetByIdempotencyKeyAsync(It.IsAny<string>())).ReturnsAsync((Payment?)null);
            _couponServiceMock.Setup(c => c.ValidateAsync("BADCODE", 10, CouponScope.SubscriptionOnly, Now))
                .ReturnsAsync(new CouponValidationResult(false, null, 0, "Coupon code not found."));

            var act = async () => await _sut.UpgradeAsync(10, PaymentMethod.CreditCard, "4242424242424242",
                annual: false, couponCode: "BADCODE", idempotencyKey: "key-5",
                simulateNetworkFailure: false, simulateTimeout: false, Now);

            await act.Should().ThrowAsync<InvalidCouponException>();
        }

        // ════════════════════════════════════════════════════════════════
        //   GetBillingHistoryAsync
        // ════════════════════════════════════════════════════════════════

        [Test]
        public async Task GetBillingHistoryAsync_DelegatesToPaymentRepository()
        {
            var payments = new List<Payment> { new() { PaymentId = 1, UserId = 10 } };
            _paymentRepoMock.Setup(r => r.GetByUserIdAsync(10)).ReturnsAsync(payments);

            var result = await _sut.GetBillingHistoryAsync(10);

            result.Should().BeEquivalentTo(payments);
        }

        // ════════════════════════════════════════════════════════════════
        //   SweepExpiredSubscriptionsAsync
        // ════════════════════════════════════════════════════════════════

        [Test]
        public async Task Sweep_ProfessionalPastEndDateNoAutoRenew_MovesToGracePeriod()
        {
            var sub = Sub(ExaminerSubscriptionStatus.ProfessionalActive, plan: ProfessionalPlan,
                subEnd: Now.AddDays(-1), autoRenew: false);
            _subRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<ExaminerSubscription> { sub });

            var affected = await _sut.SweepExpiredSubscriptionsAsync(Now);

            affected.Should().Be(1);
            sub.Status.Should().Be(ExaminerSubscriptionStatus.GracePeriod);
        }

        [Test]
        public async Task Sweep_ProfessionalWithAutoRenew_NeverSwept()
        {
            var sub = Sub(ExaminerSubscriptionStatus.ProfessionalActive, plan: ProfessionalPlan,
                subEnd: Now.AddDays(-1), autoRenew: true);
            _subRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<ExaminerSubscription> { sub });

            var affected = await _sut.SweepExpiredSubscriptionsAsync(Now);

            affected.Should().Be(0);
            sub.Status.Should().Be(ExaminerSubscriptionStatus.ProfessionalActive);
        }

        [Test]
        public async Task Sweep_GracePeriodPastGraceWindow_MovesToFreeForever()
        {
            var sub = Sub(ExaminerSubscriptionStatus.GracePeriod, plan: ProfessionalPlan,
                subEnd: Now.AddDays(-10)); // 10 days past end -> well past the 3-day grace
            _subRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<ExaminerSubscription> { sub });

            var affected = await _sut.SweepExpiredSubscriptionsAsync(Now);

            affected.Should().Be(1);
            sub.Status.Should().Be(ExaminerSubscriptionStatus.FreeForever);
            sub.PlanId.Should().Be(FreeForeverPlan.PlanId);
        }

        [Test]
        public async Task Sweep_NothingToTransition_ReturnsZero()
        {
            var sub = Sub(ExaminerSubscriptionStatus.TrialActive, trialUsed: 2);
            _subRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<ExaminerSubscription> { sub });

            var affected = await _sut.SweepExpiredSubscriptionsAsync(Now);

            affected.Should().Be(0);
        }
    }
}
