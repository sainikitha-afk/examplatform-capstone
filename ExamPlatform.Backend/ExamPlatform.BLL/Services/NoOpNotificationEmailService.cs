// NoOpNotificationEmailService is the development/test implementation of
// INotificationEmailService. Instead of sending real emails, it logs the
// email content so developers can see what would have been sent.
// Registered in DI when Smtp:Host is empty or not configured in appsettings.

#pragma warning disable CA1848
using ExamPlatform.BLL.Interfaces;
using Microsoft.Extensions.Logging;

namespace ExamPlatform.BLL.Services
{
    public class NoOpNotificationEmailService : INotificationEmailService
    {
        private readonly ILogger<NoOpNotificationEmailService> _logger;

        public NoOpNotificationEmailService(ILogger<NoOpNotificationEmailService> logger)
        {
            _logger = logger;
        }

        // ════════════════════════════════════════════════════════════════
        //   SendAsync (no-op)
        // ════════════════════════════════════════════════════════════════
        public Task SendAsync(string toEmail, string subject, string body, bool isHtml = false)
        {
            // LogInformation writes to the configured logging sinks (console in dev).
            // isHtml is included so the log clearly shows which template type was triggered.
            _logger.LogInformation(
                "[NO-OP EMAIL] To: {To} | Subject: {Subject} | IsHtml: {IsHtml} | Body: {Body}",
                toEmail, subject, isHtml, body);

            // Task.CompletedTask is a cached, already-completed Task — zero allocation,
            // zero await overhead. Returned because the interface signature is async.
            return Task.CompletedTask;
        }
    }
}
