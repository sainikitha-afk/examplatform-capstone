// PaymentGatewayServiceTests — covers every demo card outcome, the explicit
// network-failure/timeout simulation flags, invalid input, and the weighted
// random branch (forced deterministic via a fixed-roll Random subclass).

using ExamPlatform.BLL.Models;
using ExamPlatform.BLL.Services;
using ExamPlatform.Domain.Enums;
using FluentAssertions;
using NUnit.Framework;

namespace ExamPlatform.BLL.Tests.Services
{
    [TestFixture]
    public class PaymentGatewayServiceTests
    {
        private PaymentGatewayService _sut = null!;

        [SetUp]
        public void SetUp()
        {
            _sut = new PaymentGatewayService();
        }

        // Random.Next(int) is virtual since .NET Core — overriding it is the
        // standard way to force a deterministic roll in a test.
        private class FixedRandom : Random
        {
            private readonly int _value;
            public FixedRandom(int value) { _value = value; }
            public override int Next(int maxValue) => _value;
        }

        private static PaymentChargeRequest Request(string card, bool networkFailure = false, bool timeout = false) =>
            new(Amount: 999m, Method: PaymentMethod.CreditCard, CardNumber: card,
                SimulateNetworkFailure: networkFailure, SimulateTimeout: timeout);

        [Test]
        public void Charge_SuccessDemoCard_ReturnsSuccess()
        {
            var result = _sut.Charge(Request("4242424242424242"));

            result.Status.Should().Be(PaymentStatus.Success);
            result.FailureReason.Should().BeNull();
        }

        [Test]
        public void Charge_SuccessDemoCard_WithSpacesAndDashes_StillRecognized()
        {
            var result = _sut.Charge(Request("4242 4242-4242 4242"));

            result.Status.Should().Be(PaymentStatus.Success);
        }

        [Test]
        public void Charge_DeclinedDemoCard_ReturnsFailed()
        {
            var result = _sut.Charge(Request("4000000000000002"));

            result.Status.Should().Be(PaymentStatus.Failed);
            result.FailureReason.Should().Contain("declined");
        }

        [Test]
        public void Charge_InsufficientFundsDemoCard_ReturnsFailed()
        {
            var result = _sut.Charge(Request("4000000000009995"));

            result.Status.Should().Be(PaymentStatus.Failed);
            result.FailureReason.Should().Contain("Insufficient funds");
        }

        [Test]
        public void Charge_ExpiredDemoCard_ReturnsExpired()
        {
            var result = _sut.Charge(Request("4000000000000069"));

            result.Status.Should().Be(PaymentStatus.Expired);
        }

        [Test]
        public void Charge_ProcessingErrorDemoCard_ReturnsFailed()
        {
            var result = _sut.Charge(Request("4000000000000119"));

            result.Status.Should().Be(PaymentStatus.Failed);
            result.FailureReason.Should().Contain("Processing error");
        }

        [Test]
        public void Charge_CancelledDemoCard_ReturnsCancelled()
        {
            var result = _sut.Charge(Request("0000000000000000"));

            result.Status.Should().Be(PaymentStatus.Cancelled);
        }

        [Test]
        public void Charge_SimulateNetworkFailureFlag_WinsRegardlessOfCard()
        {
            var result = _sut.Charge(Request("4242424242424242", networkFailure: true));

            result.Status.Should().Be(PaymentStatus.NetworkFailure);
        }

        [Test]
        public void Charge_SimulateTimeoutFlag_WinsRegardlessOfCard()
        {
            var result = _sut.Charge(Request("4242424242424242", timeout: true));

            result.Status.Should().Be(PaymentStatus.Timeout);
        }

        [Test]
        public void Charge_TooShortCardNumber_ReturnsFailedInvalid()
        {
            var result = _sut.Charge(Request("4242"));

            result.Status.Should().Be(PaymentStatus.Failed);
            result.FailureReason.Should().Contain("Invalid card number");
        }

        // ─── Weighted random branch (unlisted, well-formed 16-digit card) ───
        // Buckets (cumulative, out of 100): Success 0-84, Failed 85-90,
        // Expired 91-93, NetworkFailure 94-96, Timeout 97-99.

        [Test]
        public void Charge_UnknownCard_RollInSuccessBucket_ReturnsSuccess()
        {
            var result = _sut.Charge(Request("1234567890123456"), new FixedRandom(0));

            result.Status.Should().Be(PaymentStatus.Success);
        }

        [Test]
        public void Charge_UnknownCard_RollInFailedBucket_ReturnsFailed()
        {
            var result = _sut.Charge(Request("1234567890123456"), new FixedRandom(85));

            result.Status.Should().Be(PaymentStatus.Failed);
        }

        [Test]
        public void Charge_UnknownCard_RollInExpiredBucket_ReturnsExpired()
        {
            var result = _sut.Charge(Request("1234567890123456"), new FixedRandom(91));

            result.Status.Should().Be(PaymentStatus.Expired);
        }

        [Test]
        public void Charge_UnknownCard_RollInNetworkFailureBucket_ReturnsNetworkFailure()
        {
            var result = _sut.Charge(Request("1234567890123456"), new FixedRandom(94));

            result.Status.Should().Be(PaymentStatus.NetworkFailure);
        }

        [Test]
        public void Charge_UnknownCard_RollInTimeoutBucket_ReturnsTimeout()
        {
            var result = _sut.Charge(Request("1234567890123456"), new FixedRandom(99));

            result.Status.Should().Be(PaymentStatus.Timeout);
        }
    }
}
