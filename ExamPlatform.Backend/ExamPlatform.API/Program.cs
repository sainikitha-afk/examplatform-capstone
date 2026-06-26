using ExamPlatform.DAL;
using ExamPlatform.BLL;
using ExamPlatform.API.Helpers;
using ExamPlatform.API.Middleware;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

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
            // Filled when Angular calls the API.
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

            #region Services
            // Pass builder.Configuration so AddBusinessServices can conditionally
            // register SmtpNotificationEmailService vs NoOpNotificationEmailService
            // based on whether Smtp:Host is configured in appsettings.json.
            builder.Services.AddBusinessServices(builder.Configuration);
            #endregion

            var app = builder.Build();

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
            app.UseHttpsRedirection();
            app.UseAuthentication();
            app.UseAuthorization();
            #endregion

            #region Endpoints
            app.MapControllers();
            #endregion

            app.Run();
        }
    }
}