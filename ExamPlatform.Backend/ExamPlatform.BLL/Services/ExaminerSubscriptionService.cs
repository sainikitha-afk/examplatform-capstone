// ExaminerSubscriptionService — the examiner-side monetization state machine
// (Part 2 of the design). Every timing decision accepts an optional `now`
// parameter (defaulting to DateTime.UtcNow) purely as a testing seam — the
// codebase has no IClock abstraction, and one isn't needed beyond this.

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
    public class ExaminerSubscriptionService : IExaminerSubscriptionService
    {
        public const int TrialExamLimit = 8;
        public const int FreeForeverCooldownDays = 3;
        public const int GracePeriodDays = 3;
        public const int DiscountOnDeclinePercent = 25;

        private readonly IExaminerSubscriptionRepository _subscriptionRepository;
        private readonly ISubscriptionPlanRepository _planRepository;
        private readonly IExamRepository _examRepository;
        private readonly IUserRepository _userRepository;
        private readonly IPaymentRepository _paymentRepository;
        private readonly IInvoiceService _invoiceService;
        private readonly ICouponService _couponService;
        private readonly IPaymentGatewayService _gateway;
        private readonly ILogger<ExaminerSubscriptionService> _logger;

        public ExaminerSubscriptionService(
            IExaminerSubscriptionRepository subscriptionRepository,
            ISubscriptionPlanRepository planRepository,
            IExamRepository examRepository,
            IUserRepository userRepository,
            IPaymentRepository paymentRepository,
            IInvoiceService invoiceService,
            ICouponService couponService,
            IPaymentGatewayService gateway,
            ILogger<ExaminerSubscriptionService> logger)
        {
            _subscriptionRepository = subscriptionRepository;
            _planRepository = planRepository;
            _examRepository = examRepository;
            _userRepository = userRepository;
            _paymentRepository = paymentRepository;
            _invoiceService = invoiceService;
            _couponService = couponService;
            _gateway = gateway;
            _logger = logger;
        }

        // ════════════════════════════════════════════════════════════════
        //   GET OR CREATE
        // ════════════════════════════════════════════════════════════════
        public async Task<ExaminerSubscription> GetOrCreateAsync(int userId, DateTime? now = null)
        {
            var effectiveNow = now ?? DateTime.UtcNow;
            await EnsurePlansSeededAsync(effectiveNow);

            var existing = await _subscriptionRepository.GetByUserIdAsync(userId);
            if (existing != null)
                return existing;

            // Grandfathering rule: an examiner who predates this feature and
            // already has prior exams is never retroactively squeezed into
            // the 8-exam trial cap — they're grandfathered straight onto
            // Professional (unlimited, indefinite). A brand-new examiner with
            // zero prior exams gets a fresh trial, same as anyone approved
            // from this point on.
            var hasPriorExams = (await _examRepository.GetByExaminerAsync(userId)).Any();

            var plan = hasPriorExams
                ? await _planRepository.GetByTierAsync(SubscriptionTier.Professional)
                : await _planRepository.GetByTierAsync(SubscriptionTier.Trial);

            if (plan == null)
                throw new InvalidOperationException("Subscription plans are not seeded.");

            var subscription = new ExaminerSubscription
            {
                UserId = userId,
                PlanId = plan.PlanId,
                Status = hasPriorExams
                    ? ExaminerSubscriptionStatus.ProfessionalActive
                    : ExaminerSubscriptionStatus.TrialActive,
                TrialExamsUsed = 0,
                UpgradeDeclineCount = 0,
                SubscriptionStartDate = effectiveNow,
                SubscriptionEndDate = null,
                AutoRenew = hasPriorExams,
                CreatedAt = effectiveNow,
                UpdatedAt = effectiveNow
            };

            await _subscriptionRepository.AddAsync(subscription);
            await _subscriptionRepository.SaveChangesAsync();

            if (hasPriorExams)
                _logger.LogInformation(
                    "Examiner {UserId} grandfathered onto Professional (pre-existing exams found).", userId);
            else
                _logger.LogInformation("Free trial subscription created for examiner {UserId}.", userId);

            subscription.Plan = plan;
            return subscription;
        }

        // ════════════════════════════════════════════════════════════════
        //   CAN CREATE EXAM
        // ════════════════════════════════════════════════════════════════
        public async Task<ExamCreationEligibility> CanCreateExamAsync(int userId, DateTime? now = null)
        {
            var effectiveNow = now ?? DateTime.UtcNow;
            var subscription = await GetOrCreateAsync(userId, effectiveNow);
            var maxLifetime = subscription.Plan?.MaxExamsLifetime;

            bool canCreatePremium = subscription.Status is ExaminerSubscriptionStatus.ProfessionalActive
                or ExaminerSubscriptionStatus.GracePeriod;

            switch (subscription.Status)
            {
                case ExaminerSubscriptionStatus.TrialActive:
                    return new ExamCreationEligibility(true, subscription.Status,
                        subscription.TrialExamsUsed, maxLifetime, subscription.UpgradeDeclineCount,
                        null, canCreatePremium);

                case ExaminerSubscriptionStatus.TrialExhausted:
                    return new ExamCreationEligibility(false, subscription.Status,
                        subscription.TrialExamsUsed, maxLifetime, subscription.UpgradeDeclineCount,
                        null, canCreatePremium);

                case ExaminerSubscriptionStatus.ProfessionalActive:
                case ExaminerSubscriptionStatus.GracePeriod:
                    return new ExamCreationEligibility(true, subscription.Status,
                        subscription.TrialExamsUsed, maxLifetime, subscription.UpgradeDeclineCount,
                        null, canCreatePremium);

                case ExaminerSubscriptionStatus.FreeForever:
                case ExaminerSubscriptionStatus.Cancelled:
                default:
                    var cooldownDays = subscription.Plan?.ExamCooldownDays ?? FreeForeverCooldownDays;
                    var nextEligibleAt = subscription.LastFreeExamCreatedAt?.AddDays(cooldownDays);
                    var allowed = nextEligibleAt == null || effectiveNow >= nextEligibleAt;
                    return new ExamCreationEligibility(allowed, subscription.Status,
                        subscription.TrialExamsUsed, maxLifetime, subscription.UpgradeDeclineCount,
                        allowed ? null : nextEligibleAt, canCreatePremium);
            }
        }

        // ════════════════════════════════════════════════════════════════
        //   RECORD EXAM CREATED
        // ════════════════════════════════════════════════════════════════
        public async Task RecordExamCreatedAsync(int userId, DateTime? now = null)
        {
            var effectiveNow = now ?? DateTime.UtcNow;
            var subscription = await GetOrCreateAsync(userId, effectiveNow);

            if (subscription.Status == ExaminerSubscriptionStatus.TrialActive)
            {
                subscription.TrialExamsUsed++;
                if (subscription.TrialExamsUsed >= TrialExamLimit)
                {
                    subscription.Status = ExaminerSubscriptionStatus.TrialExhausted;
                    _logger.LogInformation(
                        "Examiner {UserId} exhausted their free trial after {Count} exams.",
                        userId, subscription.TrialExamsUsed);
                }
            }
            else if (subscription.Status == ExaminerSubscriptionStatus.FreeForever)
            {
                subscription.LastFreeExamCreatedAt = effectiveNow;
            }

            subscription.UpdatedAt = effectiveNow;
            _subscriptionRepository.Update(subscription);
            await _subscriptionRepository.SaveChangesAsync();
        }

        // ════════════════════════════════════════════════════════════════
        //   DECLINE UPGRADE
        // ════════════════════════════════════════════════════════════════
        public async Task<int> DeclineUpgradeAsync(int userId, DateTime? now = null)
        {
            var effectiveNow = now ?? DateTime.UtcNow;
            var subscription = await GetOrCreateAsync(userId, effectiveNow);

            if (subscription.Status != ExaminerSubscriptionStatus.TrialExhausted)
                throw new ValidationException(
                    "There is no active upgrade offer to decline for this account.");

            subscription.UpgradeDeclineCount++;

            if (subscription.UpgradeDeclineCount >= 2)
            {
                var freeForeverPlan = await _planRepository.GetByTierAsync(SubscriptionTier.FreeForever)
                    ?? throw new InvalidOperationException("Free Forever plan is not seeded.");

                subscription.PlanId = freeForeverPlan.PlanId;
                subscription.Status = ExaminerSubscriptionStatus.FreeForever;
                // No cooldown timer yet — the examiner gets one immediate exam
                // once they actually use the plan; the 3-day cadence starts
                // from that first Free Forever exam, not from the downgrade moment.
                subscription.LastFreeExamCreatedAt = null;

                _logger.LogInformation(
                    "Examiner {UserId} declined the discount offer; moved to Free Forever.", userId);
            }
            else
            {
                _logger.LogInformation(
                    "Examiner {UserId} declined the full-price upgrade; 25%-off offer will be shown next.", userId);
            }

            subscription.UpdatedAt = effectiveNow;
            _subscriptionRepository.Update(subscription);
            await _subscriptionRepository.SaveChangesAsync();

            return subscription.UpgradeDeclineCount;
        }

        // ════════════════════════════════════════════════════════════════
        //   UPGRADE (charge + activate Professional)
        // ════════════════════════════════════════════════════════════════
        public async Task<Payment> UpgradeAsync(
            int userId, PaymentMethod method, string cardNumber, bool annual,
            string? couponCode, string idempotencyKey,
            bool simulateNetworkFailure, bool simulateTimeout, DateTime? now = null)
        {
            var effectiveNow = now ?? DateTime.UtcNow;

            // Idempotency: a repeated submission with the same key returns the
            // original outcome instead of charging again.
            var existingPayment = await _paymentRepository.GetByIdempotencyKeyAsync(idempotencyKey);
            if (existingPayment != null)
                return existingPayment;

            var subscription = await GetOrCreateAsync(userId, effectiveNow);
            var professionalPlan = await _planRepository.GetByTierAsync(SubscriptionTier.Professional)
                ?? throw new InvalidOperationException("Professional plan is not seeded.");

            var baseAmount = annual ? professionalPlan.PriceYearly : professionalPlan.PriceMonthly;

            int discountPercent = 0;
            int? redeemedCouponId = null;
            var isAutomaticRetentionOffer = subscription.UpgradeDeclineCount == 1;

            if (isAutomaticRetentionOffer)
            {
                discountPercent = DiscountOnDeclinePercent;
            }
            else if (!string.IsNullOrWhiteSpace(couponCode))
            {
                var validation = await _couponService.ValidateAsync(
                    couponCode, userId, CouponScope.SubscriptionOnly, effectiveNow);
                if (!validation.IsValid)
                    throw new InvalidCouponException(validation.RejectionReason!);

                discountPercent = validation.DiscountPercent;
                redeemedCouponId = validation.CouponId;
            }

            var discountAmount = Math.Round(baseAmount * discountPercent / 100m, 2);
            var netAmount = baseAmount - discountAmount;

            var payer = await _userRepository.GetByIdAsync(userId)
                ?? throw new UserNotFoundException(userId);

            var payment = new Payment
            {
                UserId = userId,
                SubscriptionId = subscription.SubscriptionId,
                Purpose = PaymentPurpose.SubscriptionPurchase,
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
            await _paymentRepository.SaveChangesAsync(); // need PaymentId before Invoice FK

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
                    "Subscription upgrade payment {PaymentId} for examiner {UserId} ended in {Status}.",
                    payment.PaymentId, userId, outcome.Status);
                return payment;
            }

            if (redeemedCouponId.HasValue)
                await _couponService.RedeemAsync(redeemedCouponId.Value, userId, payment.PaymentId, effectiveNow);

            subscription.PlanId = professionalPlan.PlanId;
            subscription.Status = ExaminerSubscriptionStatus.ProfessionalActive;
            subscription.AutoRenew = true;
            subscription.SubscriptionStartDate = effectiveNow;
            subscription.SubscriptionEndDate = annual ? effectiveNow.AddYears(1) : effectiveNow.AddMonths(1);
            subscription.NextRenewalDate = subscription.SubscriptionEndDate;
            if (isAutomaticRetentionOffer)
                subscription.DiscountAppliedPercent = discountPercent;
            subscription.UpdatedAt = effectiveNow;
            _subscriptionRepository.Update(subscription);
            await _subscriptionRepository.SaveChangesAsync();

            // Assigned explicitly (not relying on EF Core's automatic relationship
            // fixup) so the caller can always read payment.Invoice immediately.
            payment.Invoice = await _invoiceService.GenerateAsync(new InvoiceRequest(
                payment.PaymentId, baseAmount, discountAmount, payer.FullName, payer.Email, effectiveNow));

            _logger.LogInformation(
                "Examiner {UserId} upgraded to Professional via payment {PaymentId} (annual={Annual}, discount={Discount}%).",
                userId, payment.PaymentId, annual, discountPercent);

            return payment;
        }

        // ════════════════════════════════════════════════════════════════
        //   SWEEP EXPIRED SUBSCRIPTIONS
        // ════════════════════════════════════════════════════════════════
        public async Task<int> SweepExpiredSubscriptionsAsync(DateTime? now = null)
        {
            var effectiveNow = now ?? DateTime.UtcNow;
            var all = await _subscriptionRepository.GetAllAsync();
            var affected = 0;

            SubscriptionPlan? freeForeverPlan = null;

            foreach (var subscription in all)
            {
                if (subscription.Status == ExaminerSubscriptionStatus.ProfessionalActive
                    && !subscription.AutoRenew
                    && subscription.SubscriptionEndDate.HasValue
                    && effectiveNow > subscription.SubscriptionEndDate.Value)
                {
                    subscription.Status = ExaminerSubscriptionStatus.GracePeriod;
                    subscription.UpdatedAt = effectiveNow;
                    _subscriptionRepository.Update(subscription);
                    affected++;
                }
                else if (subscription.Status == ExaminerSubscriptionStatus.GracePeriod
                    && subscription.SubscriptionEndDate.HasValue
                    && effectiveNow > subscription.SubscriptionEndDate.Value.AddDays(GracePeriodDays))
                {
                    freeForeverPlan ??= await _planRepository.GetByTierAsync(SubscriptionTier.FreeForever);
                    if (freeForeverPlan != null)
                        subscription.PlanId = freeForeverPlan.PlanId;

                    subscription.Status = ExaminerSubscriptionStatus.FreeForever;
                    subscription.AutoRenew = false;
                    subscription.LastFreeExamCreatedAt = null;
                    subscription.UpdatedAt = effectiveNow;
                    _subscriptionRepository.Update(subscription);
                    affected++;
                }
            }

            if (affected > 0)
            {
                await _subscriptionRepository.SaveChangesAsync();
                _logger.LogInformation("Subscription expiry sweep transitioned {Count} subscription(s).", affected);
            }

            return affected;
        }

        // ════════════════════════════════════════════════════════════════
        //   GET ALL PLANS (public pricing page)
        // ════════════════════════════════════════════════════════════════
        public async Task<IEnumerable<SubscriptionPlan>> GetAllPlansAsync()
        {
            await EnsurePlansSeededAsync(DateTime.UtcNow);
            return await _planRepository.GetAllAsync();
        }

        public async Task<IEnumerable<Payment>> GetBillingHistoryAsync(int userId)
        {
            return await _paymentRepository.GetByUserIdAsync(userId);
        }

        // ───────────── PRIVATE HELPERS ─────────────

        private async Task EnsurePlansSeededAsync(DateTime now)
        {
            if (await _planRepository.GetByTierAsync(SubscriptionTier.Trial) == null)
            {
                await _planRepository.AddAsync(new SubscriptionPlan
                {
                    Name = "Free Trial",
                    Tier = SubscriptionTier.Trial,
                    PriceMonthly = 0m,
                    PriceYearly = 0m,
                    MaxExamsLifetime = TrialExamLimit,
                    ExamCooldownDays = null,
                    IsActive = true
                });
            }

            if (await _planRepository.GetByTierAsync(SubscriptionTier.Professional) == null)
            {
                await _planRepository.AddAsync(new SubscriptionPlan
                {
                    Name = "Professional",
                    Tier = SubscriptionTier.Professional,
                    PriceMonthly = 1499m,
                    PriceYearly = 14990m,
                    MaxExamsLifetime = null,
                    ExamCooldownDays = null,
                    IsActive = true
                });
            }

            if (await _planRepository.GetByTierAsync(SubscriptionTier.FreeForever) == null)
            {
                await _planRepository.AddAsync(new SubscriptionPlan
                {
                    Name = "Free Forever",
                    Tier = SubscriptionTier.FreeForever,
                    PriceMonthly = 0m,
                    PriceYearly = 0m,
                    MaxExamsLifetime = null,
                    ExamCooldownDays = FreeForeverCooldownDays,
                    IsActive = true
                });
            }

            await _planRepository.SaveChangesAsync();
        }
    }
}
