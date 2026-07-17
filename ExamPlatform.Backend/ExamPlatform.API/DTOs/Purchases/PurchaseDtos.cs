using System.ComponentModel.DataAnnotations;

namespace ExamPlatform.API.DTOs.Purchases
{
    // Request body for POST /api/purchases. ExamId identifies which premium
    // exam; amount is never accepted from the client — it is always computed
    // server-side from that exam's Price.
    public class PurchaseRequestDto
    {
        [Required]
        public int ExamId { get; set; }

        // Parsed to PaymentMethod in the controller (string-DTO convention).
        [Required]
        public string Method { get; set; } = string.Empty;

        [Required]
        [StringLength(19, MinimumLength = 12, ErrorMessage = "Enter a valid card number.")]
        public string CardNumber { get; set; } = string.Empty;

        [StringLength(30)]
        public string? CouponCode { get; set; }

        [Required]
        public string IdempotencyKey { get; set; } = string.Empty;

        public bool SimulateNetworkFailure { get; set; }
        public bool SimulateTimeout { get; set; }
    }

    public class CandidatePurchaseDto
    {
        public int PurchaseId { get; set; }
        public int ExamId { get; set; }
        public string ExamTitle { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime PurchasedAt { get; set; }
        public decimal Amount { get; set; }
        public string? InvoiceNumber { get; set; }
    }
}
