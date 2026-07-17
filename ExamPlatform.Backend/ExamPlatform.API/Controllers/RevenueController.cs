// RevenueController — Admin Revenue Dashboard (Part 7).

#pragma warning disable CA1848
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using ExamPlatform.API.DTOs.Revenue;
using ExamPlatform.BLL.Interfaces;
using ExamPlatform.BLL.Models;

namespace ExamPlatform.API.Controllers
{
    [ApiController]
    [Route("api/revenue")]
    [Authorize(Roles = "Admin")]
    public class RevenueController : ControllerBase
    {
        private readonly IRevenueAnalyticsService _revenueService;
        private readonly ILogger<RevenueController> _logger;

        public RevenueController(
            IRevenueAnalyticsService revenueService,
            ILogger<RevenueController> logger)
        {
            _revenueService = revenueService;
            _logger = logger;
        }

        // ════════════════════════════════════════════════════════════════
        //   GET /api/revenue/summary  — admin dashboard data
        // ════════════════════════════════════════════════════════════════
        [HttpGet("summary")]
        public async Task<IActionResult> GetSummary()
        {
            _logger.LogInformation("GET /api/revenue/summary requested.");
            var summary = await _revenueService.GetSummaryAsync();
            return Ok(ToDto(summary));
        }

        private static RevenueSummaryDto ToDto(RevenueSummary s) => new()
        {
            TotalRevenue = s.TotalRevenue,
            SubscriptionRevenue = s.SubscriptionRevenue,
            ExamCertificationRevenue = s.ExamCertificationRevenue,
            TotalSuccessfulPayments = s.TotalSuccessfulPayments,
            TotalFailedPayments = s.TotalFailedPayments,
            PaymentFailureRate = s.PaymentFailureRate,
            ActiveTrialCount = s.ActiveTrialCount,
            ActiveProfessionalCount = s.ActiveProfessionalCount,
            ActiveFreeForeverCount = s.ActiveFreeForeverCount,
            TrialToPaidConversionRate = s.TrialToPaidConversionRate,
            CouponRedemptions = s.CouponRedemptions,
            MonthlyRevenue = s.MonthlyRevenue.Select(m => new MonthlyRevenuePointDto
            { Month = m.Month, Revenue = m.Revenue }).ToList(),
            PlanDistribution = s.PlanDistribution.Select(p => new PlanDistributionSliceDto
            { Status = p.Status, Count = p.Count }).ToList(),
            TopPurchasedExams = s.TopPurchasedExams.Select(t => new TopPurchasedExamDto
            { ExamTitle = t.ExamTitle, PurchaseCount = t.PurchaseCount, Revenue = t.Revenue }).ToList(),
            RecentTransactions = s.RecentTransactions.Select(t => new RecentTransactionDto
            { Date = t.Date, PayerName = t.PayerName, Amount = t.Amount, Status = t.Status, Purpose = t.Purpose }).ToList()
        };
    }
}
