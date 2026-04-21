using System.Net;
using System.Net.Mail;

namespace AgriskyApi.Services
{
    // ── Interface ─────────────────────────────────────────────────────────────
    public interface IEmailService
    {
        Task SendAsync(string toEmail, string subject, string htmlBody);
    }

    // ── SMTP implementation ───────────────────────────────────────────────────
    public class SmtpEmailService : IEmailService
    {
        private readonly IConfiguration _config;
        private readonly ILogger<SmtpEmailService> _logger;

        public SmtpEmailService(IConfiguration config, ILogger<SmtpEmailService> logger)
        {
            _config = config;
            _logger = logger;
        }

        public async Task SendAsync(string toEmail, string subject, string htmlBody)
        {
            // All credentials come from configuration — NEVER hardcoded.
            // In production: use Azure Key Vault / AWS Secrets Manager / env variables.
            var emailSection = _config.GetSection("Email");

            var fromAddress = emailSection["FromAddress"]
                ?? throw new InvalidOperationException("Email:FromAddress is not configured.");
            var smtpHost = emailSection["SmtpHost"]
                ?? throw new InvalidOperationException("Email:SmtpHost is not configured.");
            var smtpPortStr = emailSection["SmtpPort"] ?? "587";
            var password = emailSection["Password"]
                ?? throw new InvalidOperationException("Email:Password is not configured.");

            if (!int.TryParse(smtpPortStr, out var smtpPort))
                throw new InvalidOperationException("Email:SmtpPort is not a valid integer.");

            var smtp = new SmtpClient(smtpHost, smtpPort)
            {
                EnableSsl = true,
                Credentials = new NetworkCredential(fromAddress, password),
                DeliveryMethod = SmtpDeliveryMethod.Network
            };

            var message = new MailMessage(fromAddress, toEmail, subject, htmlBody)
            {
                IsBodyHtml = true
            };

            try
            {
                await smtp.SendMailAsync(message);
                _logger.LogInformation("Email sent to {Email} — subject: {Subject}", toEmail, subject);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to {Email}", toEmail);
                throw;
            }
            finally
            {
                smtp.Dispose();
                message.Dispose();
            }
        }
    }
}

/*
 * ── Required appsettings.json section (values from env / secrets — NOT committed) ──
 *
 * "Email": {
 *   "FromAddress": "no-reply@agrisky.app",
 *   "SmtpHost":    "smtp.gmail.com",
 *   "SmtpPort":    "587",
 *   "Password":    ""   ← set via: dotnet user-secrets set "Email:Password" "..."
 * }
 *
 * ── Registration in Program.cs ─────────────────────────────────────────────
 * builder.Services.AddScoped<IEmailService, SmtpEmailService>();
 */