using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using ExamPlatform.DAL.Contexts;
using ExamPlatform.DAL.Interfaces;
using ExamPlatform.DAL.Repositories;

namespace ExamPlatform.DAL
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddDataAccessServices(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException(
                    "Connection string 'DefaultConnection' not found in appsettings.json.");

            services.AddDbContext<ExamPlatformContext>(options =>
            {
                options.UseNpgsql(connectionString);
            });

            // ───────────── REPOSITORY REGISTRATIONS ─────────────
            //
            // AddScoped<TInterface, TImplementation>:
            //   When anyone asks DI for IUserRepository, hand them a
            //   UserRepository instance. Scoped = one per HTTP request,
            //   matching the DbContext lifetime.
            //
            // Why register the INTERFACE → IMPLEMENTATION mapping (not the
            // class directly)? So services depend on IUserRepository, not
            // UserRepository concrete. Tomorrow we could swap to MockUserRepository
            // for tests, or CachedUserRepository for performance — no service code
            // changes. THIS is loose coupling.
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IExamCategoryRepository, ExamCategoryRepository>();
            services.AddScoped<IQuestionRepository, QuestionRepository>();
            services.AddScoped<IQuestionOptionRepository, QuestionOptionRepository>();
            services.AddScoped<IExamRepository, ExamRepository>();
            services.AddScoped<IExamQuestionRepository, ExamQuestionRepository>();
            services.AddScoped<IAttemptRepository, AttemptRepository>();
            services.AddScoped<IAttemptAnswerRepository, AttemptAnswerRepository>();
            services.AddScoped<ICertificateRepository, CertificateRepository>();
            services.AddScoped<INotificationRepository, NotificationRepository>();
            services.AddScoped<IExaminerRequestRepository, ExaminerRequestRepository>();
            services.AddScoped<IIssueReportRepository, IssueReportRepository>();

            // ───────────── SUBSCRIPTION & PAYMENT SYSTEM ─────────────
            services.AddScoped<ISubscriptionPlanRepository, SubscriptionPlanRepository>();
            services.AddScoped<IExaminerSubscriptionRepository, ExaminerSubscriptionRepository>();
            services.AddScoped<IPaymentRepository, PaymentRepository>();
            services.AddScoped<IInvoiceRepository, InvoiceRepository>();
            services.AddScoped<ICandidatePurchaseRepository, CandidatePurchaseRepository>();
            services.AddScoped<ICouponRepository, CouponRepository>();
            services.AddScoped<ICouponRedemptionRepository, CouponRedemptionRepository>();
            services.AddScoped<IInstitutionRepository, InstitutionRepository>();

            return services;
        }
        
    }
}


/*

// Microsoft.EntityFrameworkCore — gives us UseNpgsql() etc.
using Microsoft.EntityFrameworkCore;

// Microsoft.Extensions.DependencyInjection — IServiceCollection (the DI container).
using Microsoft.Extensions.DependencyInjection;

// Microsoft.Extensions.Configuration — IConfiguration (reads appsettings.json).
using Microsoft.Extensions.Configuration;

using ExamPlatform.DAL.Contexts;

namespace ExamPlatform.DAL
{
    // 'static' class — never instantiated, just a container for extension methods.
    // This is the conventional name for the DI-wiring helper in each layer.
    public static class DependencyInjection
    {
        // ───────────── EXTENSION METHOD ─────────────
        // The 'this' modifier makes this an extension method on IServiceCollection.
        // Lets API call:    builder.Services.AddDataAccessServices(builder.Configuration);
        // Reads like:       "add the data-access services to my service collection."
        //
        // Why take IConfiguration as a parameter?
        // Because we need to read the connection string. API has the IConfiguration
        // already (from appsettings.json). We pass it in rather than rebuilding one.
        public static IServiceCollection AddDataAccessServices(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            // Pull the connection string from the config.
            // GetConnectionString("DefaultConnection") is shorthand for
            // configuration["ConnectionStrings:DefaultConnection"].
            // Returns null if missing; we guard with ??.
            var connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException(
                    "Connection string 'DefaultConnection' not found in appsettings.json.");

            // ───────────── REGISTER DbContext ─────────────
            // AddDbContext<T>() does several things in one call:
            //   1. Registers ExamPlatformContext with SCOPED lifetime (one instance
            //      per HTTP request — critical for change tracking isolation).
            //   2. Registers DbContextOptions<ExamPlatformContext> — this is what
            //      the DbContext receives in its constructor.
            //   3. Configures the options using the lambda we pass.
            //
            // Inside the lambda:
            //   options.UseNpgsql(connStr) tells EF Core:
            //     - use the PostgreSQL provider (Npgsql)
            //     - connect using this string
            //   This is the moment EF Core learns "we're talking to PostgreSQL."
            services.AddDbContext<ExamPlatformContext>(options =>
            {
                options.UseNpgsql(connectionString);
            });

            // We'll register repositories here later (as we build them).
            // For now, only the DbContext.

            // Return the IServiceCollection so callers can chain further calls:
            //   .AddDataAccessServices(...).AddBusinessServices(...).
            return services;
        }
    }
}
*/