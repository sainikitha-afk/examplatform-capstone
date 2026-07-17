// CouponsController — admin-managed discount codes (Part 8/9/10), plus a
// validate endpoint any authenticated user can call at checkout to preview
// a discount before submitting payment.

#pragma warning disable CA1848
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using ExamPlatform.API.DTOs.Coupons;
using ExamPlatform.BLL.Exceptions;
using ExamPlatform.BLL.Interfaces;
using ExamPlatform.DAL.Interfaces;
using ExamPlatform.Domain.Entities;
using ExamPlatform.Domain.Enums;

namespace ExamPlatform.API.Controllers
{
    [ApiController]
    [Route("api/coupons")]
    [Authorize]
    public class CouponsController : ControllerBase
    {
        private readonly ICouponService _couponService;
        private readonly ICouponRepository _couponRepository;
        private readonly ILogger<CouponsController> _logger;

        public CouponsController(
            ICouponService couponService,
            ICouponRepository couponRepository,
            ILogger<CouponsController> logger)
        {
            _couponService = couponService;
            _couponRepository = couponRepository;
            _logger = logger;
        }

        // ════════════════════════════════════════════════════════════════
        //   POST /api/coupons/validate  — checkout preview (any authenticated role)
        // ════════════════════════════════════════════════════════════════
        [HttpPost("validate")]
        public async Task<IActionResult> Validate([FromBody] ValidateCouponRequestDto dto)
        {
            if (!Enum.TryParse<CouponScope>(dto.Scope, ignoreCase: true, out var scope))
                throw new ValidationException(
                    "Scope must be one of: SubscriptionOnly, ExamPurchaseOnly, Both.");

            var userId = ExtractUserId();
            var result = await _couponService.ValidateAsync(dto.Code, userId, scope, DateTime.UtcNow);

            return Ok(new ValidateCouponResponseDto
            {
                IsValid = result.IsValid,
                DiscountPercent = result.DiscountPercent,
                RejectionReason = result.RejectionReason
            });
        }

        // ════════════════════════════════════════════════════════════════
        //   GET /api/coupons  — admin: all coupons
        // ════════════════════════════════════════════════════════════════
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAll()
        {
            var coupons = await _couponRepository.GetAllAsync();
            return Ok(coupons.Select(ToDto));
        }

        // ════════════════════════════════════════════════════════════════
        //   POST /api/coupons  — admin: create a coupon
        // ════════════════════════════════════════════════════════════════
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create([FromBody] CreateCouponDto dto)
        {
            if (!Enum.TryParse<CouponScope>(dto.AppliesTo, ignoreCase: true, out var appliesTo))
                throw new ValidationException(
                    "AppliesTo must be one of: SubscriptionOnly, ExamPurchaseOnly, Both.");

            _logger.LogInformation("POST /api/coupons: creating coupon '{Code}'.", dto.Code);

            var coupon = new Coupon
            {
                Code = dto.Code.Trim().ToUpperInvariant(),
                DiscountPercent = dto.DiscountPercent,
                MaxRedemptions = dto.MaxRedemptions,
                TimesRedeemed = 0,
                ValidFrom = dto.ValidFrom,
                ValidTo = dto.ValidTo,
                IsActive = true,
                AppliesTo = appliesTo
            };
            await _couponRepository.AddAsync(coupon);
            await _couponRepository.SaveChangesAsync();

            return CreatedAtAction(nameof(GetAll), new { }, ToDto(coupon));
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

        private static CouponDto ToDto(Coupon c) => new()
        {
            CouponId = c.CouponId,
            Code = c.Code,
            DiscountPercent = c.DiscountPercent,
            MaxRedemptions = c.MaxRedemptions,
            TimesRedeemed = c.TimesRedeemed,
            ValidFrom = c.ValidFrom,
            ValidTo = c.ValidTo,
            IsActive = c.IsActive,
            AppliesTo = c.AppliesTo.ToString()
        };
    }
}
