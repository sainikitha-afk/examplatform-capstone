using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ExamPlatform.BLL.Interfaces;
using ExamPlatform.BLL.Services;
using ExamPlatform.BLL.Settings;

namespace ExamPlatform.BLL
{
    public static class DependencyInjection
    {
        // BLL's composition root. API calls this method to wire up every service.
        public static IServiceCollection AddBusinessServices(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            // ───────────── CORE SERVICES ─────────────
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IExamCategoryService, ExamCategoryService>();
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<ICertificateService, CertificateService>();
            services.AddScoped<IQuestionService, QuestionService>();
            services.AddScoped<IExamService, ExamService>();
            services.AddScoped<IAttemptService, AttemptService>();
            services.AddScoped<INotificationService, NotificationService>();
            services.AddScoped<IAnalyticsService, AnalyticsService>();
            services.AddScoped<IExaminerRequestService, ExaminerRequestService>();
            services.AddScoped<IIssueReportService, IssueReportService>();

            // ───────────── SUBSCRIPTION & PAYMENT SYSTEM ─────────────
            services.AddScoped<IPaymentGatewayService, PaymentGatewayService>();
            services.AddScoped<IInvoiceService, InvoiceService>();
            services.AddScoped<ICouponService, CouponService>();
            services.AddScoped<IExaminerSubscriptionService, ExaminerSubscriptionService>();
            services.AddScoped<ICandidatePurchaseService, CandidatePurchaseService>();
            services.AddScoped<IRevenueAnalyticsService, RevenueAnalyticsService>();
            services.AddScoped<IInstitutionVerificationService, InstitutionVerificationService>();
            services.AddHostedService<SubscriptionExpirySweepHostedService>();

            // ───────────── BACKGROUND TASK QUEUE & WORKERS ─────────────
            services.AddSingleton<IBackgroundTaskQueue>(ctx => new BackgroundTaskQueue(100));
            services.AddHostedService<CommonBackgroundService>();
            services.AddScoped<IAnalyticsProcessor, AnalyticsProcessor>();

            // ───────────── AI LEARNING COACH ─────────────
            // Reads the topic-score JSON the analytics pipeline already produces. Registered
            // separately from the coach service so tests can stub the file system.
            services.AddScoped<ITopicScoreReader, TopicScoreReader>();
            services.AddScoped<ILearningCoachService, LearningCoachService>();

            // ───────────── HTTP SERVICES ─────────────
            services.AddHttpClient<IAgentService, AgentService>();

            // The coach's own Gemini channel, kept separate from AgentService so the working
            // question-categorization pipeline is not disturbed. The timeout is what turns a
            // hanging provider into a prompt "temporarily unavailable" instead of a request
            // that holds a connection open until the client gives up.
            services.AddHttpClient<ICoachGeminiClient, CoachGeminiClient>(client =>
            {
                client.Timeout = TimeSpan.FromSeconds(20);
            });

            // ───────────── EMAIL SETTINGS (Options Pattern) ─────────────
            // Manually maps "Smtp:*" keys from IConfiguration to EmailSettings.
            // We avoid the Configure<T>(IConfigurationSection) extension because
            // that extension lives in Microsoft.Extensions.Options.ConfigurationExtensions
            // which is not referenced by this plain class-library project.
            services.Configure<EmailSettings>(opts =>
            {
                var s = configuration.GetSection(EmailSettings.SectionName);
                opts.Host        = s["Host"]        ?? string.Empty;
                opts.Port        = int.TryParse(s["Port"], out var p) ? p : 587;
                opts.Username    = s["Username"]    ?? string.Empty;
                opts.Password    = s["Password"]    ?? string.Empty;
                opts.FromAddress = s["FromAddress"] ?? string.Empty;
                opts.FromName    = s["FromName"]    ?? "Exam Platform";
                opts.EnableSsl   = !bool.TryParse(s["EnableSsl"], out var ssl) || ssl;
            });

            // ───────────── EMAIL SERVICE ─────────────
            // Use real SMTP when Smtp:Host is configured; fall back to the no-op
            // logger implementation so the app runs locally without an SMTP server.
            var smtpHost = configuration[EmailSettings.SectionName + ":Host"];
            if (!string.IsNullOrWhiteSpace(smtpHost))
                services.AddScoped<INotificationEmailService, SmtpNotificationEmailService>();
            else
                services.AddScoped<INotificationEmailService, NoOpNotificationEmailService>();

            return services;
        }
    }
}
