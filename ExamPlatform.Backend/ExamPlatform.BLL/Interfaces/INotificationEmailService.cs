namespace ExamPlatform.BLL.Interfaces
{
    // Abstraction for sending email notifications. Two implementations exist:
    //   SmtpNotificationEmailService — sends real emails via MailKit SMTP.
    //   NoOpNotificationEmailService — logs the email content instead of sending.
    // The right implementation is registered in DependencyInjection based on
    // whether Smtp:Host is configured in appsettings.
    public interface INotificationEmailService
    {
        // Sends (or simulates sending) an email. Should never throw — callers
        // use fire-and-forget semantics and catch exceptions individually.
        // isHtml=true renders the body as HTML; false sends plain text.
        Task SendAsync(string toEmail, string subject, string body, bool isHtml = false);
    }
}
