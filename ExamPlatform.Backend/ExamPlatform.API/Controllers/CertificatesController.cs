// CertificatesController exposes certificate data to two actor types:
//
//   Admin  — platform-wide list, per-ID lookup, revocation
//   Candidate — their own certificate list, per-ID detail, download
//
// Security model:
//   Admins can read and revoke any certificate.
//   Candidates can only read certificates where cert.UserId == their JWT userId.
//   Ownership enforcement is in the service layer (GetByIdWithDetailsAsync);
//   the controller communicates isAdmin=true/false to let the service decide.

// CA1848: standard ILogger extension methods used deliberately — see CertificateService.cs
#pragma warning disable CA1848
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using ExamPlatform.API.DTOs.Certificate;
using ExamPlatform.API.DTOs.Common;
using ExamPlatform.BLL.Interfaces;
using ExamPlatform.Domain.Entities;

namespace ExamPlatform.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // base-level JWT check; each action restricts further by role
    public class CertificatesController : ControllerBase
    {
        private readonly ICertificateService _certificateService;
        private readonly ILogger<CertificatesController> _logger;

        public CertificatesController(
            ICertificateService certificateService,
            ILogger<CertificatesController> logger)
        {
            _certificateService = certificateService;
            _logger = logger;
        }

        // ════════════════════════════════════════════════════════════════
        //   GET /api/certificates  — admin: paginated list of all certs
        //   Supports: ?page=1 &pageSize=20
        //   pageSize clamped to [1, 100] by the service.
        // ════════════════════════════════════════════════════════════════
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAll(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            _logger.LogInformation(
                "GET /api/certificates: admin page {Page} pageSize {PageSize}.", page, pageSize);

            var paged = await _certificateService.GetAllPagedAsync(page, pageSize);

            return Ok(new PagedResultDto<CertificateDto>
            {
                Items           = paged.Items.Select(ToAdminDto),
                Page            = paged.Page,
                PageSize        = paged.PageSize,
                TotalCount      = paged.TotalCount,
                TotalPages      = paged.TotalPages,
                HasNextPage     = paged.HasNextPage,
                HasPreviousPage = paged.HasPreviousPage
            });
        }

        // ════════════════════════════════════════════════════════════════
        //   GET /api/certificates/mine  — candidate: own certificate list
        //   MUST be declared BEFORE {id:int} to avoid routing ambiguity.
        //   ASP.NET Core: literal segment "mine" beats the int-constrained
        //   "{id:int}" template when both have the same prefix.
        // ════════════════════════════════════════════════════════════════
        [HttpGet("mine")]
        [Authorize(Roles = "Candidate")]
        public async Task<IActionResult> GetMine()
        {
            var userId = ExtractUserId();
            _logger.LogInformation(
                "GET /api/certificates/mine: candidate {UserId} retrieving own certificates.", userId);

            var certs = await _certificateService.GetByUserAsync(userId);

            // Map to the lean candidate DTO — no UserId, no AttemptId exposure.
            return Ok(certs.Select(ToCandidateDto));
        }

        // ════════════════════════════════════════════════════════════════
        //   GET /api/certificates/mine/{id}  — candidate: own cert detail
        //   Ownership enforced by GetByIdWithDetailsAsync (isAdmin=false).
        //   Returns 404 if the cert doesn't exist OR belongs to another user.
        // ════════════════════════════════════════════════════════════════
        [HttpGet("mine/{id:int}")]
        [Authorize(Roles = "Candidate")]
        public async Task<IActionResult> GetMineById(int id)
        {
            var userId = ExtractUserId();
            // isAdmin=false → service checks cert.UserId == userId
            var cert = await _certificateService.GetByIdWithDetailsAsync(id, userId, isAdmin: false);
            return Ok(ToCandidateDto(cert));
        }

        // ════════════════════════════════════════════════════════════════
        //   GET /api/certificates/{id}  — admin: single cert detail
        //   Full CertificateDto including UserId, UserFullName, AttemptId.
        // ════════════════════════════════════════════════════════════════
        [HttpGet("{id:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetById(int id)
        {
            var cert = await _certificateService.GetByIdAsync(id);
            return Ok(ToAdminDto(cert));
        }

        // ════════════════════════════════════════════════════════════════
        //   GET /api/certificates/{id}/download
        //   Accessible to: Admin (any cert) OR Candidate who owns the cert.
        //   Returns a CertificateDownloadDto — a rich JSON payload the
        //   frontend can render as a printable/shareable certificate card.
        //
        //   Why not a PDF stream?
        //     Adding a PDF library (QuestPDF, iTextSharp) introduces a build
        //     dependency and generation complexity. The DTO approach is framework-
        //     agnostic: the frontend (React/Vue/Blazor) can layout the certificate
        //     however it likes, or call window.print(). A future migration to a
        //     PDF stream only requires changing this action's return type and the
        //     Content-Type header — the service call stays identical.
        // ════════════════════════════════════════════════════════════════
        [HttpGet("{id:int}/download")]
        public async Task<IActionResult> Download(int id)
        {
            var userId = ExtractUserId();
            var isAdmin = User.IsInRole("Admin");

            _logger.LogInformation(
                "GET /api/certificates/{CertId}/download: caller {UserId} isAdmin={IsAdmin}.",
                id, userId, isAdmin);

            // Service handles both authorization cases:
            //   Admin  → any cert, no ownership check
            //   Candidate → cert.UserId must equal userId
            var cert = await _certificateService.GetByIdWithDetailsAsync(id, userId, isAdmin);

            return Ok(ToDownloadDto(cert));
        }

        // ════════════════════════════════════════════════════════════════
        //   POST /api/certificates/{id}/revoke  — admin revokes + notifies
        // ════════════════════════════════════════════════════════════════
        [HttpPost("{id:int}/revoke")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Revoke(int id)
        {
            _logger.LogInformation(
                "POST /api/certificates/{CertId}/revoke triggered.", id);
            var cert = await _certificateService.RevokeAsync(id);
            return Ok(ToAdminDto(cert));
        }

        // ───────────── PRIVATE HELPERS ─────────────

        /// <summary>
        /// Extracts the calling user's integer ID from the JWT claims.
        /// Throws UnauthorizedAccessException if the claim is absent or malformed —
        /// the global exception middleware converts that to HTTP 401.
        /// </summary>
        private int ExtractUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                        ?? User.FindFirst("sub")?.Value
                        ?? User.FindFirst("nameid")?.Value;

            if (!int.TryParse(claim, out var id))
                throw new UnauthorizedAccessException("Invalid token: missing user id claim.");
            return id;
        }

        /// <summary>
        /// Full admin-facing DTO — includes UserId, UserFullName, AttemptId,
        /// and RevokedAt so the admin dashboard shows complete audit information.
        /// </summary>
        private static CertificateDto ToAdminDto(Certificate c) => new()
        {
            CertificateId   = c.CertificateId,
            CertificateCode = c.CertificateCode,
            UserId          = c.UserId,
            UserFullName    = c.User?.FullName,    // populated via Include
            ExamId          = c.ExamId,
            ExamTitle       = c.Exam?.Title,        // populated via Include
            AttemptId       = c.AttemptId,
            Score           = c.Score,
            IssuedAt        = c.IssuedAt,
            CertStatus      = c.CertStatus.ToString(),
            RevokedAt       = c.RevokedAt
        };

        /// <summary>
        /// Lean candidate-facing DTO — omits UserId (they know it's theirs),
        /// AttemptId (not actionable for a candidate), and exposes ExamTitle
        /// so the list is readable without a follow-up GET /api/exams/{id} call.
        /// </summary>
        private static CandidateCertificateDto ToCandidateDto(Certificate c) => new()
        {
            CertificateId   = c.CertificateId,
            CertificateCode = c.CertificateCode,
            ExamId          = c.ExamId,
            ExamTitle       = c.Exam?.Title,
            Score           = c.Score,
            IssuedAt        = c.IssuedAt,
            CertStatus      = c.CertStatus.ToString(),
            RevokedAt       = c.RevokedAt,
            IsPremium         = c.IsPremium,
            InstituteName     = c.InstituteName,
            InstituteLogoUrl  = c.InstituteLogoUrl
        };

        /// <summary>
        /// Download DTO — includes candidate name, exam title, total marks,
        /// and a pre-formatted certificate text string for easy frontend rendering.
        /// The CertificateText field can be used verbatim or as a print/share fallback.
        /// </summary>
        private static CertificateDownloadDto ToDownloadDto(Certificate c)
        {
            var candidateName = c.User?.FullName ?? "Candidate";
            var examTitle     = c.Exam?.Title ?? $"Exam {c.ExamId}";
            var totalMarks    = c.Exam?.TotalMarks ?? 0m;

            // Pre-formatted certificate body. The frontend can render this as a
            // banner or use the individual fields for a custom layout.
            var certText = $"This certifies that {candidateName} has successfully " +
                           $"completed \"{examTitle}\" with a score of " +
                           $"{c.Score} / {totalMarks}, " +
                           $"issued on {c.IssuedAt:MMMM dd, yyyy}.";

            return new CertificateDownloadDto
            {
                CertificateId   = c.CertificateId,
                CertificateCode = c.CertificateCode,
                CandidateName   = candidateName,
                ExamTitle       = examTitle,
                TotalMarks      = totalMarks,
                Score           = c.Score,
                IssuedAt        = c.IssuedAt,
                CertStatus      = c.CertStatus.ToString(),
                CertificateText = certText,
                IsPremium        = c.IsPremium,
                InstituteName    = c.InstituteName,
                InstituteLogoUrl = c.InstituteLogoUrl
            };
        }
    }
}
