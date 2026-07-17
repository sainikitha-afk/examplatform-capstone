using ExamPlatform.BLL.Models;
using ExamPlatform.Domain.Entities;

namespace ExamPlatform.BLL.Interfaces
{
    public interface IInvoiceService
    {
        // Generates and persists one Invoice for an already-Successful Payment.
        // Called at most once per payment — Invoice.PaymentId is unique.
        Task<Invoice> GenerateAsync(InvoiceRequest request);
    }
}
