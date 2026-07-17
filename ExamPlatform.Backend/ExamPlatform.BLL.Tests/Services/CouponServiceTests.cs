// CouponServiceTests — every rejection reason in Part 10's coupon validation
// matrix (not found, inactive, expired, scope mismatch, redemption limit,
// already redeemed by this user), plus the happy path and redemption bookkeeping.

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
    public class CouponServiceTests
    {
        private Mock<ICouponRepository> _couponRepoMock = null!;
        private Mock<ICouponRedemptionRepository> _redemptionRepoMock = null!;
        private Mock<ILogger<CouponService>> _loggerMock = null!;
        private CouponService _sut = null!;
        private static readonly DateTime Now = new(2026, 7, 16, 12, 0, 0, DateTimeKind.Utc);

        private static Coupon ValidCoupon() => new()
        {
            CouponId = 1,
            Code = "SAVE25",
            DiscountPercent = 25,
            MaxRedemptions = 100,
            TimesRedeemed = 0,
            ValidFrom = Now.AddDays(-1),
            ValidTo = Now.AddDays(30),
            IsActive = true,
            AppliesTo = CouponScope.Both
        };

        [SetUp]
        public void SetUp()
        {
            _couponRepoMock = new Mock<ICouponRepository>();
            _redemptionRepoMock = new Mock<ICouponRedemptionRepository>();
            _loggerMock = new Mock<ILogger<CouponService>>();
            _sut = new CouponService(_couponRepoMock.Object, _redemptionRepoMock.Object, _loggerMock.Object);
        }

        [Test]
        public async Task ValidateAsync_ValidCoupon_ReturnsValid()
        {
            _couponRepoMock.Setup(r => r.GetByCodeAsync("SAVE25")).ReturnsAsync(ValidCoupon());
            _redemptionRepoMock.Setup(r => r.ExistsForCouponAndUserAsync(1, 5)).ReturnsAsync(false);

            var result = await _sut.ValidateAsync("save25", 5, CouponScope.ExamPurchaseOnly, Now);

            result.IsValid.Should().BeTrue();
            result.CouponId.Should().Be(1);
            result.DiscountPercent.Should().Be(25);
        }

        [Test]
        public async Task ValidateAsync_UnknownCode_ReturnsNotFound()
        {
            _couponRepoMock.Setup(r => r.GetByCodeAsync(It.IsAny<string>())).ReturnsAsync((Coupon?)null);

            var result = await _sut.ValidateAsync("NOPE", 5, CouponScope.Both, Now);

            result.IsValid.Should().BeFalse();
            result.RejectionReason.Should().Contain("not found");
        }

        [Test]
        public async Task ValidateAsync_InactiveCoupon_Rejected()
        {
            var coupon = ValidCoupon();
            coupon.IsActive = false;
            _couponRepoMock.Setup(r => r.GetByCodeAsync("SAVE25")).ReturnsAsync(coupon);

            var result = await _sut.ValidateAsync("SAVE25", 5, CouponScope.Both, Now);

            result.IsValid.Should().BeFalse();
            result.RejectionReason.Should().Contain("no longer active");
        }

        [Test]
        public async Task ValidateAsync_ExpiredCoupon_Rejected()
        {
            var coupon = ValidCoupon();
            coupon.ValidTo = Now.AddDays(-1);
            _couponRepoMock.Setup(r => r.GetByCodeAsync("SAVE25")).ReturnsAsync(coupon);

            var result = await _sut.ValidateAsync("SAVE25", 5, CouponScope.Both, Now);

            result.IsValid.Should().BeFalse();
            result.RejectionReason.Should().Contain("expired");
        }

        [Test]
        public async Task ValidateAsync_NotYetValid_Rejected()
        {
            var coupon = ValidCoupon();
            coupon.ValidFrom = Now.AddDays(1);
            _couponRepoMock.Setup(r => r.GetByCodeAsync("SAVE25")).ReturnsAsync(coupon);

            var result = await _sut.ValidateAsync("SAVE25", 5, CouponScope.Both, Now);

            result.IsValid.Should().BeFalse();
        }

        [Test]
        public async Task ValidateAsync_ScopeMismatch_Rejected()
        {
            var coupon = ValidCoupon();
            coupon.AppliesTo = CouponScope.SubscriptionOnly;
            _couponRepoMock.Setup(r => r.GetByCodeAsync("SAVE25")).ReturnsAsync(coupon);

            var result = await _sut.ValidateAsync("SAVE25", 5, CouponScope.ExamPurchaseOnly, Now);

            result.IsValid.Should().BeFalse();
            result.RejectionReason.Should().Contain("cannot be used");
        }

        [Test]
        public async Task ValidateAsync_ScopeBoth_AllowsAnyRequiredScope()
        {
            var coupon = ValidCoupon(); // AppliesTo = Both
            _couponRepoMock.Setup(r => r.GetByCodeAsync("SAVE25")).ReturnsAsync(coupon);
            _redemptionRepoMock.Setup(r => r.ExistsForCouponAndUserAsync(It.IsAny<int>(), It.IsAny<int>())).ReturnsAsync(false);

            var result = await _sut.ValidateAsync("SAVE25", 5, CouponScope.SubscriptionOnly, Now);

            result.IsValid.Should().BeTrue();
        }

        [Test]
        public async Task ValidateAsync_MaxRedemptionsReached_Rejected()
        {
            var coupon = ValidCoupon();
            coupon.MaxRedemptions = 10;
            coupon.TimesRedeemed = 10;
            _couponRepoMock.Setup(r => r.GetByCodeAsync("SAVE25")).ReturnsAsync(coupon);

            var result = await _sut.ValidateAsync("SAVE25", 5, CouponScope.Both, Now);

            result.IsValid.Should().BeFalse();
            result.RejectionReason.Should().Contain("redemption limit");
        }

        [Test]
        public async Task ValidateAsync_UnlimitedRedemptions_NeverRejectedOnCount()
        {
            var coupon = ValidCoupon();
            coupon.MaxRedemptions = null;
            coupon.TimesRedeemed = 999999;
            _couponRepoMock.Setup(r => r.GetByCodeAsync("SAVE25")).ReturnsAsync(coupon);
            _redemptionRepoMock.Setup(r => r.ExistsForCouponAndUserAsync(It.IsAny<int>(), It.IsAny<int>())).ReturnsAsync(false);

            var result = await _sut.ValidateAsync("SAVE25", 5, CouponScope.Both, Now);

            result.IsValid.Should().BeTrue();
        }

        [Test]
        public async Task ValidateAsync_AlreadyRedeemedByThisUser_Rejected()
        {
            var coupon = ValidCoupon();
            _couponRepoMock.Setup(r => r.GetByCodeAsync("SAVE25")).ReturnsAsync(coupon);
            _redemptionRepoMock.Setup(r => r.ExistsForCouponAndUserAsync(1, 5)).ReturnsAsync(true);

            var result = await _sut.ValidateAsync("SAVE25", 5, CouponScope.Both, Now);

            result.IsValid.Should().BeFalse();
            result.RejectionReason.Should().Contain("already used");
        }

        [Test]
        public async Task RedeemAsync_IncrementsTimesRedeemedAndRecordsRedemption()
        {
            _couponRepoMock.Setup(r => r.TryIncrementRedemptionAsync(1)).ReturnsAsync(true);
            _redemptionRepoMock.Setup(r => r.AddAsync(It.IsAny<CouponRedemption>()))
                .ReturnsAsync((CouponRedemption r) => r);
            _redemptionRepoMock.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);

            await _sut.RedeemAsync(1, 5, paymentId: 99, Now);

            _couponRepoMock.Verify(r => r.TryIncrementRedemptionAsync(1), Times.Once);
            _redemptionRepoMock.Verify(r => r.AddAsync(It.Is<CouponRedemption>(
                red => red.CouponId == 1 && red.UserId == 5 && red.PaymentId == 99)), Times.Once);
        }

        [Test]
        public async Task RedeemAsync_CapAlreadyReachedByConcurrentRequest_DoesNotThrowAndStillRecordsRedemption()
        {
            // TryIncrementRedemptionAsync returning false simulates a concurrent
            // request having just exhausted MaxRedemptions. The payment for
            // THIS user already succeeded — RedeemAsync must not throw, and
            // must still record that this user used the code (so they can't
            // redeem it a second time).
            _couponRepoMock.Setup(r => r.TryIncrementRedemptionAsync(1)).ReturnsAsync(false);
            _redemptionRepoMock.Setup(r => r.AddAsync(It.IsAny<CouponRedemption>()))
                .ReturnsAsync((CouponRedemption r) => r);
            _redemptionRepoMock.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);

            var act = async () => await _sut.RedeemAsync(1, 5, paymentId: 99, Now);

            await act.Should().NotThrowAsync();
            _redemptionRepoMock.Verify(r => r.AddAsync(It.Is<CouponRedemption>(
                red => red.CouponId == 1 && red.UserId == 5)), Times.Once);
        }
    }
}
