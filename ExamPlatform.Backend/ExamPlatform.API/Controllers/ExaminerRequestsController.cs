// ExaminerRequestsController — manages the examiner self-service registration flow.
//
// Actors and endpoints:
//   Anonymous  → POST /api/examiner-requests               (submit a request)
//   Admin      → GET  /api/examiner-requests               (all requests)
//   Admin      → GET  /api/examiner-requests/pending       (only pending)
//   Admin      → GET  /api/examiner-requests/{id}          (single request)
//   Admin      → POST /api/examiner-requests/{id}/approve  (create account + notify)
//   Admin      → POST /api/examiner-requests/{id}/reject   (reject + notify)
//
// Security:
//   Submit is [AllowAnonymous] — anyone can apply.
//   Read and action endpoints are [Authorize(Roles="Admin")] — admin only.

#pragma warning disable CA1848
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using ExamPlatform.API.DTOs.ExaminerRequest;
using ExamPlatform.BLL.Interfaces;
using ExamPlatform.Domain.Entities;
using ExamPlatform.BLL.Models;

namespace ExamPlatform.API.Controllers
{
    [ApiController]
    [Route("api/examiner-requests")]
    [Authorize] // base guard; individual actions override where needed
    public class ExaminerRequestsController : ControllerBase
    {
        private readonly IExaminerRequestService _service;
        private readonly ILogger<ExaminerRequestsController> _logger;

        public ExaminerRequestsController(
            IExaminerRequestService service,
            ILogger<ExaminerRequestsController> logger)
        {
            _service = service;
            _logger  = logger;
        }

        // ════════════════════════════════════════════════════════════════
        //   POST /api/examiner-requests
        //   [AllowAnonymous] — no JWT required. Anyone can submit a request.
        //   201 Created on success; 409 if email already has a live request.
        // ════════════════════════════════════════════════════════════════
        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> Submit([FromBody] SubmitExaminerRequestDto dto)
        {
            _logger.LogInformation(
                "POST /api/examiner-requests: submission from {Email}.", dto.Email);

            var request = await _service.SubmitAsync(dto.FullName, dto.Email, dto.Purpose);

            return CreatedAtAction(
                nameof(GetById),
                new { id = request.ExaminerRequestId },
                ToDto(request));
        }

        // ════════════════════════════════════════════════════════════════
        //   GET /api/examiner-requests  — admin: all requests
        // ════════════════════════════════════════════════════════════════
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAll()
        {
            var requests = await _service.GetAllAsync();
            return Ok(requests.Select(ToDto));
        }

        // ════════════════════════════════════════════════════════════════
        //   GET /api/examiner-requests/pending  — admin: action queue
        //   MUST be declared BEFORE {id:int} to avoid routing ambiguity.
        // ════════════════════════════════════════════════════════════════
        [HttpGet("pending")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetPending()
        {
            var requests = await _service.GetPendingAsync();
            return Ok(requests.Select(ToDto));
        }

        // ════════════════════════════════════════════════════════════════
        //   GET /api/examiner-requests/{id}  — admin: single request
        // ════════════════════════════════════════════════════════════════
        [HttpGet("{id:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetById(int id)
        {
            var request = await _service.GetByIdAsync(id);
            return Ok(ToDto(request));
        }

        // ════════════════════════════════════════════════════════════════
        //   POST /api/examiner-requests/{id}/approve  — admin
        //   Creates the examiner account, sends email, posts notification.
        //   Returns credentials in the body so admin can relay them if
        //   email isn't configured.
        // ════════════════════════════════════════════════════════════════
        [HttpPost("{id:int}/approve")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Approve(int id)
        {
            _logger.LogInformation(
                "POST /api/examiner-requests/{Id}/approve triggered.", id);

            var result = await _service.ApproveAsync(id);

            return Ok(ToApprovalResultDto(result));
        }

        // ════════════════════════════════════════════════════════════════
        //   POST /api/examiner-requests/{id}/reject  — admin
        //   Marks as Rejected, sends rejection email, optional admin note.
        // ════════════════════════════════════════════════════════════════
        [HttpPost("{id:int}/reject")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Reject(int id, [FromBody] RejectExaminerRequestDto dto)
        {
            _logger.LogInformation(
                "POST /api/examiner-requests/{Id}/reject triggered.", id);

            var request = await _service.RejectAsync(id, dto.AdminNote);
            return Ok(ToDto(request));
        }

        // ───────────── PRIVATE MAPPERS ─────────────

        private static ExaminerRequestDto ToDto(ExaminerRequest r) => new()
        {
            ExaminerRequestId = r.ExaminerRequestId,
            FullName          = r.FullName,
            Email             = r.Email,
            Purpose           = r.Purpose,
            Status            = r.Status.ToString(),
            RequestedAt       = r.RequestedAt,
            ProcessedAt       = r.ProcessedAt,
            AdminNote         = r.AdminNote
        };

        private static ExaminerApprovalResultDto ToApprovalResultDto(ExaminerApprovalResult result) => new()
        {
            Request             = ToDto(result.Request),
            CreatedUserId       = result.CreatedUser.UserId,
            CreatedUsername     = result.CreatedUser.Username,
            CreatedUserEmail    = result.CreatedUser.Email,
            TemporaryPassword   = result.TemporaryPassword
        };
    }
}
