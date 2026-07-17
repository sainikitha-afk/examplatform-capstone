using ExamPlatform.BLL.Models;

namespace ExamPlatform.BLL.Interfaces
{
    // A fully simulated, offline payment gateway. No network call is ever
    // made — every outcome is decided locally from the demo card number (or
    // an explicit failure-simulation flag). See PaymentGatewayService for the
    // documented demo-card table.
    public interface IPaymentGatewayService
    {
        // 'random' is an optional testing seam (defaults to Random.Shared) —
        // pass a seeded Random to make the weighted-random branch
        // deterministic in unit tests.
        PaymentChargeOutcome Charge(PaymentChargeRequest request, Random? random = null);
    }
}
