using ExamPlatform.BLL.Interfaces;
using ExamPlatform.BLL.Models;
using ExamPlatform.Domain.Enums;

namespace ExamPlatform.BLL.Services
{
    // Fully simulated, offline "payment gateway" — Part 5 of the design.
    // No real gateway integration exists anywhere in ExamHub; this class is
    // the entire "network call" for every charge. Deterministic demo card
    // numbers mirror Stripe's own published test-card convention so the
    // behaviour is instantly recognizable, while an unlisted well-formed
    // number still exercises every outcome via weighted randomness so the
    // demo isn't purely deterministic.
    public class PaymentGatewayService : IPaymentGatewayService
    {
        // Card number (spaces/dashes stripped) -> fixed outcome. Keeping this
        // as a lookup table (not a chain of ifs) makes every case trivially
        // unit-testable and easy to extend.
        private static readonly IReadOnlyDictionary<string, (PaymentStatus Status, string? Reason)> DemoCards =
            new Dictionary<string, (PaymentStatus, string?)>
            {
                ["4242424242424242"] = (PaymentStatus.Success, null),
                ["4000000000000002"] = (PaymentStatus.Failed, "Card declined by the issuing bank."),
                ["4000000000009995"] = (PaymentStatus.Failed, "Insufficient funds."),
                ["4000000000000069"] = (PaymentStatus.Expired, "Card has expired."),
                ["4000000000000119"] = (PaymentStatus.Failed, "Processing error at the issuing bank."),
                ["0000000000000000"] = (PaymentStatus.Cancelled, "Payment cancelled."),
            };

        // Weighted outcomes for any other well-formed 16-digit number, so the
        // demo isn't only reachable through the fixed table. Order matters —
        // first bucket whose cumulative weight the roll falls under wins.
        private static readonly (int Weight, PaymentStatus Status, string? Reason)[] RandomOutcomes =
        {
            (85, PaymentStatus.Success, null),
            (6,  PaymentStatus.Failed, "Card declined by the issuing bank."),
            (3,  PaymentStatus.Expired, "Card has expired."),
            (3,  PaymentStatus.NetworkFailure, "Network error while contacting the payment gateway."),
            (3,  PaymentStatus.Timeout, "The payment gateway did not respond in time."),
        };

        public PaymentChargeOutcome Charge(PaymentChargeRequest request, Random? random = null)
        {
            // The dedicated "Simulate network failure" control always wins,
            // regardless of card number — it exists specifically to demo the
            // retry UI on demand rather than waiting for a random roll.
            if (request.SimulateNetworkFailure)
                return new PaymentChargeOutcome(PaymentStatus.NetworkFailure,
                    "Network error while contacting the payment gateway.");

            if (request.SimulateTimeout)
                return new PaymentChargeOutcome(PaymentStatus.Timeout,
                    "The payment gateway did not respond in time.");

            var cleaned = new string((request.CardNumber ?? string.Empty)
                .Where(char.IsDigit).ToArray());

            if (DemoCards.TryGetValue(cleaned, out var outcome))
                return new PaymentChargeOutcome(outcome.Status, outcome.Reason);

            if (cleaned.Length != 16)
                return new PaymentChargeOutcome(PaymentStatus.Failed, "Invalid card number.");

            return RollWeightedOutcome(random ?? Random.Shared);
        }

        private static PaymentChargeOutcome RollWeightedOutcome(Random random)
        {
            var totalWeight = RandomOutcomes.Sum(o => o.Weight);
            var roll = random.Next(totalWeight);
            var cumulative = 0;

            foreach (var (weight, status, reason) in RandomOutcomes)
            {
                cumulative += weight;
                if (roll < cumulative)
                    return new PaymentChargeOutcome(status, reason);
            }

            // Unreachable given weights sum to totalWeight, but keeps the
            // method total.
            return new PaymentChargeOutcome(PaymentStatus.Success, null);
        }
    }
}
