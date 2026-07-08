// NotificationEmailServiceTests.cs — covers the two INotificationEmailService
// implementations:
//   NoOpNotificationEmailService  — dev/test implementation that only logs.
//   SmtpNotificationEmailService  — real sender; only the paths that do NOT
//   open a network connection are unit-testable (missing host, invalid address).
//   The actual SMTP handshake requires a live server and stays out of unit tests.

using ExamPlatform.BLL.Services;
using ExamPlatform.BLL.Settings;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;

namespace ExamPlatform.BLL.Tests.Services
{
    [TestFixture]
    public class NotificationEmailServiceTests
    {
        // ════════════════════════════════════════════════════════════════
        //   NoOpNotificationEmailService
        // ════════════════════════════════════════════════════════════════

        [Test]
        public async Task NoOp_SendAsync_CompletesWithoutSendingAnything()
        {
            var loggerMock = new Mock<ILogger<NoOpNotificationEmailService>>();
            var sut = new NoOpNotificationEmailService(loggerMock.Object);

            var act = async () => await sut.SendAsync(
                "someone@test.com", "Subject", "<p>Body</p>", isHtml: true);

            await act.Should().NotThrowAsync();

            // The no-op implementation logs the email instead of sending it.
            loggerMock.Verify(l => l.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("NO-OP EMAIL")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
        }

        // ════════════════════════════════════════════════════════════════
        //   SmtpNotificationEmailService
        // ════════════════════════════════════════════════════════════════

        private static SmtpNotificationEmailService BuildSmtpService(EmailSettings settings) =>
            new(Options.Create(settings),
                new Mock<ILogger<SmtpNotificationEmailService>>().Object);

        [Test]
        public async Task Smtp_SendAsync_HostNotConfigured_ReturnsWithoutSending()
        {
            // Empty Host — the guard clause returns before touching the network.
            var sut = BuildSmtpService(new EmailSettings { Host = "" });

            var act = async () => await sut.SendAsync("someone@test.com", "Subject", "Body");

            await act.Should().NotThrowAsync();
        }

        [Test]
        public async Task Smtp_SendAsync_InvalidRecipientAddress_IsCaughtNotThrown()
        {
            // MailboxAddress.Parse throws on a malformed address BEFORE any network
            // call; the catch block must swallow it (email failures are non-fatal).
            var sut = BuildSmtpService(new EmailSettings
            {
                Host = "smtp.example.com",
                FromAddress = "noreply@example.com"
            });

            var act = async () => await sut.SendAsync("", "Subject", "Body", isHtml: true);

            await act.Should().NotThrowAsync();
        }

        [TestCase(true,  TestName = "Smtp_ConnectFails_HtmlBody")]
        [TestCase(false, TestName = "Smtp_ConnectFails_PlainTextBody")]
        public async Task Smtp_SendAsync_ConnectionRefused_IsCaughtNotThrown(bool isHtml)
        {
            // 127.0.0.1 on port 1 refuses instantly — no external network involved.
            // Covers the full message-building path (both body branches) and the
            // catch block around the SMTP connection.
            var sut = BuildSmtpService(new EmailSettings
            {
                Host = "127.0.0.1",
                Port = 1,
                EnableSsl = false,
                FromAddress = "noreply@example.com"
            });

            var act = async () => await sut.SendAsync("someone@test.com", "Subject", "Body", isHtml);

            await act.Should().NotThrowAsync();
        }
    }
}
