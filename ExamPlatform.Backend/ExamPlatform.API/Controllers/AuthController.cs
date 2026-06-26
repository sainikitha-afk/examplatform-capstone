// CA1848: standard ILogger extension methods used deliberately — see CertificateService.cs
#pragma warning disable CA1848
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using ExamPlatform.API.DTOs.Auth;
using ExamPlatform.API.Helpers;
using ExamPlatform.BLL.Interfaces;
using ExamPlatform.Domain.Entities;

namespace ExamPlatform.API.Controllers
{
    // [ApiController] enables:
    //   - Automatic model state validation (DataAnnotation failures → 400)
    //   - Inferred parameter binding (complex types → [FromBody] by default)
    //   - Required attribute routing
    [ApiController]

    // Route prefix. [controller] is a route token replaced at startup by the
    // class name (minus the "Controller" suffix). So AuthController → "api/auth".
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        // ───────────── DEPENDENCIES (Constructor Injection) ─────────────
        private readonly IAuthService _authService;
        private readonly JwtTokenGenerator _tokenGenerator;

        // ILogger<AuthController>: logs HTTP-layer events (request received,
        // outcome). The service already logs business-layer detail; the controller
        // logs the HTTP boundary so the two can be correlated in output.
        private readonly ILogger<AuthController> _logger;

        // DI hands us all three at request time. The controller is itself a
        // scoped service (one per request) — same lifetime as the services
        // it consumes, keeping the entire request on a single DbContext.
        public AuthController(
            IAuthService authService,
            JwtTokenGenerator tokenGenerator,
            ILogger<AuthController> logger)
        {
            _authService = authService;
            _tokenGenerator = tokenGenerator;
            _logger = logger;
        }

        // ════════════════════════════════════════════════════════════════
        //   POST /api/auth/register
        // ════════════════════════════════════════════════════════════════
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            // Note: DataAnnotation validation on RegisterDto runs BEFORE this
            // method due to [ApiController]. If FullName is missing or password
            // too short, ASP.NET already returned 400 — we never get here.
            //
            // BUSINESS validation (duplicate email, etc.) happens in the
            // service, which throws exceptions that the global middleware
            // (coming up next) will translate to HTTP responses. For now,
            // exceptions will bubble as 500 — we'll fix that with middleware.

            // Delegate to the service.
            var user = await _authService.RegisterAsync(
                dto.FullName,
                dto.Username,
                dto.Email,
                dto.Password);

            // Service returned a User. Generate a token and shape the response.
            var token = _tokenGenerator.GenerateToken(user);
            var response = ToAuthResponse(user, token);

            _logger.LogInformation(
                "POST /api/auth/register completed: userId={UserId}.", user.UserId);

            // 201 Created is the proper status for a successful registration.
            // We don't have a "GET /api/users/{id}" endpoint yet, so we just
            // return 200 OK with the body. (When we add user GET, we'll switch
            // to CreatedAtAction.)
            return Ok(response);
        }

        // ════════════════════════════════════════════════════════════════
        //   POST /api/auth/login
        // ════════════════════════════════════════════════════════════════
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            var user = await _authService.LoginAsync(dto.EmailOrUsername, dto.Password);

            var token = _tokenGenerator.GenerateToken(user);
            var response = ToAuthResponse(user, token);

            _logger.LogInformation(
                "POST /api/auth/login completed: userId={UserId}, role={Role}.",
                user.UserId, user.Role);

            return Ok(response);
        }

        // ───────────── PRIVATE MAPPER ─────────────
        // Local helper to convert a User + token into the response DTO.
        // Kept private because no other class needs this exact mapping.
        // If this mapping is duplicated elsewhere later, we'll extract it
        // into a Mappers/ folder. Until then, here is fine.
        private static AuthResponseDto ToAuthResponse(User user, string token) =>
            new AuthResponseDto
            {
                Token = token,
                UserId = user.UserId,
                FullName = user.FullName,
                Username = user.Username,
                Email = user.Email,
                Role = user.Role.ToString()
            };
    }
}