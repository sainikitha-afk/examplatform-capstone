using Microsoft.EntityFrameworkCore;
using ExamPlatform.DAL.Contexts;
using ExamPlatform.DAL.Interfaces;
using ExamPlatform.Domain.Entities;
using ExamPlatform.Domain.Enums;

namespace ExamPlatform.DAL.Repositories
{
    // IssueReportRepository scopes candidate-raised reports to the examiner who
    // owns the exam, joining Exam so an examiner only ever sees reports about
    // exams they created.
    public class IssueReportRepository : AbstractRepository<int, IssueReport>, IIssueReportRepository
    {
        public IssueReportRepository(ExamPlatformContext context) : base(context) { }

        // Reports for exams owned by this examiner. The join to Exams enforces
        // ownership at the database layer (an examiner can never see another
        // examiner's issue reports).
        public async Task<(IEnumerable<IssueReport> Items, int TotalCount)> GetForExaminerPagedAsync(
            int examinerId, int page, int pageSize, bool openOnly)
        {
            var query =
                from r in _dbSet
                join e in _context.Exams on r.ExamId equals e.ExamId
                where e.ExaminerId == examinerId
                select r;

            if (openOnly)
                query = query.Where(r => r.Status == IssueStatus.Open);

            query = query.OrderByDescending(r => r.CreatedAt);

            var totalCount = await query.CountAsync();
            var items = await query
                .Include(r => r.Candidate)
                .Include(r => r.Exam)
                .Include(r => r.Question)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        public async Task<int> OpenCountForExaminerAsync(int examinerId)
        {
            return await (
                from r in _dbSet
                join e in _context.Exams on r.ExamId equals e.ExamId
                where e.ExaminerId == examinerId && r.Status == IssueStatus.Open
                select r).CountAsync();
        }

        public async Task<IssueReport?> GetByIdWithContextAsync(int issueReportId)
        {
            return await _dbSet
                .Include(r => r.Candidate)
                .Include(r => r.Exam)
                .Include(r => r.Question)
                .FirstOrDefaultAsync(r => r.IssueReportId == issueReportId);
        }
    }
}
