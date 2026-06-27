using System.Net;
using System.Net.Http.Headers;
using System.Net.Mail;
using System.Text;
using System.Text.Json;
using DATN64.Models;
using Microsoft.Extensions.Options;

namespace DATN64.Services
{
    public class EmailService
    {
        private readonly MailSettings _settings;
        private readonly HttpClient _httpClient;

        public EmailService(IOptions<MailSettings> options)
        {
            _settings = options.Value;
            _httpClient = new HttpClient();
        }

        public void SendEmail(string toEmail, string subject, string htmlBody)
        {
            if (string.IsNullOrWhiteSpace(_settings.FromEmail))
            {
                throw new InvalidOperationException("Email sender chưa được cấu hình. Vui lòng bổ sung MailSettings:FromEmail.");
            }

            var useSendGrid = !string.IsNullOrWhiteSpace(_settings.Provider) && _settings.Provider.Trim().Equals("sendgrid", StringComparison.OrdinalIgnoreCase);
            var hasValidSendGridKey = !string.IsNullOrWhiteSpace(_settings.SendGridApiKey) && !_settings.SendGridApiKey.Contains("YOUR_SENDGRID_API_KEY", StringComparison.OrdinalIgnoreCase);

            if (useSendGrid && hasValidSendGridKey)
            {
                SendViaSendGrid(toEmail, subject, htmlBody);
                return;
            }

            if (useSendGrid && !hasValidSendGridKey)
            {
                // Fall back to SMTP when SendGrid is requested but not configured.
                useSendGrid = false;
            }

            if (string.IsNullOrWhiteSpace(_settings.Host) || string.IsNullOrWhiteSpace(_settings.Username) || string.IsNullOrWhiteSpace(_settings.Password))
            {
                throw new InvalidOperationException("Cấu hình SMTP chưa đúng. Vui lòng kiểm tra MailSettings:Host, Username và Password.");
            }

            if (_settings.Password.Contains("your-gmail-app-password", StringComparison.OrdinalIgnoreCase) || _settings.Password.Contains("app-password", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Mật khẩu SMTP chưa được cấu hình. Vui lòng cập nhật MailSettings:Password với mật khẩu ứng dụng email hoặc mật khẩu đăng nhập SMTP.");
            }

            using var client = new SmtpClient(_settings.Host, _settings.Port)
            {
                EnableSsl = _settings.EnableSsl,
                Credentials = new NetworkCredential(_settings.Username, _settings.Password),
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
                Timeout = 15000
            };

            using var message = new MailMessage()
            {
                From = new MailAddress(_settings.FromEmail, _settings.FromName ?? "NovaTech"),
                Subject = subject,
                Body = htmlBody,
                IsBodyHtml = true,
                Priority = MailPriority.High
            };

            message.To.Add(toEmail);
            client.Send(message);
        }

        private void SendViaSendGrid(string toEmail, string subject, string htmlBody)
        {
            if (string.IsNullOrWhiteSpace(_settings.SendGridApiKey))
            {
                throw new InvalidOperationException("SendGrid API key chưa được cấu hình. Vui lòng bổ sung MailSettings:SendGridApiKey.");
            }

            var payload = new
            {
                personalizations = new[]
                {
                    new
                    {
                        to = new[] { new { email = toEmail } }
                    }
                },
                from = new { email = _settings.FromEmail, name = _settings.FromName ?? "NovaTech" },
                subject,
                content = new[] { new { type = "text/html", value = htmlBody } }
            };

            using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.sendgrid.com/v3/mail/send");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _settings.SendGridApiKey);
            request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            using var response = _httpClient.Send(request);
            if (!response.IsSuccessStatusCode)
            {
                var responseBody = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                throw new InvalidOperationException($"SendGrid gửi email thất bại: {response.StatusCode}. {responseBody}");
            }
        }
    }
}
