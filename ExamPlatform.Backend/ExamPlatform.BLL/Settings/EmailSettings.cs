namespace ExamPlatform.BLL.Settings
{
    // Strongly-typed POCO bound to the "Smtp" section in appsettings.json.
    // Registered via services.Configure<EmailSettings>() in BLL DependencyInjection.
    // Injected into SmtpNotificationEmailService via IOptions<EmailSettings>
    // instead of raw IConfiguration — follows the Options Pattern (SOLID / DIP).
    public class EmailSettings
    {
        public const string SectionName = "Smtp";

        public string Host { get; set; } = string.Empty;
        public int Port { get; set; } = 587;
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string FromAddress { get; set; } = string.Empty;
        public string FromName { get; set; } = "Exam Platform";
        public bool EnableSsl { get; set; } = true;
    }
}
