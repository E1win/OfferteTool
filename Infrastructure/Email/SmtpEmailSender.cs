using System.Net;
using System.Net.Mail;
using Application.Interfaces.Services;
using Application.Models.Email;
using Infrastructure.Configuration;
using Microsoft.Extensions.Options;

namespace Infrastructure.Email;

public class SmtpEmailSender(IOptions<SmtpEmailOptions> options) : IEmailSender
{
    private readonly SmtpEmailOptions smtpOptions = options.Value;

    public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);
        EnsureConfigured();

        using var mailMessage = new MailMessage
        {
            From = new MailAddress(smtpOptions.FromAddress, smtpOptions.FromName),
            Subject = message.Subject,
            Body = message.HtmlBody ?? message.TextBody,
            IsBodyHtml = !string.IsNullOrWhiteSpace(message.HtmlBody)
        };

        mailMessage.To.Add(message.To);

        // For accessibility/compatibility reasons, add plain text alternative view if HTML body is provided
        if (!string.IsNullOrWhiteSpace(message.HtmlBody))
            mailMessage.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(message.TextBody, null, "text/plain"));

        using var smtpClient = new SmtpClient(smtpOptions.Host, smtpOptions.Port)
        {
            EnableSsl = smtpOptions.EnableSsl,
            Credentials = new NetworkCredential(smtpOptions.UserName, smtpOptions.Password)
        };

        // Register cancellation to cancel the SendAsync operation if the token is triggered
        using var cancellationRegistration = cancellationToken.Register(smtpClient.SendAsyncCancel);
        await smtpClient.SendMailAsync(mailMessage, cancellationToken);
    }

    private void EnsureConfigured()
    {
        if (string.IsNullOrWhiteSpace(smtpOptions.Host)
            || string.IsNullOrWhiteSpace(smtpOptions.UserName)
            || string.IsNullOrWhiteSpace(smtpOptions.Password)
            || string.IsNullOrWhiteSpace(smtpOptions.FromAddress))
        {
            throw new InvalidOperationException("SMTP e-mail is niet goed geconfigureerd.");
        }
    }
}
