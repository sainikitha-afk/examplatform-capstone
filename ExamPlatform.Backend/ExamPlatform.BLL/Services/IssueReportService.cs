// CA1848: standard ILogger extension methods used deliberately — see CertificateService.cs
#pragma warning disable CA1848
using Microsoft.Extensions.Logging;
using ExamPlatform.BLL.Exceptions;
using ExamPlatform.BLL.Interfaces;
using ExamPlatform.BLL.Models;
using ExamPlatform.DAL.Interfaces;
using ExamPlatform.Domain.Entities;
using ExamPlatform.Domain.Enums;

namespace ExamPlatform.BLL.Services
{
    // IssueReportService owns the candidate "flag / report a problem" workflow.
    // Raising a report is intentionally forgiving: it must NEVER throw in a way
    // that blocks a candidate who is mid-exam and already frustrated. It performs
    // light validation, persists the report, and best-effort notifies the examiner.
    public class IssueReportService : IIssueReportService
    {
        private readonly IIssueReportRepository _reportRepository;
        private readonly IExamRepository _examRepository;
        private readonly IUserRepository _userRepository;
        private readonly INotificationRepository _notifRepository;
        private readonly ILogger<IssueReportService> _logger;

        // Guardrails so a report can't be blank or abusively large. The DB column
        // caps at 1000; we validate before hitting it so the message is friendly.
        private const int MinMessageLength = 3;
        private const int MaxMessageLength = 1000;

        public IssueReportService(
            IIssueReportRepository reportRepository,
            IExamRepository examRepository,
            IUserRepository userRepository,
            INotificationRepository notifRepository,
            ILogger<IssueReportService> logger)
        {
            _reportRepository = reportRepository;
            _examRepository = examRepository;
            _userRepository = userRepository;
            _notifRepository = notifRepository;
            _logger = logger;
        }

        // ════════════════════════════════════════════════════════════════
        //   RaiseAsync — candidate submits a report
        // ════════════════════════════════════════════════════════════════
        public async Task<IssueReport> RaiseAsync(
            int candidateId, int examId, int? attemptId, int? questionId, string message)
        {
            // ── Validate the message (trim first so "   " counts as empty) ──
            message = (message ?? string.Empty).Trim();
            if (message.Length < MinMessageLength)
                throw new ValidationException(
                    "Please describe the problem in a few words so the examiner can help.");
            if (message.Length > MaxMessageLength)
                throw new ValidationException(
                    $"Your message is too long (max {MaxMessageLength} characters).");

            // ── The exam must exist. 404 if not. ──
            var exam = await _examRepository.GetByIdAsync(examId)
                ?? throw new ExamNotFoundException(examId);

            // ── Persist the report ──
            var report = new IssueReport
            {
                CandidateId = candidateId,
                ExamId = examId,
                AttemptId = attemptId,
                QuestionId = questionId,
                Message = message,
                Status = IssueStatus.Open,
                CreatedAt = DateTime.UtcNow
            };
            await _reportRepository.AddAsync(report);
            await _reportRepository.SaveChangesAsync();

            _logger.LogInformation(
                "IssueReport {ReportId} raised by candidate {CandidateId} on exam {ExamId} (question {QuestionId}).",
                report.IssueReportId, candidateId, examId, questionId);

            // ── Best-effort notify the examiner. A notification failure must
            //    never undo a successfully-saved report. ──
            try
            {
                var candidate = await _userRepository.GetByIdAsync(candidateId);
                var who = candidate?.FullName ?? "A candidate";
                var scope = questionId.HasValue
                    ? "a specific question"
                    : "their exam experience";

                await _notifRepository.AddAsync(new Notification
                {
                    UserId = exam.ExaminerId,
                    NotifType = NotificationType.IssueReported,
                    Message = $"{who} reported an issue about {scope} on '{exam.Title}'. " +
                              "Open the Issue Reports panel to review it.",
                    RelatedExamId = examId,
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                });
                await _notifRepository.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                // Log and swallow — the report itself is safely stored.
                _logger.LogError(ex,
                    "IssueReport {ReportId} saved but examiner notification failed.",
                    report.IssueReportId);
            }

            return report;
        }

        // ════════════════════════════════════════════════════════════════
        //   GetForExaminerPagedAsync — examiner inbox
        // ════════════════════════════════════════════════════════════════
        public async Task<PagedResult<IssueReport>> GetForExaminerPagedAsync(
            int examinerId, int page, int pageSize, bool openOnly)
        {
            if (page < 1) page = 1;
            pageSize = Math.Clamp(pageSize, 1, 100);

            var (items, total) = await _reportRepository.GetForExaminerPagedAsync(
                examinerId, page, pageSize, openOnly);

            return new PagedResult<IssueReport>
            {
                Items = items,
                Page = page,
                PageSize = pageSize,
                TotalCount = total
            };
        }

        public Task<int> OpenCountForExaminerAsync(int examinerId)
            => _reportRepository.OpenCountForExaminerAsync(examinerId);

        // ════════════════════════════════════════════════════════════════
        //   UpdateStatusAsync — examiner triages a report
        // ════════════════════════════════════════════════════════════════
        public async Task<IssueReport> UpdateStatusAsync(
            int issueReportId, int examinerId, IssueStatus status, string? resolutionNote)
        {
            var report = await _reportRepository.GetByIdWithContextAsync(issueReportId)
                ?? throw new IssueReportNotFoundException(issueReportId);

            // Ownership: the examiner must own the exam the report is about.
            // 404 (not 403) so we don't leak other examiners' reports.
            if (report.Exam == null || report.Exam.ExaminerId != examinerId)
                throw new IssueReportNotFoundException(issueReportId);

            if (resolutionNote != null)
            {
                resolutionNote = resolutionNote.Trim();
                if (resolutionNote.Length > MaxMessageLength)
                    throw new ValidationException(
                        $"The resolution note is too long (max {MaxMessageLength} characters).");
                report.ResolutionNote = resolutionNote.Length == 0 ? null : resolutionNote;
            }

            report.Status = status;
            report.ResolvedAt = status == IssueStatus.Resolved ? DateTime.UtcNow : null;

            _reportRepository.Update(report);
            await _reportRepository.SaveChangesAsync();

            _logger.LogInformation(
                "IssueReport {ReportId} moved to {Status} by examiner {ExaminerId}.",
                issueReportId, status, examinerId);

            // On resolution, let the candidate know their voice was heard.
            if (status == IssueStatus.Resolved)
            {
                try
                {
                    await _notifRepository.AddAsync(new Notification
                    {
                        UserId = report.CandidateId,
                        NotifType = NotificationType.IssueResolved,
                        Message = $"The issue you reported on '{report.Exam!.Title}' has been resolved" +
                                  (report.ResolutionNote != null ? $": {report.ResolutionNote}" : ".") ,
                        RelatedExamId = report.ExamId,
                        IsRead = false,
                        CreatedAt = DateTime.UtcNow
                    });
                    await _notifRepository.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "IssueReport {ReportId} resolved but candidate notification failed.",
                        issueReportId);
                }
            }

            return report;
        }
    }
}
