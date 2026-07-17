// CandidatePurchaseService — the candidate-side purchase flow for premium
// exams (Part 4/5 of the design). Mirrors ExaminerSubscriptionService's
// UpgradeAsync shape (idempotency check -> Pending Payment -> simulated
// charge -> grant access -> invoice) for the candidate/exam domain.

#pragma warning disable CA1848
using Microsoft.Extensions.Logging;
using ExamPlatform.BLL.Exceptions;
using ExamPlatform.BLL.Interfaces;
using ExamPlatform.BLL.Models;
using ExamPlatform.DAL.Interfaces;
using ExamPlatform.Domain.Entities;
using ExamPlatform.Domain.Enums;

namespace ExamPlatform.BLL.Services
{
    public class CandidatePurchaseService : ICandidatePurchaseService
    {
        private readonly ICandidatePurchaseRepository _purchaseRepository;
        private readonly IExamRepository _examRepository;
        private readonly IUserRepository _userRepository;
        private readonly IPaymentRepository _paymentRepository;
        private readonly IInvoiceService _invoiceService;
        private readonly ICouponService _couponService;
        private readonly IPaymentGatewayService _gateway;
        private readonly ILogger<CandidatePurchaseService> _logger;

        public CandidatePurchaseService(
            ICandidatePurchaseRepository purchaseRepository,
            IExamRepository examRepository,
            IUserRepository userRepository,
            IPaymentRepository paymentRepository,
            IInvoiceService invoiceService,
            ICouponService couponService,
            IPaymentGatewayService gateway,
            ILogger<CandidatePurchaseService> logger)
        {
            _purchaseRepository = purchaseRepository;
            _examRepository = examRepository;
            _userRepository = userRepository;
            _paymentRepository = paymentRepository;
            _invoiceService = invoiceService;
            _couponService = couponService;
            _gateway = gateway;
            _logger = logger;
        }

        public async Task<bool> HasActivePurchaseAsync(int userId, int examId)
        {
            var purchase = await _purchaseRepository.GetByUserAndExamAsync(userId, examId);
            return purchase != null && purchase.Status == CandidatePurchaseStatus.Active;
        }

