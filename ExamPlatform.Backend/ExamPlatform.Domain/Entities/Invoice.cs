namespace ExamPlatform.Domain.Entities
{
    // Maps to "invoices". One-to-one with a Successful Payment. Rendered
    // client-side from these structured fields — no PDF blob is stored
    // server-side, consistent with how the app never persists rendered
    // artifacts (certificates render from data too).
    public class Invoice
    {
        public int InvoiceId { get; set; }              // PK (internal)

        public int PaymentId { get; set; }               // FK -> Payment, unique

        public string InvoiceNumber { get; set; } = null!;   // unique, e.g. "INV-2026-000123"

        // Clearly-fake demo GST number — a fixed placeholder format, never a
        // real GSTIN.
        public string FakeGstNumber { get; set; } = null!;

        // Snapshotted at issuance so the invoice never changes even if the
        // user later edits their profile.
        public string BillingName { get; set; } = null!;
        public string BillingEmail { get; set; } = null!;

        public decimal Subtotal { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal Total { get; set; }

        public DateTime IssuedAt { get; set; }

        public Payment Payment { get; set; } = null!;
    }
}
