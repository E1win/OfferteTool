using Application.Models.Email;

namespace Application.Interfaces.Services;

public interface IEmailSender
{
    Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default);
}