        public async Task<Payment> PurchaseAsync(
            int userId, int examId, PaymentMethod method, string cardNumber,
            string? couponCode, string idempotencyKey,
            bool simulateNetworkFailure, bool simulateTimeout, DateTime? now = null)
        {
            var effectiveNow = now ?? DateTime.UtcNow;

            // Idempotency: a repeated submission with the same key returns the
            // original outcome instead of charging again.
            var existingPayment = await _paymentRepository.GetByIdempotencyKeyAsync(idempotencyKey);
            if (existingPayment != null)
                return existingPayment;

            var exam = await _examRepository.GetByIdAsync(examId)
                ?? throw new ExamNotFoundException(examId);

            if (!exam.IsPremium)
                throw new ValidationException("This exam is not a premium certification exam — no purchase is required.");

            // Friendly pre-check; the DB unique index on (UserId, ExamId) is
            // the actual race-safe guard against a concurrent double-purchase.
            var existingPurchase = await _purchaseRepository.GetByUserAndExamAsync(userId, examId);
            if (existingPurchase != null && existingPurchase.Status == CandidatePurchaseStatus.Active)
                throw new DuplicatePurchaseException();

            var baseAmount = exam.Price
                ?? throw new InvalidOperationException($"Premium exam {examId} has no price set.");

            int discountPercent = 0;
            int? redeemedCouponId = null;
            if (!string.IsNullOrWhiteSpace(couponCode))
            {
                var validation = await _couponService.ValidateAsync(
                    couponCode, userId, CouponScope.ExamPurchaseOnly, effectiveNow);
                if (!validation.IsValid)
                    throw new InvalidCouponException(validation.RejectionReason!);

                discountPercent = validation.DiscountPercent;
                redeemedCouponId = validation.CouponId;
            }

            var discountAmount = Math.Round(baseAmount * discountPercent / 100m, 2);
            var netAmount = baseAmount - discountAmount;

            var candidate = await _userRepository.GetByIdAsync(userId)
                ?? throw new UserNotFoundException(userId);

            var payment = new Payment
            {
                UserId = userId,
                Purpose = PaymentPurpose.ExamCertificationPurchase,
                Amount = netAmount,
                Currency = "INR",
                Method = method,
                Status = PaymentStatus.Pending,
                FakeTransactionId = PaymentReferenceGenerator.TransactionId(effectiveNow),
                FakeGatewayReference = PaymentReferenceGenerator.GatewayReference(),
                IdempotencyKey = idempotencyKey,
                CouponId = redeemedCouponId,
                InitiatedAt = effectiveNow
            };
            await _paymentRepository.AddAsync(payment);
            await _paymentRepository.SaveChangesAsync(); // need PaymentId before CandidatePurchase/Invoice FK

            var outcome = _gateway.Charge(new PaymentChargeRequest(
                netAmount, method, cardNumber, simulateNetworkFailure, simulateTimeout));

            payment.Status = outcome.Status;
            payment.FailureReason = outcome.FailureReason;
            payment.CompletedAt = effectiveNow;
            _paymentRepository.Update(payment);
            await _paymentRepository.SaveChangesAsync();

            if (outcome.Status != PaymentStatus.Success)
            {
                _logger.LogInformation(
                    "Premium exam purchase payment {PaymentId} for candidate {UserId}/exam {ExamId} ended in {Status}.",
                    payment.PaymentId, userId, examId, outcome.Status);
                return payment;
            }

            if (redeemedCouponId.HasValue)
                await _couponService.RedeemAsync(redeemedCouponId.Value, userId, payment.PaymentId, effectiveNow);

            // The DB unique index is on (UserId, ExamId) with no status
            // qualifier — it applies whether the existing row is Active or
            // Refunded. A candidate buying again after a refund must REUSE
            // that row (flip it back to Active against the new payment)
            // rather than INSERT a second one, or SaveChangesAsync throws an
            // unhandled constraint-violation 500 on a perfectly legitimate
            // re-purchase.
            if (existingPurchase != null)
            {
                existingPurchase.PaymentId = payment.PaymentId;
                existingPurchase.Status = CandidatePurchaseStatus.Active;
                existingPurchase.PurchasedAt = effectiveNow;
                _purchaseRepository.Update(existingPurchase);
            }
            else
            {
                await _purchaseRepository.AddAsync(new CandidatePurchase
                {
                    UserId = userId,
                    ExamId = examId,
                    PaymentId = payment.PaymentId,
                    Status = CandidatePurchaseStatus.Active,
                    PurchasedAt = effectiveNow
                });
            }
            await _purchaseRepository.SaveChangesAsync();

            // Assigned explicitly (not relying on EF Core's automatic relationship
            // fixup) so the caller can always read payment.Invoice immediately.
            payment.Invoice = await _invoiceService.GenerateAsync(new InvoiceRequest(
                payment.PaymentId, baseAmount, discountAmount, candidate.FullName, candidate.Email, effectiveNow));

            _logger.LogInformation(
                "Candidate {UserId} purchased premium exam {ExamId} via payment {PaymentId}.",
                userId, examId, payment.PaymentId);

            return payment;
        }

        public async Task<IEnumerable<CandidatePurchase>> GetMyPurchasesAsync(int userId)
        {
            return await _purchaseRepository.GetByUserIdAsync(userId);
        }

        public async Task RefundAsync(int purchaseId, DateTime? now = null)
        {
            var effectiveNow = now ?? DateTime.UtcNow;
            var purchase = await _purchaseRepository.GetByIdAsync(purchaseId);
            if (purchase == null)
                throw new ValidationException("Purchase not found.");

            if (purchase.Status == CandidatePurchaseStatus.Refunded)
                return; // idempotent — already refunded

            purchase.Status = CandidatePurchaseStatus.Refunded;
            _purchaseRepository.Update(purchase);
            await _purchaseRepository.SaveChangesAsync();

            var payment = await _paymentRepository.GetByIdAsync(purchase.PaymentId);
            if (payment != null)
            {
                payment.Status = PaymentStatus.Refunded;
                _paymentRepository.Update(payment);
                await _paymentRepository.SaveChangesAsync();
            }

            _logger.LogInformation(
                "Purchase {PurchaseId} refunded at {Now:u}; attempt access revoked.", purchaseId, effectiveNow);
        }
    }
}
