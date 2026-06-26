// SmtpNotificationEmailService sends real emails via MailKit + MimeKit.
// MailKit is the modern, actively maintained SMTP library for .NET.
// System.Net.Mail.SmtpClient is marked obsolete in newer .NET versions.
//
// Configuration is injected via IOptions<EmailSettings> (Options Pattern)
// rather than raw IConfiguration — this follows SOLID (DIP) and makes the
// service easily testable by injecting a test EmailSettings instance.
//
// This implementation is only registered in DI when Smtp:Host is non-empty;
// otherwise NoOpNotificationEmailService is used instead.

#pragma warning disable CA1848
using ExamPlatform.BLL.Interfaces;
using ExamPlatform.BLL.Settings;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;

namespace ExamPlatform.BLL.Services
{
    public class SmtpNotificationEmailService : INotificationEmailService
    {
        private readonly EmailSettings _settings;
        private readonly ILogger<SmtpNotificationEmailService> _logger;

        public SmtpNotificationEmailService(
            IOptions<EmailSettings> options,
            ILogger<SmtpNotificationEmailService> logger)
        {
            _settings = options.Value;
            _logger   = logger;
        }

        // ════════════════════════════════════════════════════════════════
        //   SendAsync
        // ════════════════════════════════════════════════════════════════
        public async Task SendAsync(string toEmail, string subject, string body, bool isHtml = false)
        {
            if (string.IsNullOrWhiteSpace(_settings.Host))
            {
                _logger.LogWarning(
                    "SMTP host is not configured. Email to {To} was not sent.", toEmail);
                return;
            }

            try
            {
                // MimeMessage is MailKit/MimeKit's strongly-typed email message.
                // MailboxAddress.Parse validates the address format at construction time.
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(_settings.FromName, _settings.FromAddress));
                message.To.Add(MailboxAddress.Parse(toEmail));
                message.Subject = subject;

                // BodyBuilder produces either HtmlBody or TextBody depending on the caller.
                // Both can be set simultaneously for multipart/alternative (HTML + plain fallback),
                // but for simplicity we set only one based on the isHtml flag.
                var builder = new BodyBuilder();
                if (isHtml)
                    builder.HtmlBody = body;
                else
                    builder.TextBody = body;
                message.Body = builder.ToMessageBody();

                // SmtpClient here is MailKit's client, not System.Net.Mail.
                // It supports STARTTLS (port 587) and SSL (port 465) natively.
                using var client = new SmtpClient();

                // SecureSocketOptions.StartTls negotiates TLS after connecting on port 587.
                // SecureSocketOptions.None is used when EnableSsl=false (e.g. dev relay).
                var socketOptions = _settings.EnableSsl
                    ? SecureSocketOptions.StartTls
                    : SecureSocketOptions.None;

                await client.ConnectAsync(_settings.Host, _settings.Port, socketOptions);
                await client.AuthenticateAsync(_settings.Username, _settings.Password);
                await client.SendAsync(message);

                // quit=true sends the SMTP QUIT command before closing the connection.
                await client.DisconnectAsync(quit: true);

                _logger.LogInformation(
                    "Email sent to {To}: {Subject}", toEmail, subject);
            }
            catch (Exception ex)
            {
                // Log but do NOT rethrow. Email failures must never roll back
                // the already-committed certificate/notification records.
                _logger.LogError(ex,
                    "Failed to send email to {To}: {Subject}", toEmail, subject);
            }
        }
    }
}
