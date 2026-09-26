using System.Net;
using System.Net.Mail;
using ITAM.Shared.Services.Mail;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ITAM.Infrastructure.Services.Mail;

public interface ISmtpMailSender
{
    Task<(bool Sent, string? Error)> SendAsync(string toEmail, string subject, string bodyHtml, CancellationToken ct = default);
    bool IsConfigured { get; }
}

public sealed class SmtpMailSender : ISmtpMailSender
{
    private readonly SmtpSettings _settings;
    private readonly ILogger<SmtpMailSender> _logger;

    public SmtpMailSender(IOptions<SmtpSettings> options, ILogger<SmtpMailSender> logger)
    {
        _settings = options.Value;
        _logger = logger;
    }

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(_settings.Host)
        && !string.IsNullOrWhiteSpace(_settings.FromEmail);

    public async Task<(bool Sent, string? Error)> SendAsync(
        string toEmail, string subject, string bodyHtml, CancellationToken ct = default)
    {
        if (!IsConfigured)
            return (false, "SMTP no configurado (SmtpSettings.Host / FromEmail).");

        try
        {
            using var client = new SmtpClient(_settings.Host, _settings.Port)
            {
                EnableSsl = _settings.EnableSsl,
                DeliveryMethod = SmtpDeliveryMethod.Network
            };
            if (!string.IsNullOrWhiteSpace(_settings.UserName))
                client.Credentials = new NetworkCredential(_settings.UserName, _settings.Password);

            using var msg = new MailMessage
            {
                From = new MailAddress(_settings.FromEmail, _settings.FromDisplayName),
                Subject = subject,
                Body = bodyHtml,
                IsBodyHtml = true
            };
            msg.To.Add(toEmail);
            await client.SendMailAsync(msg, ct);
            return (true, null);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error enviando correo a {Email}", toEmail);
            return (false, ex.Message);
        }
    }
}
