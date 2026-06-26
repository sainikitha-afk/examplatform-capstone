// CA1848: standard ILogger extension methods used deliberately — see CertificateService.cs
#pragma warning disable CA1848
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using ExamPlatform.API.DTOs.Auth;
using ExamPlatform.API.DTOs.Common;
using ExamPlatform.API.DTOs.User;
using ExamPlatform.API.Helpers;
using ExamPlatform.BLL.Interfaces;
using ExamPlatform.Domain.Entities;

namespace ExamPlatform.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]   // default: any authenticated user
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly IAuthService _authService;
        private readonly JwtTokenGenerator _tokenGenerator;
        private readonly ILogger<UsersController> _logger;

        public UsersController(
            IUserService userService,
            IAuthService authService,
            JwtTokenGenerator tokenGenerator,
            ILogger<UsersController> logger)
        {
            _userService = userService;
            _authService = authService;
            _tokenGenerator = tokenGenerator;
            _logger = logger;
        }

        // ════════════════════════════════════════════════════════════════
        //   GET /api/users/me  — current user's profile
        //   Available to any authenticated user.
        // ════════════════════════════════════════════════════════════════
        [HttpGet("me")]
        public async Task<IActionResult> GetMe()
        {
            // Pull the user's id from the JWT claims that middleware populated
            // on HttpContext.User. The "sub" claim was the UserId we baked in.
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                              ?? User.FindFirst("sub")?.Value
                              ?? User.FindFirst("nameid")?.Value;

            if (!int.TryParse(userIdClaim, out var userId))
                return Unauthorized(new { error = "Invalid token claims." });

            var user = await _userService.GetByIdAsync(userId);
            return Ok(ToDetailDto(user));
        }

        // ════════════════════════════════════════════════════════════════
        //   GET /api/users  — admin paginated list of all users
        //   Supports: ?page=1 &pageSize=20
        //   pageSize clamped to [1, 100] by the service — values > 100 silently
        //   become 100 rather than returning a 400 (a convenience, not a bug).
        // ════════════════════════════════════════════════════════════════
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAll(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            _logger.LogInformation(
                "GET /api/users: admin page {Page} pageSize {PageSize}.", page, pageSize);

            var paged = await _userService.GetAllPagedAsync(page, pageSize);

            return Ok(new PagedResultDto<UserSummaryDto>
            {
                Items           = paged.Items.Select(ToSummaryDto),
                Page            = paged.Page,
                PageSize        = paged.PageSize,
                TotalCount      = paged.TotalCount,
                TotalPages      = paged.TotalPages,
                HasNextPage     = paged.HasNextPage,
                HasPreviousPage = paged.HasPreviousPage
            });
        }

        // ════════════════════════════════════════════════════════════════
        //   GET /api/users/{id}  — admin drill-down on one user
        // ════════════════════════════════════════════════════════════════
        [HttpGet("{id:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetById(int id)
        {
            var user = await _userService.GetByIdAsync(id);
            return Ok(ToSummaryDto(user));
        }

        // ════════════════════════════════════════════════════════════════
        //   POST /api/users/examiners  — admin creates an examiner account
        // ════════════════════════════════════════════════════════════════
        [HttpPost("examiners")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateExaminer([FromBody] CreateExaminerDto dto)
        {
            var user = await _authService.CreateExaminerAsync(
                dto.FullName, dto.Username, dto.Email, dto.Password);

            _logger.LogInformation(
                "POST /api/users/examiners: created examinerId={UserId} ('{Username}').",
                user.UserId, user.Username);

            // We DON'T return a token here — admin created this account FOR
            // someone else. That examiner logs in themselves to get their token.
            return CreatedAtAction(nameof(GetById), new { id = user.UserId }, ToSummaryDto(user));
        }

        // ════════════════════════════════════════════════════════════════
        //   PATCH /api/users/{id}/active  — admin toggles is_active
        // ════════════════════════════════════════════════════════════════
        [HttpPatch("{id:int}/active")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> SetActiveStatus(int id, [FromBody] UpdateActiveStatusDto dto)
        {
            var user = await _userService.SetActiveStatusAsync(id, dto.IsActive);
            return Ok(ToSummaryDto(user));
        }

        // ───────────── PRIVATE MAPPERS ─────────────
        private static UserSummaryDto ToSummaryDto(User u) => new()
        {
            UserId = u.UserId,
            FullName = u.FullName,
            Username = u.Username,
            Email = u.Email,
            Role = u.Role.ToString(),
            IsActive = u.IsActive,
            DateOfJoining = u.DateOfJoining
        };

        private static UserDetailDto ToDetailDto(User u) => new()
        {
            UserId = u.UserId,
            FullName = u.FullName,
            Username = u.Username,
            Email = u.Email,
            Role = u.Role.ToString(),
            IsActive = u.IsActive,
            DateOfJoining = u.DateOfJoining
        };
    }
}