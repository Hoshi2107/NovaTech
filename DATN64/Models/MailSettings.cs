namespace DATN64.Models
{
    public class MailSettings
    {
        public string? Provider { get; set; }
        public string? Host { get; set; }
        public int Port { get; set; } = 587;
        public bool EnableSsl { get; set; } = true;
        public string? Username { get; set; }
        public string? Password { get; set; }
        public string? FromEmail { get; set; }
        public string? FromName { get; set; }
        public string? SendGridApiKey { get; set; }
    }
}
