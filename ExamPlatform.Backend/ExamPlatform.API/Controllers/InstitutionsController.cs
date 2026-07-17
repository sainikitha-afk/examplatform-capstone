// InstitutionsController — Institute Verification badge (Part 3/7).
// Mirrors ExaminerRequestsController's request/decision shape.

#pragma warning disable CA1848
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using ExamPlatform.API.DTOs.Institutions;
using ExamPlatform.BLL.Interfaces;
using ExamPlatform.Domain.Entities;

namespace ExamPlatform.API.Controllers
{
    [ApiController]
    [Route("api/institutions")]
    [Authorize]
    public class InstitutionsController : ControllerBase
    {
        private readonly IInstitutionVerificationService _service;
        private readonly ILogger<InstitutionsController> _logger;

        public InstitutionsController(
            IInstitutionVerificationService service,
            ILogger<InstitutionsController> logger)
        {
            _service = service;
            _logger = logger;
        }

        // ════════════════════════════════════════════════════════════════
        //   POST /api/institutions  — examiner: submit for verification
        // ════════════════════════════════════════════════════════════════
        [HttpPost]
        [Authorize(Roles = "Examiner")]
        public async Task<IActionResult> Submit([FromBody] SubmitInstitutionDto dto)
        {
            var userId = ExtractUserId();
            _logger.LogInformation("POST /api/institutions: examiner {UserId} submitting '{Name}'.", userId, dto.Name);

            var institution = await _service.SubmitAsync(userId, dto.Name, dto.LogoUrl);
            return Ok(ToDto(institution));
        }

        // ════════════════════════════════════════════════════════════════
        //   GET /api/institutions/mine  — examiner: my own request/status
        // ════════════════════════════════════════════════════════════════
        [HttpGet("mine")]
        [Authorize(Roles = "Examiner")]
        public async Task<IActionResult> GetMine()
        {
            var userId = ExtractUserId();
            var institution = await _service.GetMineAsync(userId);
            return institution == null ? Ok((InstitutionDto?)null) : Ok(ToDto(institution));
        }

        // ════════════════════════════════════════════════════════════════
        //   GET /api/institutions/pending  — admin: action queue
        // ════════════════════════════════════════════════════════════════
        [HttpGet("pending")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetPending()
        {
            var institutions = await _service.GetPendingAsync();
            return Ok(institutions.Select(ToDto));
        }

        // ════════════════════════════════════════════════════════════════
        //   POST /api/institutions/{id}/approve  — admin
        // ════════════════════════════════════════════════════════════════
        [HttpPost("{id:int}/approve")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Approve(int id)
        {
            var adminId = ExtractUserId();
            _logger.LogInformation("POST /api/institutions/{Id}/approve by admin {AdminId}.", id, adminId);

            var institution = await _service.ApproveAsync(id, adminId);
            return Ok(ToDto(institution));
        }

        // ════════════════════════════════════════════════════════════════
        //   POST /api/institutions/{id}/reject  — admin
        // ════════════════════════════════════════════════════════════════
        [HttpPost("{id:int}/reject")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Reject(int id)
        {
            var adminId = ExtractUserId();
            _logger.LogInformation("POST /api/institutions/{Id}/reject by admin {AdminId}.", id, adminId);

            var institution = await _service.RejectAsync(id, adminId);
            return Ok(ToDto(institution));
        }

        // ───────────── PRIVATE HELPERS ─────────────

        private int ExtractUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                        ?? User.FindFirst("sub")?.Value
                        ?? User.FindFirst("nameid")?.Value;

            if (!int.TryParse(claim, out var id))
                throw new UnauthorizedAccessException("Invalid token: missing user id claim.");
            return id;
        }

        private static InstitutionDto ToDto(Institution i) => new()
        {
            InstitutionId = i.InstitutionId,
            Name = i.Name,
            LogoUrl = i.LogoUrl,
            VerificationStatus = i.VerificationStatus.ToString(),
            RequestedAt = i.RequestedAt,
            VerifiedAt = i.VerifiedAt,
            OwnerUserId = i.OwnerUserId,
            OwnerName = i.Owner?.FullName
        };
    }
}
