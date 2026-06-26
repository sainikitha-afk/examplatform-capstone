using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using ExamPlatform.Domain.Entities;

namespace ExamPlatform.API.Helpers
{
    // Generates a signed JWT for an authenticated user.
    //
    // Why in API and not BLL?
    //   - JWT is a transport mechanism (HTTP Authorization header).
    //   - Configuration (key, issuer, audience) is API-layer concern.
    //   - BLL stays HTTP-agnostic — it returns User entities; this helper
    //     converts them into tokens at the HTTP boundary.
    public class JwtTokenGenerator
    {
        private readonly IConfiguration _configuration;

        public JwtTokenGenerator(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        // Returns: the signed JWT string ready to be sent to the client.
        public string GenerateToken(User user)
        {
            // ─── STEP 1: READ CONFIG ───────────────────────────────────
            // Pull JWT settings from appsettings.json. The '!' is the
            // null-forgiving operator — we assert these MUST be present
            // (the app should fail at startup if config is missing).
            var jwtKey = _configuration["Jwt:Key"]!;
            var jwtIssuer = _configuration["Jwt:Issuer"]!;
            var jwtAudience = _configuration["Jwt:Audience"]!;
            
            var expiryMinutes = int.Parse(_configuration["Jwt:ExpiryMinutes"] ?? "60");

            // ─── STEP 2: BUILD THE SIGNING CREDENTIALS ─────────────────
            // SymmetricSecurityKey: HMAC algorithms (HS256/HS384/HS512) use
            // the same secret key to sign and verify. The key must be ≥256 bits.
            // We get those bits from UTF-8 bytes of the configured string.
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));

            // The credentials specify HOW to sign — algorithm + key.
            // HmacSha256 is the industry-standard symmetric JWT algorithm.
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            // ─── STEP 3: BUILD CLAIMS ──────────────────────────────────
            // Claims are key-value pairs embedded in the token. Anyone with
            // the token can decode and read them (they're NOT encrypted —
            // they're just signed, which means tamper-evident, not secret).
            //
            // Standard claims:
            //   - "sub" (Subject)  — typically the user's ID
            //   - "name"           — display name
            //   - "email"          — email
            //   - "role"           — role(s) — used by [Authorize(Roles=...)]
            //   - "jti" (JWT ID)   — unique token identifier, prevents replay
            //
            // The Angular app can decode the token to know who the user is
            // (no need to call /api/me on every page load).
            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.UserId.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Role, user.Role.ToString()),
            };

            // ─── STEP 4: ASSEMBLE THE TOKEN ────────────────────────────
            // JwtSecurityToken is the in-memory representation. It includes:
            //   - Issuer/Audience (who made it, who it's for)
            //   - Claims (the payload)
            //   - Expiration (how long it's valid)
            //   - Signing credentials (key + algorithm)
            //
            // Expires: 1 hour from now. Short-lived tokens are safer
            // (compromised tokens expire quickly). For longer sessions,
            // production systems pair this with refresh tokens — out of
            // scope for the capstone.
            var token = new JwtSecurityToken(
                issuer: jwtIssuer,
                audience: jwtAudience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(expiryMinutes),
                signingCredentials: credentials
            );

            // ─── STEP 5: SERIALIZE TO STRING ───────────────────────────
            // WriteToken converts the in-memory token to the compact
            // three-part Base64URL-encoded string that goes in the
            // Authorization header: "Header.Payload.Signature"
            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}