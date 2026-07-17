using Microsoft.EntityFrameworkCore;
using ExamPlatform.DAL.Contexts;
using ExamPlatform.DAL.Interfaces;
using ExamPlatform.Domain.Entities;

namespace ExamPlatform.DAL.Repositories
{
    public class InvoiceRepository : AbstractRepository<int, Invoice>, IInvoiceRepository
    {
        public InvoiceRepository(ExamPlatformContext context) : base(context) { }

        public async Task<Invoice?> GetByPaymentIdAsync(int paymentId)
        {
            return await _dbSet.FirstOrDefaultAsync(i => i.PaymentId == paymentId);
        }

        public async Task<int> GetCountForYearAsync(int year)
        {
            return await _dbSet.CountAsync(i => i.IssuedAt.Year == year);
        }
    }
}
