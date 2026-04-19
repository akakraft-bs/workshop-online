using System.Net;
using System.Net.Mail;
using AkaKraft.Application.Interfaces;
using AkaKraft.Infrastructure.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AkaKraft.Infrastructure.Services;

public class SmtpEmailService(
    IOptions<EmailSettings> options,
    ILogger<SmtpEmailService> logger) : IEmailService
{
    private readonly EmailSettings _settings = options.Value;

    public Task SendEmailConfirmationAsync(string toEmail, string toName, string confirmationLink)
    {
        const string subject = "E-Mail-Adresse bestätigen – AkaKraft Online";
        var body = $"""
            <html><body style="font-family:sans-serif;color:#1a1a1a;max-width:560px;margin:0 auto">
              <h2 style="color:#1565c0">Willkommen bei AkaKraft Online!</h2>
              <p>Hallo {HtmlEncode(toName)},</p>
              <p>bitte bestätige deine E-Mail-Adresse, um deinen Account zu aktivieren:</p>
              <p style="margin:24px 0">
                <a href="{confirmationLink}"
                   style="background:#1565c0;color:#fff;padding:12px 24px;border-radius:6px;
                          text-decoration:none;font-weight:600;display:inline-block">
                  E-Mail bestätigen
                </a>
              </p>
              <p style="font-size:13px;color:#666">
                Der Link ist 24 Stunden gültig.<br>
                Falls du dich nicht registriert hast, kannst du diese E-Mail ignorieren.
              </p>
              <hr style="border:none;border-top:1px solid #eee;margin:24px 0">
              <p style="font-size:12px;color:#999">AkaKraft Online · Vereinsverwaltung</p>
            </body></html>
            """;

        return SendAsync(toEmail, toName, subject, body);
    }

    public Task SendPasswordResetAsync(string toEmail, string toName, string resetLink)
    {
        const string subject = "Passwort zurücksetzen – AkaKraft Online";
        var body = $"""
            <html><body style="font-family:sans-serif;color:#1a1a1a;max-width:560px;margin:0 auto">
              <h2 style="color:#1565c0">Passwort zurücksetzen</h2>
              <p>Hallo {HtmlEncode(toName)},</p>
              <p>du hast eine Anfrage zum Zurücksetzen deines Passworts gestellt.
                 Klicke auf den Button, um ein neues Passwort zu vergeben:</p>
              <p style="margin:24px 0">
                <a href="{resetLink}"
                   style="background:#1565c0;color:#fff;padding:12px 24px;border-radius:6px;
                          text-decoration:none;font-weight:600;display:inline-block">
                  Passwort zurücksetzen
                </a>
              </p>
              <p style="font-size:13px;color:#666">
                Der Link ist 1 Stunde gültig.<br>
                Falls du diese Anfrage nicht gestellt hast, kannst du diese E-Mail ignorieren.
              </p>
              <hr style="border:none;border-top:1px solid #eee;margin:24px 0">
              <p style="font-size:12px;color:#999">AkaKraft Online · Vereinsverwaltung</p>
            </body></html>
            """;

        return SendAsync(toEmail, toName, subject, body);
    }

    private async Task SendAsync(string toEmail, string toName, string subject, string htmlBody)
    {
        if (string.IsNullOrWhiteSpace(_settings.SmtpHost) || string.IsNullOrWhiteSpace(_settings.FromAddress))
        {
            logger.LogWarning("E-Mail-Versand übersprungen: SMTP ist nicht konfiguriert (SmtpHost/FromAddress fehlt).");
            return;
        }

        using var client = new SmtpClient(_settings.SmtpHost, _settings.SmtpPort)
        {
            EnableSsl = true,
            DeliveryMethod = SmtpDeliveryMethod.Network,
            Credentials = _settings.SmtpRequiresAuth
                ? new NetworkCredential(_settings.SmtpUsername, _settings.SmtpPassword)
                : null,
        };

        using var message = new MailMessage
        {
            From = new MailAddress(_settings.FromAddress, _settings.FromName),
            Subject = subject,
            Body = htmlBody,
            IsBodyHtml = true,
        };
        message.To.Add(new MailAddress(toEmail, toName));

        try
        {
            await client.SendMailAsync(message);
            logger.LogInformation("E-Mail '{Subject}' an {To} gesendet.", subject, toEmail);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Fehler beim Senden der E-Mail an {To}.", toEmail);
            throw;
        }
    }

    private static string HtmlEncode(string value) =>
        System.Net.WebUtility.HtmlEncode(value);
}
