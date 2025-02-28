using ClaimRequest.BLL.Services.Interfaces;
using ClaimRequest.DAL.Data.Requests.Email;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;
using SmtpClient = MailKit.Net.Smtp.SmtpClient;

namespace ClaimRequest.BLL.Services.Implements
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _config;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration config, ILogger<EmailService> logger)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<bool> SendEmailAsync(SendMailRequest request)
        {
            try
            {
                // Kiểm tra dữ liệu đầu vào
                if (string.IsNullOrWhiteSpace(request.To))
                {
                    _logger.LogError("Recipient email address is required.");
                    return false;
                }

                if (string.IsNullOrWhiteSpace(request.Subject))
                {
                    _logger.LogWarning("Email subject is empty.");
                }

                if (string.IsNullOrWhiteSpace(request.Body))
                {
                    _logger.LogWarning("Email body is empty.");
                }



                // Đọc thông tin cấu hình từ appsettings.json
                var smtpServer = _config["EmailSettings:SmtpServer"];
                var smtpPort = _config["EmailSettings:SmtpPort"];
                var senderEmail = _config["EmailSettings:SenderEmail"];
                var senderPassword = _config["EmailSettings:SenderPassword"];

                if (string.IsNullOrWhiteSpace(smtpServer) || string.IsNullOrWhiteSpace(smtpPort) ||
                    string.IsNullOrWhiteSpace(senderEmail) || string.IsNullOrWhiteSpace(senderPassword))
                {
                    _logger.LogError("SMTP configuration is missing in appsettings.json.");
                    return false;
                }

                // Tạo đối tượng MimeMessage
                var email = new MimeMessage();
                email.From.Add(MailboxAddress.Parse(senderEmail));
                email.To.Add(MailboxAddress.Parse(request.To));
                email.Subject = request.Subject;


                var bodyBuilder = new BodyBuilder { HtmlBody = request.Body };

                // Xử lý file đính kèm (nếu có)
                //if (request.Attachments != null && request.Attachments.Count > 0)
                //{
                //    foreach (var attachment in request.Attachments)
                //    {
                //        try
                //        {
                //            if (!string.IsNullOrWhiteSpace(attachment) && File.Exists(attachment))
                //            {
                //                bodyBuilder.Attachments.Add(attachment);
                //            }
                //            else
                //            {
                //                _logger.LogWarning($"Attachment not found: {attachment}");
                //            }
                //        }
                //        catch (Exception ex)
                //        {
                //            _logger.LogError(ex, $"Error processing attachment: {attachment}");
                //        }
                //    }
                //}

                email.Body = bodyBuilder.ToMessageBody();

                // Kết nối và gửi email qua SMTP
                using var smtp = new SmtpClient();
                await smtp.ConnectAsync(smtpServer, int.Parse(smtpPort), SecureSocketOptions.StartTls);
                await smtp.AuthenticateAsync(senderEmail, senderPassword);
                await smtp.SendAsync(email);
                await smtp.DisconnectAsync(true);

                _logger.LogInformation($"Email sent successfully to {request.To}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Email sending failed.");
                return false;
            }
        }
    }
}
