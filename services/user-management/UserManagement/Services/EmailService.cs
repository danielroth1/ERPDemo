using System.Net;
using System.Net.Mail;
using UserManagement.Configuration;

namespace UserManagement.Services;

public class EmailService
{
    private readonly SmtpSettings _smtpSettings;
    private readonly ILogger<EmailService> _logger;

    public EmailService(SmtpSettings smtpSettings, ILogger<EmailService> logger)
    {
        _smtpSettings = smtpSettings;
        _logger = logger;
    }

    public async Task SendEmailAsync(string to, string subject, string body, bool isHtml = true)
    {
        try
        {
            using var smtpClient = new SmtpClient(_smtpSettings.Host, _smtpSettings.Port)
            {
                Credentials = new NetworkCredential(_smtpSettings.Username, _smtpSettings.Password),
                EnableSsl = _smtpSettings.EnableSsl
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress(_smtpSettings.FromEmail),
                Subject = subject,
                Body = body,
                IsBodyHtml = isHtml
            };

            mailMessage.To.Add(to);

            await smtpClient.SendMailAsync(mailMessage);

            _logger.LogInformation("Email sent successfully to {Recipient}", to);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {Recipient}", to);
            throw;
        }
    }

    public async Task SendWelcomeEmailAsync(string email, string firstName)
    {
        var subject = "Welcome to ERP System";
        var body = $@"
            <html>
            <body>
                <h2>Welcome to ERP System, {firstName}!</h2>
                <p>Your account has been successfully created.</p>
                <p>You can now log in and start using the system.</p>
                <br/>
                <p>Best regards,<br/>ERP System Team</p>
            </body>
            </html>
        ";

        await SendEmailAsync(email, subject, body);
    }

    public async Task SendPasswordResetEmailAsync(string email, string resetToken)
    {
        var subject = "Password Reset Request";
        var body = $@"
            <html>
            <body>
                <h2>Password Reset Request</h2>
                <p>You have requested to reset your password.</p>
                <p>Please use the following token to reset your password:</p>
                <p><strong>{resetToken}</strong></p>
                <p>This token will expire in 1 hour.</p>
                <p>If you did not request this, please ignore this email.</p>
                <br/>
                <p>Best regards,<br/>ERP System Team</p>
            </body>
            </html>
        ";

        await SendEmailAsync(email, subject, body);
    }

    public async Task SendPasswordChangedEmailAsync(string email, string firstName)
    {
        var subject = "Password Changed Successfully";
        var body = $@"
            <html>
            <body>
                <h2>Password Changed</h2>
                <p>Hello {firstName},</p>
                <p>Your password has been successfully changed.</p>
                <p>If you did not make this change, please contact support immediately.</p>
                <br/>
                <p>Best regards,<br/>ERP System Team</p>
            </body>
            </html>
        ";

        await SendEmailAsync(email, subject, body);
    }
}
