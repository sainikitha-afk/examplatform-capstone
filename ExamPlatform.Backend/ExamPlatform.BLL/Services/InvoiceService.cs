using ExamPlatform.BLL.Interfaces;
using ExamPlatform.BLL.Models;
using ExamPlatform.DAL.Interfaces;
using ExamPlatform.Domain.Entities;

namespace ExamPlatform.BLL.Services
{
    // Generates the Invoice for a successful Payment. Rendered client-side
    // from these structured fields — no PDF is stored server-side (Part 5).
    public class InvoiceService : IInvoiceService
    {
        // A fixed, clearly-fake placeholder — never a real GSTIN. "Fixed"
        // deliberately: this is simulated platform infrastructure info, not
        // something that varies per payment.
        private const string FakeGstNumber = "29AAAAA0000EXM1Z5";

        // Simulated flat GST rate applied on (Subtotal - Discount).
        private const decimal GstRate = 0.18m;

        private readonly IInvoiceRepository _invoiceRepository;

        public InvoiceService(IInvoiceRepository invoiceRepository)
        {
            _invoiceRepository = invoiceRepository;
        }

        public async Task<Invoice> GenerateAsync(InvoiceRequest request)
        {
            var year = request.Now.Year;
            var countThisYear = await _invoiceRepository.GetCountForYearAsync(year);
            var invoiceNumber = $"INV-{year}-{countThisYear + 1:D6}";

            var taxableAmount = request.Subtotal - request.DiscountAmount;
            var taxAmount = Math.Round(taxableAmount * GstRate, 2);
            var total = taxableAmount + taxAmount;

            var invoice = new Invoice
            {
                PaymentId = request.PaymentId,
                InvoiceNumber = invoiceNumber,
                FakeGstNumber = FakeGstNumber,
                BillingName = request.BillingName,
                BillingEmail = request.BillingEmail,
                Subtotal = request.Subtotal,
                DiscountAmount = request.DiscountAmount,
                TaxAmount = taxAmount,
                Total = total,
                IssuedAt = request.Now
            };

            await _invoiceRepository.AddAsync(invoice);
            await _invoiceRepository.SaveChangesAsync();

            return invoice;
        }
    }
}
