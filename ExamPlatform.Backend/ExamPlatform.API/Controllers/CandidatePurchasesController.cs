// CandidatePurchasesController — premium exam checkout + purchase history
// (Part 4/5/6 of the design). Candidate-only, except the simulated refund
// which is an Admin action.

#pragma warning disable CA1848
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Logging;
using ExamPlatform.API.DTOs.Payments;
using ExamPlatform.API.DTOs.Purchases;
using ExamPlatform.API.Helpers;
using ExamPlatform.BLL.Exceptions;
using ExamPlatform.BLL.Interfaces;
using ExamPlatform.Domain.Entities;
using ExamPlatform.Domain.Enums;

namespace ExamPlatform.API.Controllers
{
    [ApiController]
    [Route("api/purchases")]
    [Authorize]
    public class CandidatePurchasesController : ControllerBase
    {
        private readonly ICandidatePurchaseService _purchaseService;
        private readonly ILogger<CandidatePurchasesController> _logger;

        public CandidatePurchasesController(
            ICandidatePurchaseService purchaseService,
            ILogger<CandidatePurchasesController> logger)
        {
            _purchaseService = purchaseService;
            _logger = logger;
        }

        // ════════════════════════════════════════════════════════════════
        //   GET /api/purchases/mine  — candidate purchase/billing history
        // ════════════════════════════════════════════════════════════════
        [HttpGet("mine")]
        [Authorize(Roles = "Candidate")]
        public async Task<IActionResult> GetMine()
        {
            var userId = ExtractUserId();
            var purchases = await _purchaseService.GetMyPurchasesAsync(userId);
            return Ok(purchases.Select(ToDto));
        }

        // ════════════════════════════════════════════════════════════════
        //   GET /api/purchases/check/{examId}  — has-access check
        //   Used by ExamDetails/Attempt screens before showing the attempt
        //   button vs. the "Purchase to Unlock" CTA.
        // ════════════════════════════════════════════════════════════════
        [HttpGet("check/{examId:int}")]
        [Authorize(Roles = "Candidate")]
        public async Task<IActionResult> CheckAccess(int examId)
        {
            var userId = ExtractUserId();
            var hasAccess = await _purchaseService.HasActivePurchaseAsync(userId, examId);
            return Ok(new { examId, hasAccess });
        }

        // ════════════════════════════════════════════════════════════════
        //   POST /api/purchases  — candidate checkout
        // ════════════════════════════════════════════════════════════════
        [HttpPost]
        [Authorize(Roles = "Candidate")]
        [EnableRateLimiting(RateLimitPolicies.Payments)]
        public async Task<IActionResult> Purchase([FromBody] PurchaseRequestDto dto)
        {
            if (!Enum.TryParse<PaymentMethod>(dto.Method, ignoreCase: true, out var method))
                throw new ValidationException(
                    "Method must be one of: CreditCard, DebitCard, UPI, NetBanking, Wallet, DemoTestCard.");

            var userId = ExtractUserId();
            _logger.LogInformation(
                "POST /api/purchases: candidate {UserId}, exam {ExamId}.", userId, dto.ExamId);

            var payment = await _purchaseService.PurchaseAsync(
                userId, dto.ExamId, method, dto.CardNumber, dto.CouponCode, dto.IdempotencyKey,
                dto.SimulateNetworkFailure, dto.SimulateTimeout);

            return Ok(ToPaymentDto(payment));
        }

        // ════════════════════════════════════════════════════════════════
        //   POST /api/purchases/{id}/refund  — admin: simulated refund
        // ════════════════════════════════════════════════════════════════
        [HttpPost("{id:int}/refund")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Refund(int id)
        {
            _logger.LogInformation("POST /api/purchases/{Id}/refund triggered.", id);
            await _purchaseService.RefundAsync(id);
            return NoContent();
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

        private static CandidatePurchaseDto ToDto(CandidatePurchase p) => new()
        {
            PurchaseId = p.PurchaseId,
            ExamId = p.ExamId,
            ExamTitle = p.Exam?.Title ?? string.Empty,
            Status = p.Status.ToString(),
            PurchasedAt = p.PurchasedAt,
            Amount = p.Payment?.Amount ?? 0m,
            InvoiceNumber = p.Payment?.Invoice?.InvoiceNumber
        };

        private static PaymentDto ToPaymentDto(Payment p) => new()
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
