namespace ExamPlatform.API.DTOs.Payments
{
    // Response shape for a Payment — returned by both the subscription
    // upgrade and the premium-exam purchase endpoints, success or failure.
    // The frontend branches on Status; FakeTransactionId/FakeGatewayReference
    // are shown on the receipt/invoice regardless of outcome.
    public class PaymentDto
    {
        public int PaymentId { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "INR";
        public string Method { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string FakeTransactionId { get; set; } = string.Empty;
        public string FakeGatewayReference { get; set; } = string.Empty;
        public string? FailureReason { get; set; }
        public DateTime InitiatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public InvoiceDto? Invoice { get; set; }
    }

    public class InvoiceDto
    {
        public string InvoiceNumber { get; set; } = string.Empty;
        public string FakeGstNumber { get; set; } = string.Empty;
        public string BillingName { get; set; } = string.Empty;
        public string BillingEmail { get; set; } = string.Empty;
        public decimal Subtotal { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal Total { get; set; }
        public DateTime IssuedAt { get; set; }
    }
}
