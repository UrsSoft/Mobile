using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace SantiyeTalepApi.Services
{
    public interface IEmailService
    {
        Task SendPasswordResetEmailAsync(string toEmail, string toName, string resetToken);
        Task SendEmailAsync(string toEmail, string subject, string body, bool isHtml = true);
    }

    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task SendPasswordResetEmailAsync(string toEmail, string toName, string resetToken)
        {
            var resetLink = $"{_configuration["EmailSettings:WebAppUrl"]}/Account/ResetPassword?token={resetToken}";

            var htmlBody = $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #4CAF50; color: white; padding: 20px; text-align: center; }}
        .content {{ background-color: #f9f9f9; padding: 30px; }}
        .button {{ 
            display: inline-block; 
            padding: 12px 30px; 
            background-color: #4CAF50; 
            color: white; 
            text-decoration: none; 
            border-radius: 5px;
            margin: 20px 0;
        }}
        .footer {{ text-align: center; padding: 20px; color: #666; font-size: 12px; }}
        .warning {{ background-color: #fff3cd; border-left: 4px solid #ffc107; padding: 12px; margin: 20px 0; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>Þifre Sýfýrlama</h1>
        </div>
        <div class='content'>
            <p>Merhaba {toName},</p>
            
            <p>Þifre sýfýrlama talebinizi aldýk. Þifrenizi sýfýrlamak için aþaðýdaki butona týklayýn:</p>
            
            <div style='text-align: center;'>
                <a href='{resetLink}' class='button'>Þifremi Sýfýrla</a>
            </div>
            
            <p>Veya aþaðýdaki linki tarayýcýnýza kopyalayabilirsiniz:</p>
            <p style='word-break: break-all; background-color: #e9ecef; padding: 10px; border-radius: 3px;'>{resetLink}</p>
            
            <div class='warning'>
                <strong>?? Dikkat:</strong> Bu baðlantý güvenlik nedeniyle 1 saat sonra geçersiz olacaktýr.
            </div>
            
            <p>Eðer þifre sýfýrlama talebinde bulunmadýysanýz, bu e-postayý görmezden gelebilirsiniz.</p>
        </div>
        <div class='footer'>
            <p>© 2024 Þantiye Talep Yönetim Sistemi</p>
            <p>Bu otomatik bir e-postadýr, lütfen yanýtlamayýnýz.</p>
        </div>
    </div>
</body>
</html>";

            await SendEmailAsync(toEmail, "Þifre Sýfýrlama Talebi", htmlBody, true);
        }

        public async Task SendEmailAsync(string toEmail, string subject, string body, bool isHtml = true)
        {
            try
            {
                var email = new MimeMessage();
                email.From.Add(new MailboxAddress(
                    _configuration["EmailSettings:SenderName"], 
                    _configuration["EmailSettings:SenderEmail"]
                ));
                email.To.Add(MailboxAddress.Parse(toEmail));
                email.Subject = subject;

                var builder = new BodyBuilder();
                if (isHtml)
                {
                    builder.HtmlBody = body;
                }
                else
                {
                    builder.TextBody = body;
                }
                email.Body = builder.ToMessageBody();

                using var smtp = new SmtpClient();
                
                // Gmail için özel ayarlar
                await smtp.ConnectAsync(
                    _configuration["EmailSettings:SmtpServer"],
                    int.Parse(_configuration["EmailSettings:SmtpPort"] ?? "587"),
                    SecureSocketOptions.StartTls
                );

                await smtp.AuthenticateAsync(
                    _configuration["EmailSettings:SenderEmail"],
                    _configuration["EmailSettings:SenderPassword"]
                );

                await smtp.SendAsync(email);
                await smtp.DisconnectAsync(true);

                _logger.LogInformation($"Email sent successfully to {toEmail}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error sending email to {toEmail}");
                throw;
            }
        }
    }
}
