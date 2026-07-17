namespace ExamPlatform.API.DTOs.Revenue
{
    public class RevenueSummaryDto
    {
        public decimal TotalRevenue { get; set; }
        public decimal SubscriptionRevenue { get; set; }
        public decimal ExamCertificationRevenue { get; set; }
        public int TotalSuccessfulPayments { get; set; }
        public int TotalFailedPayments { get; set; }
        public double PaymentFailureRate { get; set; }
        public int ActiveTrialCount { get; set; }
        public int ActiveProfessionalCount { get; set; }
        public int ActiveFreeForeverCount { get; set; }
        public double TrialToPaidConversionRate { get; set; }
        public int CouponRedemptions { get; set; }
        public List<MonthlyRevenuePointDto> MonthlyRevenue { get; set; } = new();
        public List<PlanDistributionSliceDto> PlanDistribution { get; set; } = new();
        public List<TopPurchasedExamDto> TopPurchasedExams { get; set; } = new();
        public List<RecentTransactionDto> RecentTransactions { get; set; } = new();
    }

    public class MonthlyRevenuePointDto
    {
        public string Month { get; set; } = string.Empty;
        public decimal Revenue { get; set; }
    }

    public class PlanDistributionSliceDto
    {
        public string Status { get; set; } = string.Empty;
        public int Count { get; set; }
    }

    public class TopPurchasedExamDto
    {
        public string ExamTitle { get; set; } = string.Empty;
        public int PurchaseCount { get; set; }
        public decimal Revenue { get; set; }
    }

    public class RecentTransactionDto
    {
        public DateTime Date { get; set; }
        public string PayerName { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Status { get; set; } = string.Empty;
        public string Purpose { get; set; } = string.Empty;
    }
}
