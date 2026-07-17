using ExamPlatform.DAL;
using ExamPlatform.DAL.Contexts;
using ExamPlatform.BLL;
using Microsoft.EntityFrameworkCore;
using ExamPlatform.API.Helpers;
using ExamPlatform.API.Middleware;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;

namespace ExamPlatform.API
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            #region Controllers & Swagger
            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();

            // Plain Swagger — no Authorize button.
            // We test protected endpoints via the Authorization header in
            // Swagger's "curl" output, or via Postman.
            builder.Services.AddSwaggerGen(opt =>
            {
                // Adds support for IFormFile parameters (used by QuestionsController.ImportFromExcel).
                // Without this filter, Swagger fails to generate the swagger.json on startup.
                opt.OperationFilter<ExamPlatform.API.Helpers.SwaggerFileOperationFilter>();
            });
            #endregion

            #region CORS
            // Allowed origins come from configuration so the deployed frontend's
            // origin can be added in Azure (App Setting "Cors:AllowedOrigins:0" = the
            // Static Web App URL) without a code change. Falls back to the local
            // Angular dev origin when nothing is configured.
            var corsOrigins = builder.Configuration
                .GetSection("Cors:AllowedOrigins")
                .Get<string[]>();
            if (corsOrigins is null || corsOrigins.Length == 0)
                corsOrigins = new[] { "http://localhost:4200" };

            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AngularDev", policy =>
                {
                    policy.WithOrigins(corsOrigins)
                        .AllowAnyHeader()
                        .AllowAnyMethod();
                });
            });
            #endregion

            #region Contexts
            builder.Services.AddDataAccessServices(builder.Configuration);
            #endregion

            #region Authentication
            var jwtKey = builder.Configuration["Jwt:Key"]!;
            var jwtIssuer = builder.Configuration["Jwt:Issuer"]!;
            var jwtAudience = builder.Configuration["Jwt:Audience"]!;

            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtIssuer,
                    ValidAudience = jwtAudience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
                };
            });

            builder.Services.AddAuthorization();
            builder.Services.AddSingleton<JwtTokenGenerator>();
            #endregion

            #region Rate Limiting (AI Coach only)
            // A NAMED policy — deliberately not a global limiter. Only endpoints that opt in
            // with [EnableRateLimiting(RateLimitPolicies.AiCoach)] are affected, which today
            // means CoachController and nothing else. Every existing endpoint keeps its
            // current behaviour.
            //
            // Why the coach needs one at all: each request costs Gemini quota and money, so an
            // impatient (or malicious) user holding down Send is a real cost problem. Exam
            // submission or loading the dashboard is not.
            builder.Services.AddRateLimiter(options =>
            {
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

                options.AddPolicy(RateLimitPolicies.AiCoach, httpContext =>
                {
                    // Partition per user so one heavy user cannot exhaust everyone else's
                    // budget. The user id comes from the validated JWT — UseRateLimiter runs
                    // after UseAuthentication, so the claim is populated by now. IP is only a
                    // fallback (in practice the endpoints all require a token anyway).
                    var partitionKey =
                        httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                        ?? httpContext.User.FindFirst("sub")?.Value
                        ?? httpContext.Connection.RemoteIpAddress?.ToString()
                        ?? "anonymous";

                    return RateLimitPartition.GetFixedWindowLimiter(partitionKey, _ =>
                        new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = 10,
                            Window = TimeSpan.FromMinutes(1),
                            // Queue nothing: a rejected request should fail fast with 429 so the
                            // UI can say "slow down", not hang while it waits for a slot.
                            QueueLimit = 0
                        });
                });

                // Same isolated-policy technique, applied only to the two payment-charge
                // endpoints (subscription upgrade, exam purchase) — prevents a rapid-fire
                // checkout submission loop, never touches any other endpoint.
                options.AddPolicy(RateLimitPolicies.Payments, httpContext =>
                {
                    var partitionKey =
                        httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                        ?? httpContext.User.FindFirst("sub")?.Value
                        ?? httpContext.Connection.RemoteIpAddress?.ToString()
                        ?? "anonymous";

                    return RateLimitPartition.GetFixedWindowLimiter(partitionKey, _ =>
                        new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = 10,
                            Window = TimeSpan.FromMinutes(1),
                            QueueLimit = 0
                        });
                });
            });
            #endregion

            #region Services
            // Pass builder.Configuration so AddBusinessServices can conditionally
            // register SmtpNotificationEmailService vs NoOpNotificationEmailService
            // based on whether Smtp:Host is configured in appsettings.json.
            builder.Services.AddBusinessServices(builder.Configuration);
            #endregion

            var app = builder.Build();

            #region Database Migration
            // Apply any pending EF Core migrations at startup. On Azure the Postgres
            // database starts empty, so without this the app would crash on the first
            // query (no tables). Migrate() is idempotent — it only applies migrations
            // that haven't run yet, so it is a safe no-op once the schema is current.
            // Gated behind a config flag (default on) so it can be disabled if a
            // separate migration step is ever preferred.
            if (app.Configuration.GetValue("Database:MigrateOnStartup", true))
            {
                using var scope = app.Services.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<ExamPlatformContext>();
                var startupLogger = scope.ServiceProvider
                    .GetRequiredService<ILoggerFactory>()
                    .CreateLogger("Startup.Migration");
                try
                {
                    db.Database.Migrate();
                    startupLogger.LogInformation("Database migrations applied (or already up to date).");
                }
                catch (Exception ex)
                {
                    startupLogger.LogError(ex, "Database migration failed at startup.");
                    throw; // fail fast — a mis-migrated DB should stop the app, not serve broken data
                }
            }
            #endregion

            #region Development-only Middleware
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }
            #endregion

            #region Custom Middleware
            app.UseMiddleware<GlobalExceptionMiddleware>();
            #endregion

            #region Standard Pipeline
            //app.UseHttpsRedirection();
            app.UseCors("AngularDev");
            
            // Serve static files from ExamPlatform.BLL/Assets at the /assets URL path
            var assetsPath = Path.Combine(builder.Environment.ContentRootPath, "..", "ExamPlatform.BLL", "Assets");
            if (Directory.Exists(assetsPath))
            {
                app.UseStaticFiles(new StaticFileOptions
                {
                    FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(assetsPath),
                    RequestPath = "/assets"
                });
            }

            app.UseAuthentication();
            app.UseAuthorization();

            // AFTER authentication so the AI-coach limiter can partition on the user id claim
            // (before it, User is anonymous and every caller would share one bucket).
            app.UseRateLimiter();
            #endregion

            #region Endpoints
            app.MapControllers();
            #endregion

            app.Run();
        }
    }
}