// SubscriptionsController — examiner subscription dashboard + upgrade
// checkout (Part 2/6 of the Subscription & Payment System design).
//
// Security: GET /plans is anonymous (public pricing page). Everything else
// requires an authenticated Examiner — the JWT's user id is the only source
// of identity (ExtractUserId, same helper pattern as every other controller).
// The charge endpoint (/upgrade) carries the isolated "payments" rate-limit
// policy; no other endpoint in this controller or elsewhere is affected.

#pragma warning disable CA1848
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Logging;
using ExamPlatform.API.DTOs.Payments;
using ExamPlatform.API.DTOs.Subscriptions;
using ExamPlatform.API.Helpers;
using ExamPlatform.BLL.Exceptions;
using ExamPlatform.BLL.Interfaces;
using ExamPlatform.Domain.Entities;
using ExamPlatform.Domain.Enums;

namespace ExamPlatform.API.Controllers
{
    [ApiController]
    [Route("api/subscriptions")]
    [Authorize]
    public class SubscriptionsController : ControllerBase
    {
        private readonly IExaminerSubscriptionService _subscriptionService;
        private readonly ILogger<SubscriptionsController> _logger;

        public SubscriptionsController(
            IExaminerSubscriptionService subscriptionService,
            ILogger<SubscriptionsController> logger)
        {
            _subscriptionService = subscriptionService;
            _logger = logger;
        }

        // ════════════════════════════════════════════════════════════════
        //   GET /api/subscriptions/plans  — public pricing page
        // ════════════════════════════════════════════════════════════════
        [HttpGet("plans")]
        [AllowAnonymous]
        public async Task<IActionResult> GetPlans()
        {
            var plans = await _subscriptionService.GetAllPlansAsync();
            return Ok(plans.Select(ToDto));
        }

        // ════════════════════════════════════════════════════════════════
        //   GET /api/subscriptions/mine  — examiner dashboard
        // ════════════════════════════════════════════════════════════════
        [HttpGet("mine")]
        [Authorize(Roles = "Examiner")]
        public async Task<IActionResult> GetMine()
        {
            var userId = ExtractUserId();
            var subscription = await _subscriptionService.GetOrCreateAsync(userId);
            var eligibility = await _subscriptionService.CanCreateExamAsync(userId);

            return Ok(ToStatusDto(subscription, eligibility));
        }

        // ════════════════════════════════════════════════════════════════
        //   GET /api/subscriptions/billing-history  — examiner
        // ════════════════════════════════════════════════════════════════
        [HttpGet("billing-history")]
        [Authorize(Roles = "Examiner")]
        public async Task<IActionResult> GetBillingHistory()
        {
            var userId = ExtractUserId();
            var payments = await _subscriptionService.GetBillingHistoryAsync(userId);
            return Ok(payments.Select(ToDto));
        }

        // ════════════════════════════════════════════════════════════════
        //   POST /api/subscriptions/decline-upgrade  — examiner
        // ════════════════════════════════════════════════════════════════
        [HttpPost("decline-upgrade")]
        [Authorize(Roles = "Examiner")]
        public async Task<IActionResult> DeclineUpgrade()
        {
            var userId = ExtractUserId();
            _logger.LogInformation("POST /api/subscriptions/decline-upgrade: examiner {UserId}.", userId);

            var declineCount = await _subscriptionService.DeclineUpgradeAsync(userId);

            return Ok(new DeclineUpgradeResponseDto
            {
                UpgradeDeclineCount = declineCount,
                NextStep = declineCount >= 2 ? "free-forever" : "discount"
            });
        }

        // ════════════════════════════════════════════════════════════════
        //   POST /api/subscriptions/upgrade  — examiner charge + activate
        // ════════════════════════════════════════════════════════════════
        [HttpPost("upgrade")]
        [Authorize(Roles = "Examiner")]
        [EnableRateLimiting(RateLimitPolicies.Payments)]
        public async Task<IActionResult> Upgrade([FromBody] UpgradeSubscriptionDto dto)
        {
            if (!Enum.TryParse<PaymentMethod>(dto.Method, ignoreCase: true, out var method))
                throw new ValidationException(
                    "Method must be one of: CreditCard, DebitCard, UPI, NetBanking, Wallet, DemoTestCard.");

            var userId = ExtractUserId();
            _logger.LogInformation(
                "POST /api/subscriptions/upgrade: examiner {UserId} (annual={Annual}).", userId, dto.Annual);

            var payment = await _subscriptionService.UpgradeAsync(
                userId, method, dto.CardNumber, dto.Annual, dto.CouponCode, dto.IdempotencyKey,
                dto.SimulateNetworkFailure, dto.SimulateTimeout);

            return Ok(ToDto(payment));
        }

        // ───────────── PRIVATE HELPERS ─────────────

        private int ExtractUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                        ?? User.FindFirst("sub")?.Value
                        ?? User.FindFirst("nameid")?.Value;

            if (!int.TryParse(claim, out var id))
                throw new UnauthorizedAccessException("Invalid token: missing user id claim.");
            return id;
        }

        private static SubscriptionPlanDto ToDto(SubscriptionPlan p) => new()
        {
            PlanId = p.PlanId,
            Name = p.Name,
            Tier = p.Tier.ToString(),
            PriceMonthly = p.PriceMonthly,
            PriceYearly = p.PriceYearly,
            MaxExamsLifetime = p.MaxExamsLifetime,
            ExamCooldownDays = p.ExamCooldownDays
        };

        private static SubscriptionStatusDto ToStatusDto(
            ExaminerSubscription s, BLL.Models.ExamCreationEligibility eligibility)
        {
            var dialog = s.Status == ExaminerSubscriptionStatus.TrialExhausted
                ? (s.UpgradeDeclineCount == 0 ? "upgrade" : "discount")
                : "none";

            return new SubscriptionStatusDto
            {
                Status = s.Status.ToString(),
                PlanName = s.Plan?.Name ?? string.Empty,
                TrialExamsUsed = s.TrialExamsUsed,
                MaxExamsLifetime = eligibility.MaxExamsLifetime,
                UpgradeDeclineCount = s.UpgradeDeclineCount,
                NextEligibleAt = eligibility.NextEligibleAt,
                SubscriptionEndDate = s.SubscriptionEndDate,
                NextRenewalDate = s.NextRenewalDate,
                AutoRenew = s.AutoRenew,
                DiscountAppliedPercent = s.DiscountAppliedPercent,
                CanCreatePremiumExams = eligibility.CanCreatePremiumExams,
                UpgradeDialog = dialog
            };
        }

        private static PaymentDto ToDto(Payment p) => new()
        {
            PaymentId = p.PaymentId,
            Amount = p.Amount,
            Currency = p.Currency,
            Method = p.Method.ToString(),
            Status = p.Status.ToString(),
            FakeTransactionId = p.FakeTransactionId,
            FakeGatewayReference = p.FakeGatewayReference,
            FailureReason = p.FailureReason,
            InitiatedAt = p.InitiatedAt,
            CompletedAt = p.CompletedAt,
            Invoice = p.Invoice == null ? null : new InvoiceDto
            {
                InvoiceNumber = p.Invoice.InvoiceNumber,
                FakeGstNumber = p.Invoice.FakeGstNumber,
                BillingName = p.Invoice.BillingName,
                BillingEmail = p.Invoice.BillingEmail,
                Subtotal = p.Invoice.Subtotal,
                DiscountAmount = p.Invoice.DiscountAmount,
                TaxAmount = p.Invoice.TaxAmount,
                Total = p.Invoice.Total,
                IssuedAt = p.Invoice.IssuedAt
            }
        };
    }
}
