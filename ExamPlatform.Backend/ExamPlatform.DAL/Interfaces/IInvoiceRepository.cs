using ExamPlatform.Domain.Entities;

namespace ExamPlatform.DAL.Interfaces
{
    public interface IInvoiceRepository : IRepository<int, Invoice>
    {
        Task<Invoice?> GetByPaymentIdAsync(int paymentId);

        // Count of invoices already issued this calendar year — used to build
        // the next sequential InvoiceNumber ("INV-{year}-{6-digit sequence}").
        Task<int> GetCountForYearAsync(int year);
    }
}
