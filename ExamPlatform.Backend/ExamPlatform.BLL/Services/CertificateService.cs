// CA1848: standard ILogger extension methods used deliberately — see ExamService.cs
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
    public class CertificateService : ICertificateService
    {
        // ───────────── DEPENDENCIES ─────────────
        private readonly ICertificateRepository _certRepository;

        // INotificationRepository creates in-app notifications when a certificate
        // is revoked. Both the cert UPDATE and the notification INSERT share the
        // same scoped DbContext (one HTTP request, one EF Core unit-of-work),
        // so SaveChangesAsync flushes both atomically.
        private readonly INotificationRepository _notifRepository;

        // INotificationEmailService sends a revocation email AFTER SaveChangesAsync.
        // Email failures are logged but never roll back the revocation.
        private readonly INotificationEmailService _emailService;

        private readonly ILogger<CertificateService> _logger;

        public CertificateService(
            ICertificateRepository certRepository,
            INotificationRepository notifRepository,
            INotificationEmailService emailService,
            ILogger<CertificateService> logger)
        {
            _certRepository = certRepository;
            _notifRepository = notifRepository;
            _emailService   = emailService;
            _logger         = logger;
        }

        // ════════════════════════════════════════════════════════════════
        //   GET ALL
        // ════════════════════════════════════════════════════════════════
        public async Task<IEnumerable<Certificate>> GetAllAsync()
        {
            var certs = (await _certRepository.GetAllWithUserAndExamAsync()).ToList();
            _logger.LogInformation("Admin retrieved all certificates ({Count} total).", certs.Count);
            return certs;
        }

        // ════════════════════════════════════════════════════════════════
        //   GET ALL PAGED
        // ════════════════════════════════════════════════════════════════
        public async Task<PagedResult<Certificate>> GetAllPagedAsync(int page, int pageSize)
        {
            page     = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 100);

            _logger.LogInformation(
                "Admin retrieving certificates page {Page} (pageSize={PageSize}).", page, pageSize);

            var (rawItems, totalCount) =
                await _certRepository.GetAllWithUserAndExamPagedAsync(page, pageSize);

            var items = rawItems.ToList();

            _logger.LogInformation(
                "Admin certificates page {Page}: {Count} of {Total}.",
                page, items.Count, totalCount);

            return new PagedResult<Certificate>
            {
                Items      = items,
                Page       = page,
                PageSize   = pageSize,
                TotalCount = totalCount
            };
        }

        // ════════════════════════════════════════════════════════════════
        //   GET BY ID  (Admin)
        // ════════════════════════════════════════════════════════════════
        public async Task<Certificate> GetByIdAsync(int certificateId)
        {
            var cert = await _certRepository.GetByIdWithUserAndExamAsync(certificateId);
            if (cert == null)
            {
                _logger.LogWarning("Certificate {CertId} not found.", certificateId);
                throw new CertificateNotFoundException(certificateId);
            }
            return cert;
        }

        // ════════════════════════════════════════════════════════════════
        //   GET BY USER  (Candidate — own certificates only)
        // ════════════════════════════════════════════════════════════════
        public async Task<IEnumerable<Certificate>> GetByUserAsync(int candidateId)
        {
            _logger.LogInformation(
                "Candidate {CandidateId} retrieving their certificate list.", candidateId);

            return await _certRepository.GetByUserAsync(candidateId);
        }

        // ════════════════════════════════════════════════════════════════
        //   GET BY ID WITH DETAILS  (Candidate or Admin)
        // ════════════════════════════════════════════════════════════════
        public async Task<Certificate> GetByIdWithDetailsAsync(
            int certId, int callerId, bool isAdmin)
        {
            var cert = await _certRepository.GetByIdWithUserAndExamAsync(certId);

            if (cert == null)
            {
                _logger.LogWarning(
                    "GetByIdWithDetails: certificate {CertId} not found (caller={CallerId}).",
                    certId, callerId);
                throw new CertificateNotFoundException(certId);
            }

            if (!isAdmin && cert.UserId != callerId)
            {
                _logger.LogWarning(
                    "GetByIdWithDetails: candidate {CallerId} attempted to access certificate {CertId} owned by user {OwnerId} — returning 404.",
                    callerId, certId, cert.UserId);
                throw new CertificateNotFoundException(certId);
            }

            return cert;
        }

        // ════════════════════════════════════════════════════════════════
        //   REVOKE  (Admin — also sends CertificateRevoked notification + email)
        // ════════════════════════════════════════════════════════════════
        public async Task<Certificate> RevokeAsync(int certificateId)
        {
            // GetByIdAsync calls GetByIdWithUserAndExamAsync, so cert.User and
            // cert.Exam are loaded — we can read FullName/Email/Title without
            // an additional query.
            var cert = await GetByIdAsync(certificateId);

            if (cert.CertStatus == CertificateStatus.Revoked)
            {
                _logger.LogWarning(
                    "Revoke called on certificate {CertId} ('{CertCode}') but it is already revoked; no-op.",
                    certificateId, cert.CertificateCode);
                return cert;
            }

            cert.CertStatus = CertificateStatus.Revoked;
            cert.RevokedAt  = DateTime.UtcNow;

            _certRepository.Update(cert);

            // ─── IN-APP REVOCATION NOTIFICATION ──────────────────────────
            // Notify the candidate in their notification inbox.
            // This INSERT shares the same EF Core DbContext as the cert UPDATE,
            // so both are flushed atomically in the SaveChangesAsync below.
            var examTitle = cert.Exam?.Title ?? $"Exam {cert.ExamId}";
            await _notifRepository.AddAsync(new Notification
            {
                UserId       = cert.UserId,
                NotifType    = NotificationType.CertificateRevoked,
                Message      = $"Your certificate for '{examTitle}' has been revoked.",
                RelatedExamId = cert.ExamId,
                IsRead        = false,
                CreatedAt     = DateTime.UtcNow
            });

            // Single flush: cert UPDATE + notification INSERT committed atomically.
            await _certRepository.SaveChangesAsync();

            _logger.LogInformation(
                "Certificate {CertId} ('{CertCode}') revoked for user {UserId}; CertificateRevoked notification created.",
                certificateId, cert.CertificateCode, cert.UserId);

            // ─── REVOCATION EMAIL ─────────────────────────────────────────
            // Sent AFTER SaveChangesAsync so a failed email never rolls back
            // the already-persisted revocation.
            var candidateName = cert.User?.FullName ?? cert.User?.Username ?? "Candidate";
            try
            {
                await _emailService.SendAsync(
                    cert.User!.Email,
                    $"Certificate Revoked — {examTitle}",
                    EmailTemplates.CertificateRevoked(candidateName, examTitle),
                    isHtml: true);
            }
            catch (Exception ex)
            {
                // Email failure is non-fatal — the revocation is already committed.
                _logger.LogWarning(ex,
                    "Failed to send CertificateRevoked email to user {UserId}.", cert.UserId);
            }

            return cert;
        }
    }
}
