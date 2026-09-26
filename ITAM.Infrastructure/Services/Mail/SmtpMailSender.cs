using System.Net;
using System.Net.Mail;
using ITAM.Shared.Dtos.Auth;
using ITAM.Shared.Services.Mail;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ITAM.Infrastructure.Services.Mail;

public interface ISmtpMailSender
{
    Task<(bool Sent, string? Error)> SendAsync(string toEmail, string subject, string bodyHtml, CancellationToken ct = default);
    bool IsConfigured { get; }
    SmtpDiagnosticsDto GetDiagnostics();
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

    public SmtpDiagnosticsDto GetDiagnostics()
    {
        var user = (_settings.UserName ?? "").Trim();
        var hints = new List<string>();
        if (string.IsNullOrWhiteSpace(_settings.Host))
            hints.Add("SmtpSettings.Host está vacío. Para Gmail use smtp.gmail.com.");
        if (string.IsNullOrWhiteSpace(_settings.FromEmail))
            hints.Add("SmtpSettings.FromEmail está vacío.");
        if (!string.IsNullOrWhiteSpace(user) && !user.Contains('@'))
            hints.Add("SmtpSettings.UserName debe ser el correo (ej. usuario@gmail.com), no el nombre para mostrar.");
        if (string.IsNullOrWhiteSpace(_settings.Password))
            hints.Add("SmtpSettings.Password vacío. En Gmail use una Contraseña de aplicación (16 caracteres).");
        if (string.Equals(_settings.Host, "smtp.gmail.com", StringComparison.OrdinalIgnoreCase)
            && _settings.Port is not (587 or 465))
            hints.Add("Gmail normalmente usa puerto 587 (STARTTLS) o 465 (SSL).");

        return new SmtpDiagnosticsDto
        {
            IsConfigured = IsConfigured,
            Host = _settings.Host ?? "",
            Port = _settings.Port,
            EnableSsl = _settings.EnableSsl,
            FromEmail = _settings.FromEmail ?? "",
            FromDisplayName = _settings.FromDisplayName ?? "",
            UserNameHint = Mask(user),
            HasPassword = !string.IsNullOrWhiteSpace(_settings.Password),
            UserNameLooksLikeEmail = user.Contains('@'),
            Hints = hints
        };
    }

    public async Task<(bool Sent, string? Error)> SendAsync(
        string toEmail, string subject, string bodyHtml, CancellationToken ct = default)
    {
        if (!IsConfigured)
            return (false, "SMTP no configurado: falta Host y/o FromEmail. Revise SmtpSettings en appsettings de la API.");

        var userName = (_settings.UserName ?? "").Trim();
        var password = (_settings.Password ?? "").Replace(" ", "");

        try
        {
            using var client = new SmtpClient(_settings.Host.Trim(), _settings.Port)
            {
                EnableSsl = _settings.EnableSsl,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
                Timeout = 30_000
            };

            if (!string.IsNullOrWhiteSpace(userName))
                client.Credentials = new NetworkCredential(userName, password);

            using var msg = new MailMessage
            {
                From = new MailAddress(_settings.FromEmail.Trim(), _settings.FromDisplayName),
                Subject = subject,
                Body = bodyHtml,
                IsBodyHtml = true
            };
            msg.To.Add(toEmail.Trim());
            await client.SendMailAsync(msg, ct);
            return (true, null);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error enviando correo a {Email}", toEmail);
            var detail = ex.InnerException?.Message ?? ex.Message;
            return (false, detail);
        }
    }

    private static string Mask(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "(vacío)";
        if (value.Length <= 4)
            return "****";
        return value[..2] + new string('*', Math.Min(8, value.Length - 4)) + value[^2..];
    }
}
